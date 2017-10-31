using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.Engine.PseudoClassSelectors
{
    /// <summary>
    /// Pseudoclass selector for :lang. This is not currently implemented. The problem with :lang is
    /// that it is based on an inherited property value. This messes with the index since elements
    /// will be pre-filtered by an attribute selector. This could be implemented using a pseudoclass
    /// type construct instead, e.g. as "visible" that traverses through parents, and inherits a
    /// default document-wide setting.
    /// </summary>

    public class Lang: PseudoSelectorChild
    {
        /// <summary>
        /// Test whether an element matches this selector.
        /// </summary>
        ///
        /// <exception cref="NotImplementedException">
        /// Thrown when the requested operation is unimplemented.
        /// </exception>
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
           
            //StartNewSelector(SelectorType.Attribute);
            //Current.AttributeSelectorType = AttributeSelectorType.StartsWithOrHyphen;
            //Current.TraversalType = TraversalType.Inherited;
            //Current.AttributeName = "lang";

            //Current.Criteria = scanner.GetBoundedBy('(', false);
            //break;
            // 
            throw new NotImplementedException(":lang is not currently implemented.");
        }
    }
}
