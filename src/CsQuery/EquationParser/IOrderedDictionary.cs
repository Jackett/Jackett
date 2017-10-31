using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.EquationParser
{
    /// <summary>
    /// An interface representing a dictionary that also has intrinsic element order.
    /// </summary>
    ///
    /// <typeparam name="TKey">
    /// Type of the key.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// Type of the value.
    /// </typeparam>

    public interface IOrderedDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IList<KeyValuePair<TKey, TValue>>, ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>> 
    {
        /// <summary>
        /// Obtain the zero-based index of the given key.
        /// </summary>
        ///
        /// <param name="key">
        /// The key.
        /// </param>
        ///
        /// <returns>
        /// The zero-based index of the key in the ordered dictionary
        /// </returns>

        int IndexOf(TKey key);
    }
}
