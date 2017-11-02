using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsQuery.Implementation;
using CsQuery.ExtensionMethods;
using CsQuery.ExtensionMethods.Internal;
using CsQuery.Engine;

namespace CsQuery
{
    public partial class CQ
    {

        /// <summary>
        /// Get the ancestors of each element in the current set of matched elements, up to but not
        /// including any element matched by the selector, optionally filtered by another selector.
        /// </summary>
        ///
        /// <param name="selector">
        /// A string containing a selector expression to match elements against.
        /// </param>
        /// <param name="filter">
        /// (optional) a selector which limits the elements returned.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/parentsUntil/
        /// </url>

        public CQ ParentsUntil(string selector = null, string filter = null)
        {
            return ParentsUntil(Select(selector).Elements,filter);
        }

        /// <summary>
        /// Get the ancestors of each element in the current set of matched elements, up to but not
        /// including the element matched by the selector.
        /// </summary>
        ///
        /// <param name="element">
        /// The element.
        /// </param>
        /// <param name="filter">
        /// (optional) a selector which limits the elements returned.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/parentsUntil/
        /// </url>
        /// 
        public CQ ParentsUntil(IDomElement element, string filter = null)
        {
            return ParentsUntil(Objects.Enumerate(element),filter);
        }

        /// <summary>
        /// Get the ancestors of each element in the current set of matched elements, up to but not
        /// including any element matched by the selector, optionally filtered by another selector.
        /// </summary>
        ///
        /// <param name="elements">
        /// The elements.
        /// </param>
        /// <param name="filter">
        /// (optional) a selector which limits the elements returned.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object.
        /// </returns>

        public CQ ParentsUntil(IEnumerable<IDomElement> elements, string filter = null)
        {

            CQ output = NewCqInDomain();
            HashSet<IDomElement> targets = new HashSet<IDomElement>(elements);

            var filtered = FilterElementsIgnoreNull(ParentsImpl(Selection, targets), filter);
            
            output.SetSelection(filtered,
                SelectionSetOrder.OrderAdded,
                SelectionSetOrder.Descending);

            return output;
        }

       
    }
}
