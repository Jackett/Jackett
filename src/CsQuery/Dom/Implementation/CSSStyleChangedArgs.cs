using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.Implementation
{
    /// <summary>
    /// Arguments for when a style is changed.
    /// </summary>

    public class CSSStyleChangedArgs : EventArgs
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        ///
        /// <param name="hasStyleAttribute">
        /// A value indicating whether this object has styles following the change.
        /// </param>

        public CSSStyleChangedArgs(bool hasStyleAttribute)
        {
            HasStyleAttribute = hasStyleAttribute;
        }
        /// <summary>
        /// Gets a value indicating whether this object has styles following the change.
        /// </summary>

        public bool HasStyleAttribute
        {
            get;
            protected set;
        }
    }
}
