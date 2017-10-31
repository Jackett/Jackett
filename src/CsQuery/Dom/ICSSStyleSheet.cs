using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery
{
    /// <summary>
    /// Interface to a CSS style sheet.
    /// </summary>
    ///
    /// <url>
    /// http://www.w3.org/TR/DOM-Level-2-Style/css.html#CSS-CSSStyleSheet
    /// </url>

    public interface ICSSStyleSheet
    {
        /// <summary>
        /// Indicates whether the style sheet is applied to the document. 
        /// </summary>

        bool Disabled { get; set; }

        /// <summary>
        /// If the style sheet is a linked style sheet, the value of its attribute is its location. For inline style sheets, the value of this attribute is null.
        /// </summary>

        string Href { get; set; }

        // public MediaList Media { get; set; }

        /// <summary>
        /// The node that associates this style sheet with the document. For HTML, this may be the
        /// corresponding LINK or STYLE element.
        /// </summary>

        IDomElement OwnerNode { get; }

        /// <summary>
        /// This specifies the style sheet language for this style sheet. This will always be "text/css"
        /// </summary>

        string Type { get; }

        /// <summary>
        /// Gets the CSS rules for this style sheet.
        /// </summary>

        IList<ICSSRule> CssRules { get; }


    }
}
