using System;
using System.Threading.Tasks;
using Serilog;
using Serilog.Enrichers;

namespace SchedulerPOC
{
    internal class Program
    {
        private static async Task Main()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.With(new ThreadIdEnricher())
                .WriteTo.Console(
                    outputTemplate: "{Timestamp:mm:ss.fff} [{Level}] ({ThreadId}) {Message}{NewLine}{Exception}")
                .CreateLogger();

            var samples = new IScheduler[] { new Scheduler(args => DoWork(args.jobId, args.targetId, args.scheduleId)) };

            foreach (var sample in samples)
            {
                Console.WriteLine($"--- Begin: {sample.GetType().Name} ---");
                await TriggerWork(sample);
                await Task.Delay(TimeSpan.FromSeconds(1));
                Console.WriteLine($"--- End: {sample.GetType().Name} ---");
            }
        }

        private static async Task DoWork(Guid jobId, Guid targetId, Guid scheduleId)
        {
            Log.Information("updating key:{@targetId} scheduleId:{@scheduleId}...", targetId.ToShort(), scheduleId.ToShort());
            await Task.Delay(TimeSpan.FromSeconds(0.1)).ConfigureAwait(false);
            Log.Information("updated key:{@targetId} scheduleId:{@scheduleId}...", targetId.ToShort(), scheduleId.ToShort());
        }

        private static async Task TriggerWork(IScheduler sample)
        {
            var jobId = Guid.NewGuid();
            var parentId1 = Guid.NewGuid();
            var parentId2 = Guid.NewGuid();

            var started = DateTimeOffset.UtcNow;
            while (DateTimeOffset.UtcNow - started < TimeSpan.FromSeconds(2))
            {
                await Task.Delay(10);
                sample.AddAsync(jobId, parentId1);
                //sample.AddAsync(jobId, parentId2);
            }
        }
    }
}
