using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.Engine.PseudoClassSelectors
{
    /// <summary>
    /// nth-of-type pseudo class selector. Returns elements that are the nth of their type among
    /// their siblings.
    /// </summary>
    ///
    /// <url>
    /// http://reference.sitepoint.com/css/pseudoclass-nthoftype
    /// </url>

    public class NthOfType: NthChildSelector
    {
        /// <summary>
        /// Test whether this element is an nth of its type. 
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
                NthC.IsNthChildOfType((IDomElement)element,Parameters[0],false);
        }

        /// <summary>
        /// Return a sequence of all children that are the nth element of their type.
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
            return NthC.NthChildsOfType(element,Parameters[0],false);
        }


    }
}
