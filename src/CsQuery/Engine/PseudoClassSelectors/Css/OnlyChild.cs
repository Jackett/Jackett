using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsQuery.ExtensionMethods.Internal;

namespace CsQuery.Engine.PseudoClassSelectors
{
    /// <summary>
    /// Mathches elements that are the the first child of a parent
    /// </summary>
    ///
    /// <url>
    /// http://reference.sitepoint.com/css/pseudoclass-firstchild
    /// </url>

    public class OnlyChild : PseudoSelectorChild
    {
        /// <summary>
        /// Test whether an element is the only child of its parent
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
            return OnlyChildOrNull(element.ParentNode) == element;
        }

        /// <summary>
        /// Return the only child of the parent element, or nothing if there are zero or more than one
        /// children.
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
            IDomObject child = OnlyChildOrNull(element);
            if (child != null)
            {
                yield return child;
            }
        }

        private IDomObject OnlyChildOrNull(IDomObject parent)
        {
            if (parent.NodeType == NodeType.DOCUMENT_NODE)
            {
                return null;
            }
            else
            {
                return parent.ChildElements.SingleOrDefaultAlways();
            }
        }
    }
}
