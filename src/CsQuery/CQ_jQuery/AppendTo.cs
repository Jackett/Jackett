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
        /// Insert every element in the set of matched elements to the end of each element in the targets.
        /// </summary>
        ///
        /// <remarks>
        /// The .Append() and .appendTo() methods perform the same task. The major difference is in the
        /// syntax-specifically, in the placement of the content and target. With .Append(), the selector
        /// expression preceding the method is the container into which the content is inserted. With
        /// .AppendTo(), on the other hand, the content precedes the method, either as a selector
        /// expression or as markup created on the fly, and it is inserted into the target container.
        /// </remarks>
        ///
        /// <param name="target">
        /// A selector that results in HTML to which the selection set will be appended.
        /// </param>
        ///
        /// <returns>
        ///  A CQ object containing all the elements added
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/appendTo/
        /// </url>

        public CQ AppendTo(params string[] target)
        {
            CQ output;
            NewInstance(MergeSelections(target)).Append(SelectionSet, out output);

            return output;

        }

        /// <summary>
        /// Insert every element in the set of matched elements to the end of the target.
        /// </summary>
        ///
        /// <param name="target">
        /// The element to which the elements in the current selection set should be appended.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object containing the target elements.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/appendTo/
        /// </url>

        public CQ AppendTo(IDomObject target)
        {
            return AppendTo(Objects.Enumerate(target));
        }

        /// <summary>
        /// Insert every element in the set of matched elements to the end of the target.
        /// </summary>
        ///
        /// <param name="targets">
        /// The targets to which the current selection will be appended.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object containing the target elements.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/appendTo/
        /// </url>

        public CQ AppendTo(IEnumerable<IDomObject> targets)
        {
            CQ output;
            EnsureCsQuery(targets).Append(SelectionSet, out output);
            return output;
        }

    }
}
