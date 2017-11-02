using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsQuery.ExtensionMethods.Internal;

namespace CsQuery.Engine.PseudoClassSelectors
{
    /// <summary>
    /// Mathches elements that are the the last child of a parent
    /// </summary>
    ///
    /// <url>
    /// http://reference.sitepoint.com/css/pseudoclass-lastchild
    /// </url>

    public class LastChild: PseudoSelectorChild
    {
        /// <summary>
        /// Test whether an element is the last child of its parent
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

            return element.ParentNode.LastElementChild == element;
        }

        /// <summary>
        /// Return the last child of the parent
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
            IDomElement child = element.LastElementChild;
            if (child != null)
            {
                yield return child;
            }
        }

    }
}
