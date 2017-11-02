using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsQuery.Utility;
using CsQuery.ExtensionMethods;
using CsQuery.ExtensionMethods.Internal;
using CsQuery.Engine;

namespace CsQuery
{
    public partial class CQ
    {

        /// <summary>
        /// Get the ancestors of each element in the current set of matched elements, optionally filtered
        /// by a selector.
        /// </summary>
        ///
        /// <param name="filter">
        /// (optional) a selector which limits the elements returned.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/parents/
        /// </url>

        public CQ Parents(string filter = null)
        {
            return ParentsUntil((string)null, filter);
        }

        private IEnumerable<IDomElement> ParentsImpl(IEnumerable<IDomObject> source, HashSet<IDomElement> until)
        {

            HashSet<IDomElement> alreadyAdded = new HashSet<IDomElement>();

            foreach (var item in source)
            {
                IDomElement parent = item.ParentNode as IDomElement;
                while (parent != null && !until.Contains(parent))
                {
                    if (alreadyAdded.Add(parent))
                    {
                        yield return parent;
                    }
                    else
                    {
                        break;
                    }

                    parent = parent.ParentNode as IDomElement;
                }
            }
        }
    }
}
