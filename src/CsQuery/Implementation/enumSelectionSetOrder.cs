using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.Implementation
{
    /// <summary>
    /// Orders in which the selection set can be arranged. Ascending and Descending refer to to the
    /// DOM element order.
    /// </summary>

    public enum SelectionSetOrder
    {
        /// <summary>
        /// The items should be returned in the order they were added to the selection set.
        /// </summary>
        OrderAdded = 1,
        /// <summary>
        /// The items should be returned in the order they appear in the DOM.
        /// </summary>
        Ascending = 2,
        /// <summary>
        /// The items should be returned in the reverse order they appear in the DOM.
        /// </summary>
        Descending = 3
    }
}
