//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Threading;
//using System.Threading.Tasks;
//using Serilog;

//namespace SchedulerPOC
//{
//    /// <summary>
//    /// Logically correct implementation, but poor performance
//    /// </summary>
//    class Sample2 : IScheduler
//    {
//        private Dictionary<int, ChangeState> states = new Dictionary<int, ChangeState>();
//        private ConcurrentDictionary<int, object> lockDict = new ConcurrentDictionary<int, object>();

//        public object GetLock(int entityId)
//        {
//            return lockDict.GetOrAdd(entityId, s => new object());
//        }

//        public void AddAsync(int parentId)
//        {
//            Monitor.Enter(GetLock(parentId));
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
//                        Monitor.Enter(GetLock(parentId));
//                        try
//                        {
//                            await DoWork(parentId).ConfigureAwait(false);
//                            states.Remove(parentId);
//                        }
//                        finally
//                        {
//                            Monitor.Exit(GetLock(parentId));
//                        }
//                    });
//                }
//            }
//            finally
//            {
//                Monitor.Exit(GetLock(parentId));
//            }
//        }

//        public Task DoWork(int entityId)
//        {
//            Log.Information("{@parentId}", entityId);
//            return Task.CompletedTask;
//        }
//    }
//}
