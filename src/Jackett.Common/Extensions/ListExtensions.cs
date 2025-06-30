using System;
using System.Collections.Generic;
using System.Linq;

namespace Jackett.Common.Extensions
{
    public static class ListExtensions
    {
        public static List<List<T>> ChunkBy<T>(this List<T> source, int chunkSize) => source
            .Select((x, i) => new { Index = i, Value = x }).GroupBy(x => x.Index / chunkSize)
            .Select(x => x.Select(v => v.Value).ToList()).ToList();

        public static void AddRangeIfNotExists<T, TKey>(
            this List<T> list,
            IEnumerable<T> itemsToAdd,
            Func<T, TKey> keySelector)
        {
            var existingKeys = new HashSet<TKey>(list.Select(keySelector));

            foreach (var item in itemsToAdd)
            {
                if (!existingKeys.Contains(keySelector(item)))
                {
                    list.Add(item);
                    existingKeys.Add(keySelector(item));
                }
            }
        }

        public static void ReplaceIfExistsByKey<T, TKey>(
            this List<T> list,
            IEnumerable<T> itemsToReplace,
            Func<T, TKey> keySelector)
        {
            var indexMap = list
                           .Select((item, index) => new { Key = keySelector(item), Index = index })
                           .ToDictionary(x => x.Key, x => x.Index);

            foreach (var newItem in itemsToReplace)
            {
                var key = keySelector(newItem);
                if (indexMap.TryGetValue(key, out var index))
                {
                    list[index] = newItem;
                }
            }
        }
    }
}
