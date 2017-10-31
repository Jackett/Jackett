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
        /// Add the previous set of elements on the stack to the current set.
        /// </summary>
        ///
        /// <returns>
        /// A new CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/andself/
        /// </url>

        public CQ AndSelf()
        {
            var csq = NewInstance(this);
            csq.Order = SelectionSetOrder.Ascending;

            if (CsQueryParent == null)
            {
                return csq;
            }
            else
            {
                csq.SelectionSet.AddRange(CsQueryParent.SelectionSet);
                return csq;
            }
        }
        

    }
}
