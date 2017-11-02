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
        /// The first IDomElement (e.g. not text/special nodes) in the selection set, or null if none
        /// exists.
        /// </summary>
        ///
        /// <returns>
        /// An IDomElement object.
        /// </returns>

        public IDomElement FirstElement()
        {

            using (IEnumerator<IDomElement> sequence = Elements.GetEnumerator())
            {
                if (sequence.MoveNext())
                {
                    return sequence.Current;
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
