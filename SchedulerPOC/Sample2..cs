using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace SchedulerPOC
{
    /// <summary>
    /// Logically correct implementation, but poor performance
    /// </summary>
    class Sample2 : IScheduler
    {
        private Dictionary<int, WorkArgs> states = new Dictionary<int, WorkArgs>();
        private ConcurrentDictionary<int, object> lockDict = new ConcurrentDictionary<int, object>();

        public object GetLock(int entityId)
        {
            return lockDict.GetOrAdd(entityId, s => new object());
        }

        public void TriggerWork(int entityId)
        {
            Monitor.Enter(GetLock(entityId));
            try
            {
                if (!states.TryGetValue(entityId, out var state))
                {
                    var delay = Constants.Delay;
                    var scheduledAt = DateTimeOffset.UtcNow.Add(delay);
                    states.Add(entityId, new WorkArgs(scheduledAt));
                    Task.Run(async () =>
                    {
                        await Task.Delay(delay).ConfigureAwait(false);
                        Monitor.Enter(GetLock(entityId));
                        try
                        {
                            await DoWork(entityId).ConfigureAwait(false);
                            states.Remove(entityId);
                        }
                        finally
                        {
                            Monitor.Exit(GetLock(entityId));
                        }
                    });
                }
            }
            finally
            {
                Monitor.Exit(GetLock(entityId));
            }
        }

        public Task DoWork(int entityId)
        {
            Log.Information("{@entityId}", entityId);
            return Task.CompletedTask;
        }
    }
}
