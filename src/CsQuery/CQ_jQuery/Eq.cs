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
        /// Reduce the set of matched elements to the one at the specified index.
        /// </summary>
        ///
        /// <param name="index">
        /// The zero-based index within the current selection set to match.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/eq/
        /// </url>

        public CQ Eq(int index)
        {
            if (index < 0)
            {
                index = Length + index - 1;
            }
            if (index >= 0 && index < Length)
            {
                return NewInstance(SelectionSet[index], this);
            }
            else
            {
                return NewCqInDomain();
            }
        }
        

    }
}
