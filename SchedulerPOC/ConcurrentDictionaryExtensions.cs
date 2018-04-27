using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SchedulerPOC
{
    /// <summary>
    /// See: https://blogs.msdn.microsoft.com/pfxteam/2011/04/02/little-known-gems-atomic-conditional-removals-from-concurrentdictionary/
    /// </summary>
    internal static class ConcurrentDictionaryExtensions
    {
        public static bool TryRemove<TKey, TValue>(
            this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, TValue previousValueToCompare)
        {
            var collection = (ICollection<KeyValuePair<TKey, TValue>>)dictionary;
            var toRemove = new KeyValuePair<TKey, TValue>(key, previousValueToCompare);
            return collection.Remove(toRemove);
        }
    }
}