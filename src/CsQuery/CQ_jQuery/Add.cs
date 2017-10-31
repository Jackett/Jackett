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
        /// Add elements to the set of matched elements from a selector or an HTML fragment.
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
        /// http://api.jquery.com/add/
        /// </url>

        public CQ Add(string selector)
        {
            return Add(Select(selector));
        }

        /// <summary>
        /// Add an element to the set of matched elements.
        /// </summary>
        ///
        /// <param name="element">
        /// The element to add.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/add/
        /// </url>

        public CQ Add(IDomObject element)
        {
            return Add(Objects.Enumerate(element));
        }

        /// <summary>
        /// Add elements to the set of matched elements.
        /// </summary>
        ///
        /// <param name="elements">
        /// The elements to add.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/add/
        /// </url>

        public CQ Add(IEnumerable<IDomObject> elements)
        {
            CQ res = NewInstance(this);
            res.AddSelection(elements);
            return res;
        }

        /// <summary>
        /// Add elements to the set of matched elements from a selector or an HTML fragment.
        /// </summary>
        ///
        /// <param name="selector">
        /// A string representing a selector expression to find additional elements to add to the set of
        /// matched elements.
        /// </param>
        /// <param name="context">
        /// The point in the document at which the selector should begin matching; similar to the context
        /// argument of the $(selector, context) method.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/add/
        /// </url>

        public CQ Add(string selector, IEnumerable<IDomObject> context)
        {
            return Add(Select(selector, context));
        }

        /// <summary>
        /// Add elements to the set of matched elements from a selector or an HTML fragment.
        /// </summary>
        ///
        /// <param name="selector">
        /// A string representing a selector expression to find additional elements to add to the set of
        /// matched elements.
        /// </param>
        /// <param name="context">
        /// The point in the document at which the selector should begin matching; similar to the context
        /// argument of the $(selector, context) method.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/add/
        /// </url>

        public CQ Add(string selector, IDomObject context)
        {
            return Add(Select(selector, context));
        }

    }
}
