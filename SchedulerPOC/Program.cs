using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace SchedulerPOC
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var samples = new[] { new Sample1() };

            foreach (var sample in samples)
            {
                Console.WriteLine($"--- Begin: {sample.GetType().Name} ---");
                sample.TriggerJob(0);
                sample.TriggerJob(1);
                sample.TriggerJob(0);
                sample.TriggerJob(1);
                await Task.Delay(TimeSpan.FromSeconds(2));
                sample.TriggerJob(0);
                sample.TriggerJob(1);
                sample.TriggerJob(0);
                sample.TriggerJob(1);
                await Task.Delay(TimeSpan.FromSeconds(2));
                Console.WriteLine($"--- End: {sample.GetType().Name} ---");
            }
        }
    }
}
