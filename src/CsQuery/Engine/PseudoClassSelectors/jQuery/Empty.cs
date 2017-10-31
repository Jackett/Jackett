using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.Engine.PseudoClassSelectors
{
    /// <summary>
    /// Test whether an element is empty. Elements that contain text nodes with empty or null values
    /// are considered empty.
    /// </summary>

    public class Empty : PseudoSelectorFilter
    {
        /// <summary>
        /// Test whether the element is empty
        /// </summary>
        ///
        /// <param name="element">
        /// The element to test.
        /// </param>
        ///
        /// <returns>
        /// true if it has no non-whitespace children, false if not
        /// </returns>

        public override bool Matches(IDomObject element)
        {
            // try to optimize this by checking for the least labor-intensive things first
            if (!element.HasChildren)
            {
                return true;
            }
            else
            {
                return IsEmpty(element);
            }
        }



        /// <summary>
        /// Test whether an element contains no non-empty children. An element can technically have
        /// children, but if they are text nodes with empty values, then it's considered empty.
        /// </summary>
        ///
        /// <param name="element">
        /// The element to test
        /// </param>
        ///
        /// <returns>
        /// true if an element is empty, false if not.
        /// </returns>

        public static bool IsEmpty(IDomObject element)
        {
            return !element.ChildNodes
                   .Where(item => item.NodeType == NodeType.ELEMENT_NODE ||
                       (item.NodeType == NodeType.TEXT_NODE &&
                       !String.IsNullOrEmpty(item.NodeValue)))
                   .Any();
        }


    }
}
