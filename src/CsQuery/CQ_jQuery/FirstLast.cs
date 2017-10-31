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
        /// Reduce the set of matched elements to the first in the set.
        /// </summary>
        ///
        /// <returns>
        /// A new CQ object containing the first element in the set, or no elements if the source was
        /// empty.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/first/
        /// </url>

        public CQ First()
        {
            return Eq(0);
        }

        /// <summary>
        /// Reduce the set of matched elements to the last in the set.
        /// </summary>
        ///
        /// <returns>
        /// A new CQ object containing the last element in the set, or no elements if the source was
        /// empty.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/last/
        /// </url>

        public CQ Last()
        {
            if (SelectionSet.Count == 0)
            {
                return NewCqInDomain();
            }
            else
            {
                return Eq(SelectionSet.Count - 1);
            }
        }
        

    }
}
