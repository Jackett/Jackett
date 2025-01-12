using System.Collections.Generic;

namespace Jackett.Common.Extensions
{
    public static class IEnumerableExtensions
    {
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer = null) => new(source, comparer);
    }
}
