using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;

namespace CsQuery.Implementation
{
    /// <summary>
    /// Interface for read only list.
    /// </summary>
    ///
    /// <typeparam name="T">
    /// Generic type parameter.
    /// </typeparam>

    public interface IReadOnlyList<T> : IReadOnlyCollection<T>, IEnumerable<T>, IEnumerable
    {
        /// <summary>
        /// Indexer to get items within this collection using array index syntax.
        /// </summary>
        ///
        /// <param name="index">
        /// Zero-based index of the entry to access.
        /// </param>
        ///
        /// <returns>
        /// The indexed item.
        /// </returns>

        T this[int index] { get; }
    }
}
