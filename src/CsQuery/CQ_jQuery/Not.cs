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
        /// Remove elements from the set of matched elements.
        /// </summary>
        ///
        /// <param name="selector">
        /// A CSS selector.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/not/
        /// </url>

        public CQ Not(string selector)
        {
            var subSelector = new Selector(selector);
            var notList = subSelector.ToFilterSelector().Select(Document,Selection);

            return Not(notList);
        }

        /// <summary>
        /// Selects all elements except the element passed as a parameter.
        /// </summary>
        ///
        /// <param name="element">
        /// The element to exclude.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/not/
        /// </url>

        public CQ Not(IDomObject element)
        {
            return Not(Objects.Enumerate(element));
        }

        /// <summary>
        /// Selects all elements except those passed as a parameter.
        /// </summary>
        ///
        /// <param name="elements">
        /// The elements to be excluded.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/not/
        /// </url>

        public CQ Not(IEnumerable<IDomObject> elements)
        {
            CQ csq = NewInstance(SelectionSet);
            csq.SelectionSet.ExceptWith(elements);
            csq.Selector = Selector;
            return csq;
        }

    }
}
