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
        /// Check the current matched set of elements against a selector and return true if at least one
        /// of these elements matches the selector.
        /// </summary>
        ///
        /// <param name="selector">
        /// A string containing a selector expression to match elements against.
        /// </param>
        ///
        /// <returns>
        /// true if at least one element in the selection set matches.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/is/
        /// </url>

        public bool Is(string selector)
        {
            return Filter(selector).Length > 0;
        }

        /// <summary>
        /// Check the current matched set of elements against a sequence of elements, or another CQ
        /// object, and return true if at least one of these elements matches the selector.
        /// </summary>
        ///
        /// <param name="elements">
        /// A sequence of elements or a CQ object to match against the current selection set.
        /// </param>
        ///
        /// <returns>
        /// true if the sequence matches, false if it fails.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/is/
        /// </url>

        public bool Is(IEnumerable<IDomObject> elements)
        {
            HashSet<IDomObject> els = new HashSet<IDomObject>(elements);
            els.IntersectWith(SelectionSet);
            return els.Count > 0;
        }

        /// <summary>
        /// Check the current matched set of elements against an element, and return true if the element
        /// is found within the selection set.
        /// </summary>
        ///
        /// <param name="element">
        /// An element to match against the current selection set.
        /// </param>
        ///
        /// <returns>
        /// true if it is found, false if it fails.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/is/
        /// </url>

        public bool Is(IDomObject element)
        {
            return SelectionSet.Contains(element);
        }
        

    }
}
