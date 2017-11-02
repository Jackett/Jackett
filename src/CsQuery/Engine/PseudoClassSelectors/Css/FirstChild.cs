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

    public class FirstChild : PseudoSelectorChild
    {
        /// <summary>
        /// Test whether an element is the first child of its parent
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

            return element.ParentNode.FirstElementChild == element;
        }

        /// <summary>
        /// Return the first child of element
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
            IDomObject child = element.FirstElementChild;
            if (child != null)
            {
                yield return child;
            }
        }

    }
}
