using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.Engine
{
    /// <summary>
    /// A base class for filter-type selectors that implements a simple iterator function and calls
    /// Matches for each element. Classes that depend on the element's position in the filtered list
    /// cannot use this and should implement IPseudoSelectorFilter directly.
    /// </summary>

    public abstract class PseudoSelectorFilter: PseudoSelector, IPseudoSelectorFilter
    {
        /// <summary>
        /// Test whether an element matches this selector.
        /// </summary>
        ///
        /// <param name="element">
        /// The element to test.
        /// </param>
        ///
        /// <returns>
        /// true if it matches, false if not.
        /// </returns>

        public abstract bool Matches(IDomObject element);

        /// <summary>
        /// Basic implementation of ChildMatches, runs the Matches method against each child. This should
        /// be overridden with something more efficient if possible. For example, selectors that inspect
        /// the element's index could get their results more easily by picking the correct results from
        /// the list of children rather than testing each one.
        /// </summary>
        ///
        /// <param name="elements">
        /// The parent element.
        /// </param>
        ///
        /// <returns>
        /// A sequence of children that match.
        /// </returns>

        public virtual IEnumerable<IDomObject> Filter(IEnumerable<IDomObject> elements)
        {
            return elements.Where(item => Matches(item));
        }
    }

}
