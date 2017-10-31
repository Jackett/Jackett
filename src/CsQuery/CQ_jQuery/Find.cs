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
        /// Get the descendants of each element in the current set of matched elements, filtered by a
        /// selector.
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
        /// http://api.jquery.com/find/
        /// </url>

        public CQ Find(string selector)
        {
            return FindImpl(new Selector(selector));
        }

        /// <summary>
        /// Get the descendants of each element in the current set of matched elements, filtered by a
        /// sequence of elements or CQ object.
        /// </summary>
        ///
        /// <param name="elements">
        /// The elements to match against.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/find/
        /// </url>

        public CQ Find(IEnumerable<IDomObject> elements)
        {
            return FindImpl(new Selector(elements));
        }

        /// <summary>
        /// Get a single element, if it is a descendant of the current selection set.
        /// </summary>
        ///
        /// <param name="element">
        /// The element to matc.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/find/
        /// </url>

        public CQ Find(IDomObject element)
        {
            return FindImpl(new Selector(element));
        }

        #endregion

        #region private methods

        private CQ FindImpl(Selector selector)
        {
            CQ csq = NewCqInDomain();
            var selection = selector.ToContextSelector().Select(Document, this);

            csq.AddSelection(selection);
            csq.Selector = selector;
            return csq;
        }

        #endregion

    }
}
