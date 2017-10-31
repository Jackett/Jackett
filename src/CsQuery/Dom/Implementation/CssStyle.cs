using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.Implementation
{
    /// <summary>
    /// A single CSS style definition.
    /// </summary>

    public class CssStyle : ICSSStyle
    {
        /// <summary>
        /// The name of the style.
        /// </summary>

        public string Name { get; set; }

        /// <summary>
        /// The type of data contained by this style.
        /// </summary>

        public CSSStyleType Type { get; set; }

        /// <summary>
        /// Gets or sets a format required by this style.
        /// </summary>

        public string Format { get; set; }

        /// <summary>
        /// A description of this style.
        /// </summary>

        public string Description { get; set; }

        /// <summary>
        /// The acceptable options for Option-type styles.
        /// </summary>

        public HashSet<string> Options { get; set; }

    }
}
