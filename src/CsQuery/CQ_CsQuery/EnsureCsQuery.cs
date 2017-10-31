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
        /// Return a CsQuery object wrapping the sequence passed, or the object itself if it's already a
        /// CsQuery obect. Unlike "new CsQuery(context)", this will not create a new CsQuery object from
        /// an existing one.
        /// </summary>
        ///
        /// <param name="elements">
        /// A sequence of IDomObject elements.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object when the source is disconnected elements, or the CQ object passed.
        /// </returns>

        public CQ EnsureCsQuery(IEnumerable<IDomObject> elements)
        {
            if (elements is CQ)
            {
                return (CQ)elements;
            }
            else
            {
                var cq = NewCqUnbound();
                ConfigureNewInstance(cq,elements);
                return cq;
            }
        }
    }
}
