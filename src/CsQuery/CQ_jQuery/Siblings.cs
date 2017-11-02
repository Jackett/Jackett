using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsQuery.Utility;
using CsQuery.ExtensionMethods;
using CsQuery.ExtensionMethods.Internal;
using CsQuery.Engine;
using CsQuery.Implementation;

namespace CsQuery
{
    public partial class CQ
    {
        /// <summary>
        /// Description: Get the siblings of each element in the set of matched elements, optionally
        /// filtered by a selector.
        /// </summary>
        ///
        /// <param name="selector">
        /// A selector used to filter the siblings.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/siblings/
        /// </url>

        public CQ Siblings(string selector = null)
        {

            // Add siblings of each item in the selection except the item itself for that iteration.
            // If two siblings are in the selection set, then all children of their mutual parent should
            // be returned. Otherwise, all children except the item iteself.

            return FilterIfSelector(selector, GetSiblings(SelectionSet), SelectionSetOrder.Ascending);
        }

        /// <summary>
        /// Return all the siblings of each element in the sequence.
        /// </summary>
        ///
        /// <param name="elements">
        /// The elements.
        /// </param>
        ///
        /// <returns>
        /// An enumerator that exposes each sibling of each element passed.
        /// </returns>

        protected IEnumerable<IDomObject> GetSiblings(IEnumerable<IDomObject> elements)
        {
            foreach (var item in elements)
            {
                foreach (var child in item.ParentNode.ChildElements)
                {
                    if (!ReferenceEquals(child, item))
                    {
                        yield return child;
                    }
                }
            }

        }

    }
}
