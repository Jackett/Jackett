using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.Implementation
{
    /// <summary>
    /// A sorted dictionary that allows lookup by range.
    /// </summary>
    interface IRangeSortedDictionary<TKey,TValue> : IDictionary<TKey[], TValue>
    {
        /// <summary>
        /// Return all keys starting with subKey
        /// </summary>
        /// <param name="subKey">The substring to match</param>
        /// <returns></returns>
        IEnumerable<TKey[]> GetRangeKeys(TKey[] subKey);

        /// <summary>
        /// Return all values having keys beginning with subKey
        /// </summary>
        /// <param name="subKey"></param>
        /// <returns></returns>
        IEnumerable<TValue> GetRange(TKey[] subKey);


    }
}
