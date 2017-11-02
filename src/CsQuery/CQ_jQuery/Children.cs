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
        /// Get the children of each element in the set of matched elements, optionally filtered by a
        /// selector.
        /// </summary>
        ///
        /// <param name="filter">
        /// A selector that must match each element returned.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/children/
        /// </url>

        public CQ Children(string filter = null)
        {
            return FilterIfSelector(filter, SelectionChildren());
        }


        /// <summary>
        /// Return all children of all selected elements. Helper method for Children()
        /// </summary>
        ///
        /// <returns>
        /// A new sequence.
        /// </returns>

        protected IEnumerable<IDomObject> SelectionChildren()
        {
            foreach (IDomObject obj in Elements)
            {
                foreach (IDomObject child in obj.ChildElements)
                {
                    yield return child;
                }
            }
        }

    }
}
