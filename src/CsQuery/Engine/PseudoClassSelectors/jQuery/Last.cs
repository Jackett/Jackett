using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.Engine.PseudoClassSelectors
{
    /// <summary>
    /// Return only the last element from a selection
    /// </summary>

    public class Last : PseudoSelector, IPseudoSelectorFilter
    {
        /// <summary>
        /// Filter for the last element in the selection set
        /// </summary>
        ///
        /// <param name="selection">
        /// The sequence of elements prior to this filter being applied.
        /// </param>
        ///
        /// <returns>
        /// The last element in the selection.
        /// </returns>

        public IEnumerable<IDomObject> Filter(IEnumerable<IDomObject> selection)
        {
            var last = selection.LastOrDefault();
            if (last != null)
            {
                yield return last;
            } 
        }
    }
}
