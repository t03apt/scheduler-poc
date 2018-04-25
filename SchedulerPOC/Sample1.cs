//using System;
//using System.Collections.Generic;
//using System.Threading;
//using System.Threading.Tasks;
//using Serilog;

//namespace SchedulerPOC
//{
//    /// <summary>
//    /// Logically correct implementation, but very poor performance
//    /// </summary>
//    class Sample1 : IScheduler
//    {
//        private object lockObj = new object();
//        private Dictionary<int, ChangeState> states =
//            new Dictionary<int, ChangeState>();

//        public void AddAsync(int parentId)
//        {
//            Monitor.Enter(lockObj);
//            try
//            {
//                if (!states.TryGetValue(parentId, out var state))
//                {
//                    var delay = Constants.Delay;
//                    var scheduledAt = DateTimeOffset.UtcNow.Add(delay);
//                    states.Add(parentId, new ChangeState(scheduledAt));
//                    Task.Run(async () =>
//                    {
//                        await Task.Delay(delay).ConfigureAwait(false);
//                        Monitor.Enter(lockObj);
//                        try
//                        {
//                            await DoWork(parentId).ConfigureAwait(false);
//                            states.Remove(parentId);
//                        }
//                        finally
//                        {
//                            Monitor.Exit(lockObj);
//                        }
//                    });
//                }
//            }
//            finally
//            {
//                Monitor.Exit(lockObj);
//            }
//        }

//        public Task DoWork(int entityId)
//        {
//            Log.Information("{@parentId}", entityId);
//            return Task.CompletedTask;
//        }
//    }
//}
