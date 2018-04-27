using System.Threading.Tasks;
using Serilog;

namespace SchedulerPOC
{
    internal static class TaskExtensions
    {
        public static Task ContinueWithFaultHandler(this Task task)
        {
            return task.ContinueWith(t =>
            {
                if (!t.IsFaulted)
                {
                    return;
                }

                t.Exception.Handle(e =>
                {
                    Log.Logger.Error(e, "Error processing task");
                    return true;
                });
            });
        }
    }
}