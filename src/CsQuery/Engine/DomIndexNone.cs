using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsQuery.Implementation;
using CsQuery.HtmlParser;

namespace CsQuery.Engine
{
    /// <summary>
    /// Simple implementation of DOM index that only stores a reference to the index target. This
    /// will perform much better than the ranged index for dom construction &amp; manipulation, but
    /// worse for complex queries.
    /// </summary>

    public class DomIndexNone : IDomIndex
    {
        /// <summary>
        /// Default constructor for the index
        /// </summary>

        public DomIndexNone()
        {
        }

        /// <summary>
        /// Adds an element to the index.
        /// </summary>
        ///
        /// <param name="element">
        /// The element to add.
        /// </param>

        public void AddToIndex(IDomIndexedNode element)
        {
            return;
        }


        /// <summary>
        /// Adds an element to the index for the specified key.
        /// </summary>
        ///
        /// <param name="key">
        /// The key to remove.
        /// </param>
        /// <param name="element">
        /// The element to add.
        /// </param>

        public void AddToIndex(ushort[] key, IDomIndexedNode element)
        {
            return;
        }

        /// <summary>
        /// Remove an element from the index using its key.
        /// </summary>
        ///
        /// <param name="key">
        /// The key to remove.
        /// </param>
        /// <param name="element">
        /// The element to remove.
        /// </param>

        public void RemoveFromIndex(ushort[] key, IDomIndexedNode element)
        {
            return;
        }

        /// <summary>
        /// Remove an element from the index.
        /// </summary>
        ///
        /// <param name="element">
        /// The element to remove
        /// </param>

        public void RemoveFromIndex(IDomIndexedNode element)
        {
            return;
        }

        /// <summary>
        /// Query the document's index for a subkey.
        /// </summary>
        ///
        /// <param name="subKey">
        /// The subkey to match
        /// </param>
        ///
        /// <returns>
        /// A sequence of all matching keys
        /// </returns>

        public IEnumerable<IDomObject> QueryIndex(ushort[] subKey)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Queries the index, returning all matching elements.
        /// </summary>
        ///
        /// <exception cref="NotImplementedException">
        /// Thrown when the requested operation is unimplemented.
        /// </exception>
        ///
        /// <param name="subKey">
        /// The subkey to match.
        /// </param>
        /// <param name="depth">
        /// The depth.
        /// </param>
        /// <param name="includeDescendants">
        /// true to include, false to exclude the descendants.
        /// </param>
        ///
        /// <returns>
        /// An enumerator that allows foreach to be used to process query index in this collection.
        /// </returns>

        public IEnumerable<IDomObject> QueryIndex(ushort[] subKey, int depth, bool includeDescendants)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Clears this object to its blank/initial state.
        /// </summary>

        public void Clear()
        {
            return;
        }

        /// <summary>
        /// The number of unique index keys.
        /// </summary>
        ///
        /// <returns>
        /// The count of items in the index.
        /// </returns>

        public int Count
        {
            get
            {
                return 0;
            }
        }

        /// <summary>
        /// Returns the features that this index implements.
        /// </summary>

        public DomIndexFeatures Features
        {
            get { return 0; }
        }

        /// <summary>
        /// When true, changes are queued until the next read operation. For the DomIndexNone provider, this is always false.
        /// </summary>

        public bool QueueChanges
        {
            get
            {
                return false;
            }
            set
            {
                return;
            }
        }


    }
}