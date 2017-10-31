using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.Engine.PseudoClassSelectors
{
    /// <summary>
    /// Nth last of type pseudo-class selector.
    /// </summary>
    ///
    /// <url>
    /// http://reference.sitepoint.com/css/pseudoclass-nthlastoftype
    /// </url>

    public class NthLastOfType: NthChildSelector
    {
        /// <summary>
        /// Tests whether the element is the nth-last-of-type based on the parameter n passed with the selector
        /// </summary>
        ///
        /// <param name="element">
        /// The element.
        /// </param>
        ///
        /// <returns>
        /// true if the element matches.
        /// </returns>

        public override bool Matches(IDomObject element)
        {
            return element.NodeType != NodeType.ELEMENT_NODE ? 
                false :
                NthC.IsNthChildOfType((IDomElement)element,Parameters[0],true);
        }

        /// <summary>
        /// Enumerates all the elements that are the nth-last-of-type
        /// </summary>
        ///
        /// <param name="element">
        /// The parent element.
        /// </param>
        ///
        /// <returns>
        /// A sequence of matching elements
        /// </returns>

        public override IEnumerable<IDomObject> ChildMatches(IDomContainer element)
        {
            return NthC.NthChildsOfType(element,Parameters[0],true);
        }
    }
}
