using System;
using System.Linq;

namespace SchedulerPOC
{
    static class Extensions
    {
        public static string ToShort(this Guid guid)
        {
            return string.Join(string.Empty, guid.ToString().Take(5));
        }
    }
}
