using System;
using System.Threading;
using Newtonsoft.Json;

namespace SchedulerPOC
{
    internal class ChangeState
    {
        public ChangeState(Guid jobId, Guid parentId, DateTimeOffset triggerDateTime, int observed, CancellationTokenSource cancellationTokenSourceOfPreviousRollup, Guid scheduleId)
        {
            JobId = jobId;
            ParentId = parentId;
            TriggerDateTime = triggerDateTime;
            Observed = observed;
            CancellationTokenSourceOfPreviousRollup = cancellationTokenSourceOfPreviousRollup;
            ScheduleId = scheduleId;
        }

        [JsonProperty("jobId")]
        public Guid JobId { get; private set; }

        [JsonProperty("parentId")]
        public Guid ParentId { get; private set; }

        [JsonProperty("triggerDateTime")]
        public DateTimeOffset TriggerDateTime { get; private set; }

        [JsonProperty("observed")]
        public int Observed { get; private set; }

        [JsonIgnore]
        public Guid ScheduleId { get; }

        [JsonIgnore]
        public CancellationTokenSource CancellationTokenSourceOfPreviousRollup { get; }
    }
}
