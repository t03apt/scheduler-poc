using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace SchedulerPOC
{
    /// <summary>
    /// Logically correct implementation, better performance
    /// </summary>
    class Sample3 : IScheduler
    {
        private readonly ConcurrentDictionary<Guid, ChangeState> _states = new ConcurrentDictionary<Guid, ChangeState>();
        private static readonly TimeSpan DefaultRollupDelay = TimeSpan.FromSeconds(1);
        private const int RollupCalculationThreshold = 10;

        public void AddAsync(Guid jobId, Guid parentId)
        {
            var newScheduleId = Guid.NewGuid();

            var isNewScheduleNeeded = false;
            var state = _states.AddOrUpdate(parentId,
                key =>
                {
                    isNewScheduleNeeded = true;
                    return new ChangeState(jobId, parentId, DateTimeOffset.UtcNow.Add(DefaultRollupDelay), 1, new CancellationTokenSource(), newScheduleId);
                },
                (key, existingValue) =>
                {
                    ChangeState newChangeState;
                    var observerd = existingValue.Observed + 1;
                    var isObserverdThresholdReached = observerd >= RollupCalculationThreshold;
                    isNewScheduleNeeded = isObserverdThresholdReached ||
                                          existingValue.TriggerDateTime < DateTimeOffset.UtcNow;

                    if (isNewScheduleNeeded)
                    {
                        existingValue.CancellationTokenSourceOfPreviousRollup.Cancel();
                        var triggerDateTime = isObserverdThresholdReached ? DateTimeOffset.UtcNow : DateTimeOffset.UtcNow.Add(DefaultRollupDelay);
                        newChangeState = new ChangeState(
                            existingValue.JobId,
                            existingValue.ParentId,
                            triggerDateTime,
                            1,
                            new CancellationTokenSource(),
                            newScheduleId);
                    }
                    else
                    {
                        newChangeState = new ChangeState(
                            existingValue.JobId,
                            existingValue.ParentId,
                            existingValue.TriggerDateTime,
                            observerd,
                            existingValue.CancellationTokenSourceOfPreviousRollup,
                            existingValue.ScheduleId);
                    }

                    Log.Logger.Information("key:{@key} newScheduleNeeded:{@isNewScheduleNeeded} scheduleId:{@scheduleId}", newChangeState.ParentId.ToShort(), isNewScheduleNeeded, existingValue.ScheduleId.ToShort());

                    return newChangeState;
                });

            if (isNewScheduleNeeded)
            {
                ScheduleRollup(parentId, newScheduleId, state.TriggerDateTime, state.CancellationTokenSourceOfPreviousRollup.Token);
            }
        }

        private void ScheduleRollup(Guid stateKey, Guid expectedScheduleId, DateTimeOffset triggerDateTime, CancellationToken cancellationToken)
        {
            Task.Run(async () =>
            {
                var delay = GetDelay(triggerDateTime);
                if (delay != null)
                {
                    try
                    {
                        await Task.Delay(delay.Value, cancellationToken).ConfigureAwait(false);
                    }
                    catch (TaskCanceledException e)
                    {
                        // Log.Logger.Information("task cancelled");
                        // it is expected

                        Log.Logger.Information("cancelled key:{@key} scheduleId:{@scheduleId}", stateKey.ToShort(), expectedScheduleId);
                        return;
                    }
                }

                if (_states.TryGetValue(stateKey, out var state) && state.ScheduleId == expectedScheduleId)
                {
                    await DoWork(state.JobId, state.ParentId, state.ScheduleId).ConfigureAwait(false);
                    _states.TryRemove(stateKey, state);
                }
            }, cancellationToken);
        }

        private static TimeSpan? GetDelay(DateTimeOffset triggerDateTime)
        {
            var delay = triggerDateTime - DateTimeOffset.UtcNow;
            return (delay > TimeSpan.Zero) ? delay : (TimeSpan?)null;
        }

        public async Task DoWork(Guid jobId, Guid targetId, Guid scheduleId)
        {
            Log.Information("updating key:{@targetId} scheduleId:{@scheduleId}...", targetId.ToShort(), scheduleId.ToShort());
            await Task.Delay(TimeSpan.FromSeconds(0.1)).ConfigureAwait(false);
            Log.Information("updated key:{@targetId} scheduleId:{@scheduleId}...", targetId.ToShort(), scheduleId.ToShort());
        }
    }

    // https://blogs.msdn.microsoft.com/pfxteam/2011/04/02/little-known-gems-atomic-conditional-removals-from-concurrentdictionary/
    static class ConcurrentDictionaryExtensions
    {
        public static bool TryRemove<TKey, TValue>(
            this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, TValue previousValueToCompare)
        {
            var collection = (ICollection<KeyValuePair<TKey, TValue>>)dictionary;
            var toRemove = new KeyValuePair<TKey, TValue>(key, previousValueToCompare);
            return collection.Remove(toRemove);
        }
    }
}
