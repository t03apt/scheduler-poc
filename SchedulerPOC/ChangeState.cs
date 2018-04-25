using System;
using System.Threading;
using Newtonsoft.Json;

namespace SchedulerPOC
{
    public class ChangeState
    {
        public ChangeState(Guid jobId, Guid parentId, DateTimeOffset triggerDateTime, int observed, CancellationTokenSource cancellationTokenSourceOfPreviousRollup, Guid changeStateId)
        {
            JobId = jobId;
            ParentId = parentId;
            TriggerDateTime = triggerDateTime;
            Observed = observed;
            CancellationTokenSourceOfPreviousRollup = cancellationTokenSourceOfPreviousRollup;
            ChangeStateId = changeStateId;
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
        public Guid ChangeStateId { get; private set; }

        [JsonIgnore]
        public CancellationTokenSource CancellationTokenSourceOfPreviousRollup { get; private set; }
    }
}
