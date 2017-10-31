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
        /// Insert every element in the set of matched elements to the beginning of the target.
        /// </summary>
        ///
        /// <param name="target">
        /// One or more HTML strings that will be targeted.
        /// </param>
        ///
        /// <returns>
        /// A CQ object containing all the elements added
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/prependTo/
        /// </url>

        public CQ PrependTo(params string[] target)
        {

            CQ output;
            NewInstance(MergeSelections(target)).Prepend(SelectionSet, out output);

            return output;
        }

        /// <summary>
        /// Insert every element in the set of matched elements to the beginning of the target.
        /// </summary>
        ///
        /// <param name="targets">
        /// The targets to which the current selection will be appended.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object representing the target elements.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/prependTo/
        /// </url>

        public CQ PrependTo(IEnumerable<IDomObject> targets)
        {
            CQ output;
            EnsureCsQuery(targets).Prepend(SelectionSet, out output);
            return output;
        }
        

    }
}
