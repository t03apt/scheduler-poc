using System;

namespace SchedulerPOC
{
    class JobArgs
    {
        public JobArgs(DateTimeOffset scheduledAt)
        {
            this.ScheduledAt = scheduledAt;
        }

        public DateTimeOffset ScheduledAt { get; set; }
        public object Args { get; set; }
    }
}
