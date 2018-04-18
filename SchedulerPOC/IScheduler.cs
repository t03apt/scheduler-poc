using System.Threading.Tasks;

namespace SchedulerPOC
{
    interface IScheduler
    {
        void TriggerWork(int entityId);

        Task DoWork(int entityId);
    }
}
