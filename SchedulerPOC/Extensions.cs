using System;
using System.Linq;

namespace SchedulerPOC
{
    internal static class Extensions
    {
        public static string ToShort(this Guid guid)
        {
            return string.Join(string.Empty, guid.ToString().Take(5));
        }
    }
}
