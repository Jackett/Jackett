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
        /// Reduce the set of matched elements to those that match the selector or pass the function's
        /// test.
        /// </summary>
        ///
        /// <param name="selector">
        /// A string containing a selector expression to match the current set of elements against.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/filter/
        /// </url>

        public CQ Filter(string selector)
        {
            return NewInstance(FilterElements(SelectionSet, selector));

        }

        /// <summary>
        /// Reduce the set of matched elements to those that matching the element passed by parameter.
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
        /// http://api.jquery.com/filter/
        /// </url>

        public CQ Filter(IDomObject element)
        {
            return Filter(Objects.Enumerate(element));
        }

        /// <summary>
        /// Reduce the set of matched elements to those matching any of the elements in a sequence passed
        /// by parameter.
        /// </summary>
        ///
        /// <param name="elements">
        /// The elements to match.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/filter/
        /// </url>

        public CQ Filter(IEnumerable<IDomObject> elements)
        {
            CQ filtered = NewInstance(this);
            filtered.SelectionSet.IntersectWith(elements);
            return filtered;
        }

        /// <summary>
        /// Reduce the set of matched elements to those that match the selector or pass the function's
        /// test.
        /// </summary>
        ///
        /// <remarks>
        /// This method doesn't offer anything that can't easily be accomplished with a LINQ "where"
        /// query but is included for completeness.
        /// </remarks>
        ///
        /// <param name="function">
        /// A function used as a test for each element in the set.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/filter/
        /// </url>

        public CQ Filter(Func<IDomObject, bool> function)
        {
            CQ output = NewCqInDomain();

            List<IDomObject> filteredList = new List<IDomObject>();
            foreach (IDomObject obj in SelectionSet)
            {
                if (function(obj))
                {
                    filteredList.Add(obj);
                }
            }
            return output.SetSelection(filteredList, Order);
        }

        /// <summary>
        /// Reduce the set of matched elements to those that match the selector or pass the function's
        /// test.
        /// </summary>
        ///
        /// <remarks>
        /// This method doesn't offer anything that can't easily be accomplished with a LINQ "where"
        /// query but is included for completeness.
        /// </remarks>
        ///
        /// <param name="function">
        /// A function used as a test for each element in the set.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/filter/
        /// </url>

        public CQ Filter(Func<IDomObject, int, bool> function)
        {
            CQ output = NewCqInDomain();
            List<IDomObject> filteredList = new List<IDomObject>();

            int index = 0;
            foreach (IDomObject obj in SelectionSet)
            {
                if (function(obj, index++))
                {
                    filteredList.Add(obj);
                }
            }

            return output.SetSelection(filteredList, Order);
        }
        

    }
}
