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
        /// Reduce the set of matched elements to those that have a descendant that matches the selector
        /// or DOM element.
        /// </summary>
        ///
        /// <param name="selector">
        /// A valid CSS/jQuery selector.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/has/
        /// </url>

        public CQ Has(string selector)
        {
            var csq = NewCqInDomain();

            foreach (IDomObject obj in SelectionSet)
            {
                if (Select(obj).Find(selector).Length > 0)
                {
                    csq.SelectionSet.Add(obj);
                }
            }
            return csq;
        }

        /// <summary>
        /// Reduce the set of matched elements to those that have the element passed as a descendant.
        /// </summary>
        ///
        /// <param name="element">
        /// The element to match.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/has/
        /// </url>

        public CQ Has(IDomObject element)
        {
            return Has(Objects.Enumerate(element));
        }

        /// <summary>
        /// Reduce the set of matched elements to those that have each of the elements passed as a descendant.
        /// </summary>
        ///
        /// <param name="elements">
        /// The elements to be excluded.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/has/
        /// </url>

        public CQ Has(IEnumerable<IDomObject> elements)
        {
            var csq = NewCqInDomain();
            foreach (IDomObject obj in SelectionSet)
            {
                if (obj.Cq().Find(elements).Length > 0)
                {
                    csq.SelectionSet.Add(obj);
                }
            }
            return csq;
        }

    }
}
