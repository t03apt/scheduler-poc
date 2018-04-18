using System;

namespace SchedulerPOC
{
    class WorkArgs
    {
        public WorkArgs(DateTimeOffset? scheduledAt, Guid? triggerId = null)
        {
            ScheduledAt = scheduledAt;
            TriggerId = triggerId ?? Guid.NewGuid();
        }

        public DateTimeOffset? ScheduledAt { get; }
        public Guid TriggerId { get; }
        public object Args { get; }
    }
}
