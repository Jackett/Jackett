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
        /// Remove all child nodes of the set of matched elements from the DOM.
        /// </summary>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/empty/
        /// </url>

        public CQ Empty()
        {

            return Each((IDomObject e) =>
            {
                if (e.HasChildren)
                {
                    e.ChildNodes.Clear();
                }
            });
        }

        

    }
}
