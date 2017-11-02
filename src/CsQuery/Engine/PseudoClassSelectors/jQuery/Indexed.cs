using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.Engine.PseudoClassSelectors
{
    /// <summary>
    /// Base class for jQuery filters that test whether an element appears at the specified position with the list.
    /// </summary>

    public abstract class Indexed : PseudoSelector, IPseudoSelectorFilter
    {
        private int _Index;
        private bool IndexParsed;

        /// <summary>
        /// The zero-based index for which to test.
        /// </summary>

        protected int Index
        {
            get
            {
                if (!IndexParsed)
                {
                    if (!int.TryParse(Parameters[0], out _Index))
                    {
                        throw new ArgumentException(String.Format("The {0} selector requires a single integer parameter.", Name));
                    }
                    IndexParsed = true;
                }
                return _Index;
            }
        }

        /// <summary>
        /// The maximum number of parameters that this selector can accept (1)
        /// </summary>
        ///
        /// <value>
        /// An integer.
        /// </value>

        public override int  MaximumParameterCount
        {
	        get 
	        { 
		         return 1;
	        }
        }

        /// <summary>
        /// The minimum number of parameters that this selector requires (1)
        /// </summary>
        ///
        /// <value>
        /// An integer.
        /// </value>

        public override int  MinimumParameterCount
        {
	        get 
	        { 
		         return 1;
	        }
        }

        /// <summary>
        /// Abstract implementation of the Filter method for the Index filter.
        /// </summary>
        ///
        /// <param name="selection">
        /// The sequence of elements prior to this filter being applied.
        /// </param>
        ///
        /// <returns>
        /// A sequence of matching elements.
        /// </returns>

        public abstract IEnumerable<IDomObject> Filter(IEnumerable<IDomObject> selection);
    }
}
