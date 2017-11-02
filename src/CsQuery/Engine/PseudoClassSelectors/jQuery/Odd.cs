using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.Engine.PseudoClassSelectors
{
    /// <summary>
    /// Return only odd-numbered elements from the selection
    /// </summary>

    public class Odd : PseudoSelector, IPseudoSelectorFilter
    {
        /// <summary>
        /// Filter the sequence of elements for only those in odd-number ordinal indices
        /// </summary>
        ///
        /// <param name="selection">
        /// The sequence of elements prior to this filter being applied.
        /// </param>
        ///
        /// <returns>
        /// A sequence of matching elements.
        /// </returns>

        public IEnumerable<IDomObject> Filter(IEnumerable<IDomObject> selection)
        {
            int index = 0;
            foreach (var child in selection)
            {
                if (index % 2 != 0)
                {
                    yield return child;
                }
                index++;
            }
        }
    }
}
