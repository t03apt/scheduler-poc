using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog;

namespace SchedulerPOC
{
    /// <summary>
    /// Logically correct implementation, better performance
    /// </summary>
    class Sample3 : IScheduler
    {
        private ConcurrentDictionary<int, WorkArgs> states = new ConcurrentDictionary<int, WorkArgs>();

        public void TriggerWork(int entityId)
        {
            var isAddedOrUpdated = false;
            var triggerId = Guid.NewGuid();
            var workArgs = states.AddOrUpdate(entityId,
                (key) =>
                {
                    isAddedOrUpdated = true;
                    return new WorkArgs(DateTimeOffset.UtcNow.Add(Constants.Delay), triggerId);
                },
                (key, existingValue) =>
                {
                    if (existingValue.ScheduledAt < DateTimeOffset.UtcNow)
                    {
                        // work should have been done by now
                        isAddedOrUpdated = true;
                        return new WorkArgs(null, triggerId);
                    }
                    else
                    {
                        isAddedOrUpdated = false;
                        return existingValue;
                    }
                });

            if (isAddedOrUpdated)
            {
                Task.Run(async () =>
                {
                    var delay = GetDelay(workArgs);
                    if (delay != null)
                    {
                        await Task.Delay(delay.Value).ConfigureAwait(false);
                    }
                    if (states.TryGetValue(entityId, out var args) && args.TriggerId == triggerId)
                    {
                        await DoWork(entityId).ConfigureAwait(false);
                        states.TryRemove(entityId, args);
                    }
                });
            }
        }

        private static TimeSpan? GetDelay(WorkArgs workArgs)
        {
            if (workArgs.ScheduledAt == null)
            {
                return null;
            }

            var delay = workArgs.ScheduledAt - DateTimeOffset.UtcNow;
            return (delay > TimeSpan.Zero) ? delay : null;
        }

        public Task DoWork(int entityId)
        {
            Log.Information("{@entityId}", entityId);
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
