using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery
{
    /// <summary>
    /// Interface for a CSS style rule.
    /// </summary>
    ///
    /// <url>
    /// http://www.w3.org/TR/DOM-Level-2-Style/css.html#CSS-CSSStyleRule
    /// </url>

    public interface ICSSStyleRule: ICSSRule
    {
        /// <summary>
        /// The textual representation of the selector for the rule set. The implementation may have
        /// stripped out insignificant whitespace while parsing the selector.
        /// </summary>

        string SelectorText { get; set; }

        /// <summary>
        /// The declaration-block of this rule set.
        /// </summary>

        ICSSStyleDeclaration Style { get; }
    }
}
