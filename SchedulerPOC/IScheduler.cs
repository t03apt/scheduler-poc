using System.Threading.Tasks;

namespace SchedulerPOC
{
    interface IScheduler
    {
        void TriggerJob(int entityId);

        Task DoJob(int entityId);
    }
}
