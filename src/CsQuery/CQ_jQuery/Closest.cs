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
        /// Get the first ancestor element that matches the selector, beginning at the current element
        /// and progressing up through the DOM tree.
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
        /// http://api.jquery.com/closest/#closest1
        /// </url>

        public CQ Closest(string selector)
        {
            CQ matchTo = Select(selector);
            return Closest(matchTo);
        }

        /// <summary>
        /// Return the element passed by parameter, if it is an ancestor of any elements in the selection
        /// set.
        /// </summary>
        ///
        /// <param name="element">
        /// The element to target.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/closest/#closest1
        /// </url>

        public CQ Closest(IDomObject element)
        {
            return Closest(Objects.Enumerate(element));
        }

        /// <summary>
        /// Get the first ancestor element of any element in the seleciton set that is also one of the
        /// elements in the sequence passed by parameter, beginning at the current element and
        /// progressing up through the DOM tree.
        /// </summary>
        ///
        /// <param name="elements">
        /// The elements to target.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/closest/#closest1
        /// </url>

        public CQ Closest(IEnumerable<IDomObject> elements)
        {
            // Use a hashset to operate faster - since we already have one for the selection set anyway
            
            HashSet<IDomObject> elementSet = new HashSet<IDomObject>(elements); 

            CQ csq = NewCqInDomain();

            foreach (var el in SelectionSet)
            {
                var search = el;
                while (search != null)
                {
                    if (elementSet.Contains(search))
                    {
                        csq.AddSelection(search);
                        search=null;
                        continue;
                    }
                    search = search.ParentNode;
                }

            }
            return csq;
        }
    }
}
