using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsQuery.Implementation;

namespace CsQuery.Engine
{
    /// <summary>
    /// Interface for a DOM index that can be queried for a range of elements 
    /// </summary>
    public interface IDomIndexRanged
    {
       
        /// <summary>
        /// Queries the index, returning all matching elements
        /// </summary>
        ///
        /// <param name="subKey">
        /// The sub key.
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

        IEnumerable<IDomObject> QueryIndex(ushort[] subKey, int depth, bool includeDescendants);
    }
}
