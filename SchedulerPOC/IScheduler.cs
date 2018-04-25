using System;

namespace SchedulerPOC
{
    interface IScheduler
    {
        void AddAsync(Guid jobId, Guid parentId);
    }
}
