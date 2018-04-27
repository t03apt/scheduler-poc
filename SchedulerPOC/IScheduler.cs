using System;

namespace SchedulerPOC
{
    internal interface IScheduler
    {
        void AddAsync(Guid jobId, Guid parentId, int observed = 1);
    }
}
