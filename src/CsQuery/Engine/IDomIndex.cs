using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsQuery.Implementation;

namespace CsQuery.Engine
{
    /// <summary>
    /// Interface for a DOM index. Defines methods to add and remove items from the index, and query the index.
    /// </summary>
    public interface IDomIndex
    {
        /// <summary>
        /// Adds an element to the index.
        /// </summary>
        ///
        /// <param name="element">
        /// The element.
        /// </param>

        void AddToIndex(IDomIndexedNode element);

        /// <summary>
        /// Adds an element to the index for the specified key.
        /// </summary>
        ///
        /// <param name="key">
        /// The key.
        /// </param>
        /// <param name="element">
        /// The element.
        /// </param>

        void AddToIndex(ushort[] key, IDomIndexedNode element);

        /// <summary>
        /// Removes an element from the index.
        /// </summary>
        ///
        /// <param name="element">
        /// The element.
        /// </param>

        void RemoveFromIndex(IDomIndexedNode element);

        /// <summary>
        /// Removes a key from the index
        /// </summary>
        ///
        /// <param name="key">
        /// The key.
        /// </param>
        /// <param name="element">
        /// The element.
        /// </param>

        void RemoveFromIndex(ushort[] key, IDomIndexedNode element);

        /// <summary>
        /// Clears this object to its blank/initial state.
        /// </summary>

        void Clear();

        /// <summary>
        /// The number of unique index keys
        /// </summary>
        ///
        /// <returns>
        /// The count of items in the index
        /// </returns>

        int Count { get; }

    }
}
