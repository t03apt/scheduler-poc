using System;
using System.Threading.Tasks;

namespace SchedulerPOC
{
    interface IScheduler
    {
        void AddAsync(Guid jobId, Guid parentId);
        Task DoWork(Guid jobId, Guid targetId);
    }
}
