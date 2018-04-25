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
        private static readonly TimeSpan DefaultRollupDelay = TimeSpan.FromSeconds(0.5);
        private const int RollupCalculationThreshold = 10;

        public void AddAsync(Guid jobId, Guid parentId)
        {
            var newChangeStateId = Guid.NewGuid();

            var isNewScheduleNeeded = false;
            var state = _states.AddOrUpdate(parentId,
                (key) =>
                {
                    isNewScheduleNeeded = true;
                    return new ChangeState(jobId, parentId, DateTimeOffset.UtcNow.Add(DefaultRollupDelay), 1, new CancellationTokenSource(), newChangeStateId);
                },
                (key, existingValue) =>
                {
                    ChangeState newChangeState;

                    if (existingValue.TriggerDateTime < DateTimeOffset.UtcNow)
                    {
                        //// 1) we have a task scheduled already
                        //return new ChangeState(existingValue.JobId, existingValue.ParentId, existingValue.TriggerDateTime, existingValue.Observed + 1, existingValue.CancellationTokenSourceOfPreviousRollup);

                        // 2) a previous task is running - we should trigger a new one
                        // 3) a previous task is failed for some reason
                        isNewScheduleNeeded = true;
                        existingValue.CancellationTokenSourceOfPreviousRollup.Cancel();
                        newChangeState = new ChangeState(
                            existingValue.JobId, 
                            existingValue.ParentId, 
                            DateTimeOffset.UtcNow.Add(DefaultRollupDelay), 
                            existingValue.Observed + 1, // or 1
                            new CancellationTokenSource(), 
                            newChangeStateId);
                    }
                    else
                    {
                        // 1) rollup is scheduled already, not running, but observed needs to be incremented
                        var observerd = existingValue.Observed + 1;
                        DateTimeOffset triggerDateTime;
                        CancellationTokenSource newCancellationTokenSource;

                        if (observerd >= RollupCalculationThreshold)
                        {
                            existingValue.CancellationTokenSourceOfPreviousRollup.Cancel();
                            triggerDateTime = DateTimeOffset.UtcNow.AddSeconds(1);
                            newCancellationTokenSource = new CancellationTokenSource();
                            isNewScheduleNeeded = true;
                        }
                        else
                        {
                            newCancellationTokenSource = existingValue.CancellationTokenSourceOfPreviousRollup;
                            triggerDateTime = existingValue.TriggerDateTime;
                            isNewScheduleNeeded = false;
                        }

                        newChangeState = new ChangeState(
                            existingValue.JobId, 
                            existingValue.ParentId,
                            triggerDateTime, 
                            observerd,
                            newCancellationTokenSource,
                            existingValue.ChangeStateId);


                    }

                    return newChangeState;
                });

            if (isNewScheduleNeeded)
            {
                ScheduleRollup(parentId, newChangeStateId, state.TriggerDateTime, state.CancellationTokenSourceOfPreviousRollup.Token);
            }
        }

        private void ScheduleRollup(Guid stateKey, Guid expectedChangeStateId, DateTimeOffset triggerDateTime, CancellationToken cancellationToken)
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
                    }
                }

                if (_states.TryGetValue(stateKey, out var state) && state.ChangeStateId == expectedChangeStateId)
                {
                    await DoWork(state.JobId, state.ParentId).ConfigureAwait(false);
                    _states.TryRemove(stateKey, state);
                }
            }, cancellationToken);
        }

        private void ScheduleImmediateRollup(Guid stateKey, Guid expectedChangeStateId, CancellationToken cancellationToken)
        {
            ScheduleRollup(stateKey, expectedChangeStateId, DateTimeOffset.UtcNow.AddSeconds(1), cancellationToken);
        }

        private static TimeSpan? GetDelay(DateTimeOffset triggerDateTime)
        {
            var delay = triggerDateTime - DateTimeOffset.UtcNow;
            return (delay > TimeSpan.Zero) ? delay : (TimeSpan?)null;
        }

        public Task DoWork(Guid jobId, Guid targetId)
        {
            Log.Information("{@jobId} {@targetId}", jobId, targetId);
            return Task.CompletedTask;
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
