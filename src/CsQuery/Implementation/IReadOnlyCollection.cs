using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;

namespace CsQuery.Implementation
{
    /// <summary>
    /// Interface for read only collection.
    /// </summary>
    ///
    /// <typeparam name="T">
    /// Generic type parameter.
    /// </typeparam>

    public interface IReadOnlyCollection<T> : IEnumerable<T>, IEnumerable
    {
        /// <summary>
        /// Gets the number of items in the collection. 
        /// </summary>

        int Count {get;}
    }
}
