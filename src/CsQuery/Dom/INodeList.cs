using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using CsQuery.Implementation;

namespace CsQuery
{
    /// <summary>
    /// Interface for node list, a read/write collection of nodes.
    /// </summary>

    public interface INodeList: IEnumerable<IDomObject>, IList<IDomObject>, ICollection<IDomObject>
    {
        /// <summary>
        /// The number of nodes in this INodeList
        /// </summary>

        int Length { get; }

        /// <summary>
        /// Get the item at the specified index
        /// </summary>
        ///
        /// <param name="index">
        /// Zero-based index of the item
        /// </param>
        ///
        /// <returns>
        /// An item
        /// </returns>

        IDomObject Item(int index);

        /// <summary>
        /// Event raised when the NodeList changes
        /// </summary>

        event EventHandler<NodeEventArgs> OnChanged;
    }
}
