using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery
{
    /// <summary>
    /// Interface for icss rule.
    /// </summary>
    ///
    /// <url>
    /// http://www.w3.org/TR/DOM-Level-2-Style/css.html#CSS-CSSRule
    /// </url>

    public interface ICSSRule
    {
        /// <summary>
        /// Gets the type of rule.
        /// </summary>

        CSSRuleType Type { get; }

        /// <summary>
        /// The parsable textual representation of the rule. This reflects the current state of the rule
        /// and not its initial value.
        /// </summary>

        string CssText {get; set;}

        /// <summary>
        /// The style sheet that contains this rule.
        /// </summary>

        ICSSStyleSheet ParentStyleSheet { get; }

        /// <summary>
        /// If this rule is contained inside another rule (e.g. a style rule inside an @media block),
        /// this is the containing rule. If this rule is not nested inside any other rules, this returns
        /// null.
        /// </summary>

        ICSSRule ParentRule { get; }
    }
}
