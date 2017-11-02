using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.Engine.PseudoClassSelectors
{
    /// <summary>
    /// Matches the first element of the same type within its siblings
    /// </summary>
    ///
    /// <url>
    /// http://reference.sitepoint.com/css/pseudoclass-firstoftype
    /// </url>

    public class FirstOfType : PseudoSelectorChild
    {
        /// <summary>
        /// Test whether an element is the first child of its type
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

            return element.ParentNode.ChildElements
              .Where(item => item.NodeNameID == element.NodeNameID)
              .FirstOrDefault() == element;
        }

        /// <summary>
        /// Return all children of the parameter element that are the first child of their type.
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
            HashSet<ushort> Types = new HashSet<ushort>();
            foreach (var child in element.ChildElements)
            {
                if (!Types.Contains(child.NodeNameID))
                {
                    Types.Add(child.NodeNameID);
                    yield return child;
                }
            }
        }

    }
}
