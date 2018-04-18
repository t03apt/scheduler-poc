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
        private static Dictionary<int, (DateTimeOffset scheduledAt, object args)> states = 
            new Dictionary<int, (DateTimeOffset scheduledAt, object args)>();

        static async Task Main(string[] args)
        {
            TriggerJob(0);
            TriggerJob(1);
            TriggerJob(0);
            TriggerJob(1);
            await Task.Delay(TimeSpan.FromSeconds(2));
            TriggerJob(0);
            TriggerJob(1);
            TriggerJob(0);
            TriggerJob(1);
            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        private static void TriggerJob(int entityId)
        {
            Monitor.Enter(lockObj);
            try
            {
                if (!states.TryGetValue(entityId, out var state))
                {
                    var delay = TimeSpan.FromSeconds(2);
                    var scheduledAt = DateTimeOffset.UtcNow.Add(delay);
                    states.Add(entityId, (scheduledAt, new object()));
                    Task.Run(async () =>
                    {
                        await Task.Delay(delay);
                        Monitor.Enter(lockObj);
                        try
                        {
                            await DoJob(entityId);
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

        private static Task DoJob(int entityId)
        {
            Console.WriteLine(entityId);
            return Task.CompletedTask;
        }
    }
}
