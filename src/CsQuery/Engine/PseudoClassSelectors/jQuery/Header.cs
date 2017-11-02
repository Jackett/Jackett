using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.Engine.PseudoClassSelectors
{
    /// <summary>
    /// The jQuery ":header" selector
    /// </summary>

    public class Header: PseudoSelectorFilter
    {
        /// <summary>
        /// Test whether an element is a header (H1-H6)
        /// </summary>
        ///
        /// <param name="element">
        /// The element to test.
        /// </param>
        ///
        /// <returns>
        /// true if it matches, false if not.
        /// </returns>

        public override bool Matches(IDomObject element)
        {
            var nodeName = element.NodeName;
            return nodeName[0] == 'H'
                && nodeName.Length == 2
                && nodeName[1] >= '0'
                && nodeName[1] <= '6';
        }
    }
}
