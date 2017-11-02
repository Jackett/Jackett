using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.Engine.PseudoClassSelectors
{
    /// <summary>
    /// Return the nth last child using the formula passed by paremter to calculate N.
    /// </summary>
    ///
    /// <url>
    /// http://reference.sitepoint.com/css/pseudoclass-nthlastchild
    /// </url>

    public class NthLastChild: NthChildSelector
    {
        /// <summary>
        /// Test whether this element is an nth child from the end among its siblings
        /// </summary>
        ///
        /// <param name="element">
        /// The element to test
        /// </param>
        ///
        /// <returns>
        /// true if it matches, false if not.
        /// </returns>

        public override bool Matches(IDomObject element)
        {
            return element.NodeType != NodeType.ELEMENT_NODE ? false :
                NthC.IsNthChild((IDomElement)element,Parameters[0],true);
        }

        /// <summary>
        /// Return a sequence of all children of the element that are nth last children.
        /// </summary>
        ///
        /// <param name="element">
        /// The parent element.
        /// </param>
        ///
        /// <returns>
        /// A sequence of children that match.
        /// </returns>

        public override IEnumerable<IDomObject> ChildMatches(IDomContainer element)
        {
            return NthC.NthChilds(element,Parameters[0],true);
        }
    }
}
