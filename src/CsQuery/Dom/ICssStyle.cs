using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery
{
    /// <summary>
    /// A single CSS style definition.
    /// </summary>
    
    public interface ICSSStyle
    {
        /// <summary>
        /// The name of the style
        /// </summary>

        string Name { get; set; }

        /// <summary>
        /// The type of data contained by this style.
        /// </summary>

        CSSStyleType Type { get; set; }

        /// <summary>
        /// Gets or sets a format required by this style
        /// </summary>

        string Format { get; set; }

        /// <summary>
        /// The acceptable options for Option-type styles
        /// </summary>

        HashSet<string> Options { get; set; }

        /// <summary>
        /// A description of this style.
        /// </summary>

        string Description { get; set; }

    }
}
