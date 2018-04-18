using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SchedulerPOC
{
    class Sample1 : IScheduler
    {
        private object lockObj = new object();
        private Dictionary<int, JobArgs> states =
            new Dictionary<int, JobArgs>();

        public void TriggerJob(int entityId)
        {
            Monitor.Enter(lockObj);
            try
            {
                if (!states.TryGetValue(entityId, out var state))
                {
                    var delay = TimeSpan.FromSeconds(1);
                    var scheduledAt = DateTimeOffset.UtcNow.Add(delay);
                    states.Add(entityId, new JobArgs(scheduledAt));
                    Task.Run(async () =>
                    {
                        await Task.Delay(delay).ConfigureAwait(false);
                        Monitor.Enter(lockObj);
                        try
                        {
                            await DoJob(entityId).ConfigureAwait(false);
                            states.Remove(entityId);
                        }
                        finally
                        {
                            Monitor.Exit(lockObj);
                        }
                    });
                }
            }
            finally
            {
                Monitor.Exit(lockObj);
            }

        }

        public Task DoJob(int entityId)
        {
            Console.WriteLine(entityId);
            return Task.CompletedTask;
        }
    }
}
