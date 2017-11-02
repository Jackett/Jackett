using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.Engine.PseudoClassSelectors
{
    /// <summary>
    /// Return only odd-numbered elements from the selection
    /// </summary>

    public class First : PseudoSelector, IPseudoSelectorFilter
    {
        /// <summary>
        /// Filter the sequence, returning only the first element.
        /// </summary>
        ///
        /// <param name="selection">
        /// A sequence of elements
        /// </param>
        ///
        /// <returns>
        /// The first element in the sequence, or an empty sequence if the original sequence is empty.
        /// </returns>

        public IEnumerable<IDomObject> Filter(IEnumerable<IDomObject> selection)
        {
            var first = selection.FirstOrDefault();
            if (first != null)
            {
                yield return first;
            } 
        }
    }
}
