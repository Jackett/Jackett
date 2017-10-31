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
        /// Create a deep copy of the set of matched elements. Clone makes copies of the actual elements
        /// in a selection set; it doesn't simply make a copy of a selector's results.
        /// </summary>
        ///
        /// <returns>
        /// A new CQ object that contains a clone of each element in the original selection set.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/clone/
        /// </url>

        public CQ Clone()
        {
            CQ csq = NewCqInDomain();
            csq.Document = Document.CreateNew();

            foreach (var item in SelectionSet)
            {
                csq.Document.ChildNodes.Add(item.Clone());
            }

           csq.SelectionSet = new SelectionSet<IDomObject>(csq.Document.ChildNodes.ToList(), 
               Order, 
               Order);
            return csq;
        }
    }
}
