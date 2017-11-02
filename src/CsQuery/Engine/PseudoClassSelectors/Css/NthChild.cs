using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.Engine.PseudoClassSelectors
{
    /// <summary>
    /// Matches elements on the basis of their positions within a parent element’s list of child element
    /// </summary>
    ///
    /// <url>
    /// http://reference.sitepoint.com/css/pseudoclass-nthchild
    /// </url>

    public class NthChild: NthChildSelector
    {
        /// <summary>
        /// Test whether this element is an nth child of its parent where values of n are calculate from
        /// the formula passed as a parameter to the :nth-child(n) selector.
        /// </summary>
        ///
        /// <param name="element">
        /// The object.
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        public override bool Matches(IDomObject element)
        {
            return element.NodeType != NodeType.ELEMENT_NODE ? false :
                NthC.IsNthChild((IDomElement)element,Parameters[0],false);
        }

        /// <summary>
        /// Return a sequence of all children of the parent element that are nth children
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
            return NthC.NthChilds(element,Parameters[0],false);
        }

       

    }
}
