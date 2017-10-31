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
        /// Get the children of each element in the set of matched elements, including text and comment
        /// nodes.
        /// </summary>
        ///
        /// <returns>
        /// A new CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/contents/
        /// </url>

        public CQ Contents()
        {

            List<IDomObject> list = new List<IDomObject>();
            foreach (IDomObject obj in SelectionSet)
            {
                if (obj is IDomContainer)
                {
                    list.AddRange(obj.ChildNodes);
                }
            }

            return NewInstance(list, this);
        }

        

    }
}
