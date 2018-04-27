using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace SchedulerPOC
{
    internal class Scheduler : IScheduler
    {
        private const int RollupCalculationThreshold = 10;
        private static readonly TimeSpan DefaultRollupDelay = TimeSpan.FromSeconds(1);

        private readonly ConcurrentDictionary<Guid, ChangeState> _states = new ConcurrentDictionary<Guid, ChangeState>();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly Func<(Guid jobId, Guid targetId, Guid scheduleId), Task> _doWorkAsync;

        public Scheduler(Func<(Guid jobId, Guid targetId, Guid scheduleId), Task> doWorkAsync)
        {
            _doWorkAsync = doWorkAsync ?? throw new ArgumentNullException(nameof(doWorkAsync));
        }

        public void AddAsync(Guid jobId, Guid parentId, int observed = 1)
        {
            var newScheduleId = Guid.NewGuid();
            var isNewScheduleNeeded = false;
            var state = _states.AddOrUpdate(parentId,
                key =>
                {
                    isNewScheduleNeeded = true;
                    return new ChangeState(jobId, parentId, ComputeTriggerDateTime(observed), observed, CreateCancellationTokenSource(), newScheduleId);
                },
                (key, existingValue) =>
                {
                    ChangeState newChangeState;
                    var newObserved = existingValue.Observed + observed;
                    isNewScheduleNeeded = IsObserverdThresholdReached(newObserved) ||
                                          existingValue.TriggerDateTime < DateTimeOffset.UtcNow;

                    if (isNewScheduleNeeded)
                    {
                        existingValue.CancellationTokenSourceOfPreviousRollup.Cancel();
                        newChangeState = new ChangeState(
                            existingValue.JobId,
                            existingValue.ParentId,
                            ComputeTriggerDateTime(newObserved),
                            1,
                            CreateCancellationTokenSource(),
                            newScheduleId);
                    }
                    else
                    {
                        newChangeState = new ChangeState(
                            existingValue.JobId,
                            existingValue.ParentId,
                            existingValue.TriggerDateTime,
                            newObserved,
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

        public void Shutdown()
        {
            _cancellationTokenSource.Cancel();
        }

        private static DateTimeOffset ComputeTriggerDateTime(int observed)
        {
            return IsObserverdThresholdReached(observed) ? DateTimeOffset.UtcNow : DateTimeOffset.UtcNow.Add(DefaultRollupDelay);
        }

        private static bool IsObserverdThresholdReached(int observed)
        {
            var isObserverdThresholdReached = observed >= RollupCalculationThreshold;
            return isObserverdThresholdReached;
        }

        private CancellationTokenSource CreateCancellationTokenSource()
        {
            return CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token);
        }

        private static TimeSpan? GetDelay(DateTimeOffset triggerDateTime)
        {
            var delay = triggerDateTime - DateTimeOffset.UtcNow;
            return (delay > TimeSpan.Zero) ? delay : (TimeSpan?)null;
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
                    catch (TaskCanceledException)
                    {
                        // Log.Logger.Information("task cancelled");
                        // it is expected

                        Log.Logger.Information("cancelled key:{@key} scheduleId:{@scheduleId}", stateKey.ToShort(), expectedScheduleId);
                        return;
                    }
                }

                if (_states.TryGetValue(stateKey, out var state) && state.ScheduleId == expectedScheduleId)
                {
                    await _doWorkAsync((state.JobId, state.ParentId, state.ScheduleId)).ConfigureAwait(false);
                    _states.TryRemove(stateKey, state);
                }
            }, cancellationToken).ContinueWithFaultHandler();
        }
    }
}
