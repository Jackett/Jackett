using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.Engine.PseudoClassSelectors
{
    /// <summary>
    /// Determines whether the target is a parent.
    /// </summary>

    public class Parent: PseudoSelectorFilter
    {
        /// <summary>
        /// Test whether an element is a parent; e.g. has children.
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

            return element.HasChildren ?
                !Empty.IsEmpty(element) : 
                false;
        }


    }
}
