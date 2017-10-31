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
        #region public methods

        /// <summary>
        /// Get the immediately preceding sibling of each element in the set of matched elements,
        /// optionally filtered by a selector.
        /// </summary>
        ///
        /// <param name="selector">
        /// A string containing a selector expression to match elements against.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/prev/
        /// </url>

        public CQ Prev(string selector = null)
        {
            return nextPrevImpl(selector, false);
        }

        /// <summary>
        /// Get the immediately following sibling of each element in the set of matched elements. If a
        /// selector is provided, it retrieves the next sibling only if it matches that selector.
        /// </summary>
        ///
        /// <param name="selector">
        /// A string containing a selector expression to match elements against.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/next/
        /// </url>

        public CQ Next(string selector = null)
        {
            return nextPrevImpl(selector, true);
        }

        /// <summary>
        /// Get all following siblings of each element in the set of matched elements, optionally
        /// filtered by a selector.
        /// </summary>
        ///
        /// <param name="filter">
        /// A selector that must match each element returned.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/nextAll/
        /// </url>

        public CQ NextAll(string filter = null)
        {
            return nextPrevAllImpl(filter, true);
        }

        /// <summary>
        /// Get all following siblings of each element up to but not including the element matched by the
        /// selector, optionally filtered by a selector.
        /// </summary>
        ///
        /// <param name="selector">
        /// A selector that must match each element returned.
        /// </param>
        /// <param name="filter">
        /// A selector use to filter each result
        /// </param>
        ///
        /// <returns>
        /// A new CQ object
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/nextUntil/
        /// </url>

        public CQ NextUntil(string selector = null, string filter = null)
        {
            return nextPrevUntilImpl(selector, filter, true);
        }

        /// <summary>
        /// Get all preceding siblings of each element in the set of matched elements, optionally
        /// filtered by a selector.
        /// </summary>
        ///
        /// <param name="filter">
        /// A selector that must match each element returned.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/prevAll/
        /// </url>

        public CQ PrevAll(string filter = null)
        {
            return nextPrevAllImpl(filter, false);
        }

        /// <summary>
        /// Get all preceding siblings of each element up to but not including the element matched by the
        /// selector, optionally filtered by a selector.
        /// </summary>
        ///
        /// <param name="selector">
        /// A selector that must match each element returned.
        /// </param>
        /// <param name="filter">
        /// A selector use to filter each result.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/prevUntil/
        /// </url>

        public CQ PrevUntil(string selector = null, string filter = null)
        {
            return nextPrevUntilImpl(selector, filter, false);
        }

        #endregion

        #region private methods

        private CQ nextPrevImpl(string selector, bool next)
        {
            IEnumerable<IDomElement> siblings;
            SelectionSetOrder order;
            if (next) {
                siblings = Elements.Select(item=>item.NextElementSibling).Where(item=>item != null);
                order = SelectionSetOrder.Ascending;
            } else {
                siblings = Elements.Select(item => item.PreviousElementSibling).Where(item => item != null);
                order = SelectionSetOrder.Descending ;
            }

            return FilterIfSelector(selector,siblings,order);
        }
        private CQ nextPrevAllImpl(string filter, bool next)
        {
            return FilterIfSelector(filter, MapRangeToNewCQ(Elements, (input) =>
            {
                return nextPrevAllImpl(input, next);
            }), next ? SelectionSetOrder.Ascending : SelectionSetOrder.Descending);
        }

        private CQ nextPrevUntilImpl(string selector, string filter, bool next)
        {
            if (string.IsNullOrEmpty(selector))
            {
                return next ? NextAll(filter) : PrevAll(filter);
            }

            HashSet<IDomElement> untilEls = new HashSet<IDomElement>(Select(selector).Elements);
            return FilterIfSelector(filter, MapRangeToNewCQ(Elements, (input) =>
            {
                return nextPrevUntilFilterImpl(input, untilEls, next);
            }), next ? SelectionSetOrder.Ascending : SelectionSetOrder.Descending);
        }

        private IEnumerable<IDomObject> nextPrevAllImpl(IDomObject input, bool next)
        {
            IDomObject item = next ? input.NextElementSibling : input.PreviousElementSibling;
            while (item != null)
            {
                yield return item;
                item = next ? item.NextElementSibling : item.PreviousElementSibling;
            }
        }

        private IEnumerable<IDomObject> nextPrevUntilFilterImpl(IDomObject input, HashSet<IDomElement> untilEls, bool next)
        {
            foreach (IDomElement el in nextPrevAllImpl(input, next))
            {
                if (untilEls.Contains(el))
                {
                    break;
                }
                yield return el;
            }
        }


        #endregion

    }
}
