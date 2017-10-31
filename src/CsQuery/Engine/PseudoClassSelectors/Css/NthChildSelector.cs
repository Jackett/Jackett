using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.Engine.PseudoClassSelectors
{
    /// <summary>
    /// Base class for all nth-child type pseudoclass selectors
    /// </summary>

    public abstract class NthChildSelector: PseudoSelector, IPseudoSelectorChild
    {
        private NthChildMatcher _NthC;

        /// <summary>
        /// NthChildMatcher object for use by inherited classes
        /// </summary>
        ///
        /// <value>
        /// An instance of the NthChildMatcher support class
        /// </value>

        protected NthChildMatcher NthC
        {
            get
            {
                if (_NthC == null)
                {
                    _NthC = new NthChildMatcher();
                }
                return _NthC;
            }
        }

        /// <summary>
        /// Test whether this element matches the selector implementation.
        /// </summary>
        ///
        /// <param name="element">
        /// The object.
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        public abstract bool Matches(IDomObject element);

        /// <summary>
        /// Return a sequence of all children matching the selector implementation.
        /// </summary>
        ///
        /// <param name="element">
        /// The parent element.
        /// </param>
        ///
        /// <returns>
        /// A sequence of children that match.
        /// </returns>

        public abstract IEnumerable<IDomObject> ChildMatches(IDomContainer element);

        /// <summary>
        /// The minimum number of parameters that this selector requires (1)
        /// </summary>
        ///
        /// <value>
        /// An integer.
        /// </value>

        public override int MinimumParameterCount
        {
            get
            {
                return 1;
            }
        }

        /// <summary>
        /// The maximum number of parameters that this selector can accept (1)
        /// </summary>
        ///
        /// <value>
        /// An integer.
        /// </value>

        public override int MaximumParameterCount
        {
            get
            {
                return 1;
            }
        }

    }
}
