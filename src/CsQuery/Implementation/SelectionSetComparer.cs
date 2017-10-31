using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.Implementation
{
    /// <summary>
    /// A comparer to ensure that items are returned from a selection set in DOM order, e.g. by comparing their
    /// internal paths.
    /// </summary>
    public class SelectionSetComparer : IComparer<IDomObject>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        ///
        /// <exception cref="InvalidOperationException">
        /// Thrown when the requested operation is invalid.
        /// </exception>
        ///
        /// <param name="order">
        /// The order used to compare two items. This must be Ascending or Descending
        /// </param>

        public SelectionSetComparer(SelectionSetOrder order)
        {
            if (order != SelectionSetOrder.Ascending && order != SelectionSetOrder.Descending)
            {
                throw new InvalidOperationException("This comparer can only be used to sort.");
            }
            Order = order;
        }

        private SelectionSetOrder Order;

        /// <summary>
        /// Compares two IDomObject objects to determine their relative ordering.
        /// </summary>
        ///
        /// <param name="x">
        /// I dom object to be compared.
        /// </param>
        /// <param name="y">
        /// I dom object to be compared.
        /// </param>
        ///
        /// <returns>
        /// Negative if 'x' is less than 'y', 0 if they are equal, or positive if it is greater.
        /// </returns>

        public int Compare(IDomObject x, IDomObject y)
        {
            return Order == SelectionSetOrder.Ascending ?
                PathKeyComparer.Comparer.Compare(x.NodePath, y.NodePath) :
                PathKeyComparer.Comparer.Compare(y.NodePath, x.NodePath);
        }
    }
}
