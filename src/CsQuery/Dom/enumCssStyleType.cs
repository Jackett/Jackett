using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery
{
    /// <summary>
    /// Values that represent data types of CSS Styles.
    /// </summary>

    public enum CSSStyleType: byte
    {
        /// <summary>
        /// A unit 
        /// </summary>
        Unit = 1,

        /// <summary>
        /// An option
        /// </summary>
        Option = 2,

        /// <summary>
        /// A unit and an option.
        /// </summary>
        UnitOption=3,

        /// <summary>
        /// A complex style definition.
        /// </summary>
        Composite = 4,

        /// <summary>
        /// A named color
        /// </summary>
        Color = 5,

        /// <summary>
        /// A font name.
        /// </summary>
        Font = 6,

        /// <summary>
        /// A url.
        /// </summary>
        Url=7,

        /// <summary>
        /// A string of text.
        /// </summary>
        String=8
    }

}
