﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace SchedulerPOC
{
    /// <summary>
    /// Logically correct implementation, but very poor performance
    /// </summary>
    class Sample1 : IScheduler
    {
        private object lockObj = new object();
        private Dictionary<int, WorkArgs> states =
            new Dictionary<int, WorkArgs>();

        public void TriggerWork(int entityId)
        {
            Monitor.Enter(lockObj);
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
                        Monitor.Enter(lockObj);
                        try
                        {
                            await DoWork(entityId).ConfigureAwait(false);
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

        public Task DoWork(int entityId)
        {
            Log.Information("{@entityId}", entityId);
            return Task.CompletedTask;
        }
    }
}