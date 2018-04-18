using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SchedulerPOC
{
    class Program
    {
        private static object lockObj = new object();

        static void Main(string[] args)
        {
            int entityId = 0;
            TriggerJob(entityId);
        }

        private static void TriggerJob(int entityId)
        {
            var states = new Dictionary<int, (DateTimeOffset scheduledAt, object args)>();

            Monitor.Enter(lockObj);
            try
            {
                if (!states.TryGetValue(entityId, out var state))
                {
                    var delay = TimeSpan.FromMinutes(1);
                    var scheduledAt = DateTimeOffset.UtcNow.Add(delay);
                    states.Add(entityId, (scheduledAt, new object()));
                    Task.Run(async () =>
                    {
                        await Task.Delay(delay);
                        Monitor.Enter(lockObj);
                        try
                        {
                            await DoJob();
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

        private static Task DoJob()
        {
            throw new NotImplementedException();
        }
    }
}
