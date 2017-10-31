using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.Engine.PseudoClassSelectors
{
    /// <summary>
    /// Return only odd-numbered elements from the selection
    /// </summary>

    public class Even : PseudoSelector, IPseudoSelectorFilter
    {
        /// <summary>
        /// Filter a sequence of elements, returning only the elements at even-numbered ordinal positions
        /// </summary>
        ///
        /// <param name="selection">
        /// The sequence to filter
        /// </param>
        ///
        /// <returns>
        /// A sequence of elements
        /// </returns>

        public IEnumerable<IDomObject> Filter(IEnumerable<IDomObject> selection)
        {
            int index = 0;
            foreach (var child in selection)
            {
                if (index % 2 == 0)
                {
                    yield return child;
                }
                index++;
            }
        }

    }
}
