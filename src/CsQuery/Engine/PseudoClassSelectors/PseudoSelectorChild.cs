using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.Engine
{
    /// <summary>
    /// Base class for an Child-type pseudoselector.
    /// </summary>

    public abstract class PseudoSelectorChild: PseudoSelector, IPseudoSelectorChild
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
        /// 
        /// Also note that the default iterator for ChildMatches only passed element (e.g. non-text node)
        /// children. If you wanted to design a filter that worked on other node types, you should
        /// override this to access all children instead of just the elements.
        /// </summary>
        ///
        /// <param name="element">
        /// The parent element.
        /// </param>
        ///
        /// <returns>
        /// A sequence of children that match.
        /// </returns>

        public virtual IEnumerable<IDomObject> ChildMatches(IDomContainer element)
        {
            return element.ChildElements.Where(item => Matches(item));
        }
    }

}
