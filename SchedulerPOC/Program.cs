using System;
using System.Threading.Tasks;
using Serilog;
using Serilog.Enrichers;

namespace SchedulerPOC
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.With(new ThreadIdEnricher())
                .WriteTo.Console(
                    outputTemplate: "{Timestamp:mm:ss.fff} [{Level}] ({ThreadId}) {Message}{NewLine}{Exception}")
                .CreateLogger();

            var samples = new IScheduler[] { new Sample1(), new Sample2(), new Sample3() };

            foreach (var sample in samples)
            {
                Console.WriteLine($"--- Begin: {sample.GetType().Name} ---");
                await TriggerWork(sample);
                await Task.Delay(TimeSpan.FromSeconds(1));
                Console.WriteLine($"--- End: {sample.GetType().Name} ---");
            }
        }

        private static async Task TriggerWork(IScheduler sample)
        {
            var started = DateTimeOffset.UtcNow;
            while (DateTimeOffset.UtcNow - started < TimeSpan.FromSeconds(2))
            {
                await Task.Delay(10);
                sample.TriggerWork(0);
                sample.TriggerWork(1);
            }
        }
    }
}
