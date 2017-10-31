using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.Implementation
{

    /// <summary>
    /// Interface for icss rule.
    /// </summary>
    ///
    /// <url>
    /// http://www.w3.org/TR/DOM-Level-2-Style/css.html#CSS-CSSRule
    /// </url>

    public abstract class CSSRule: ICSSRule
    {
        /// <summary>
        /// Constructor for a CSS rule.
        /// </summary>
        ///
        /// <param name="parentStyleSheet">
        /// The parent style sheet.
        /// </param>
        /// <param name="parentRule">
        /// The parent rule.
        /// </param>

        public CSSRule(ICSSStyleSheet parentStyleSheet, ICSSRule parentRule)
        {
            ParentStyleSheet = parentStyleSheet;
            ParentRule = parentRule;
        }
        /// <summary>
        /// Gets the type of rule.
        /// </summary>

        public CSSRuleType Type
        {
            get;
            set;
        }

        /// <summary>
        /// The parsable textual representation of the rule. This reflects the current state of the rule
        /// and not its initial value.
        /// </summary>

        public abstract string CssText { get; set; }

        /// <summary>
        /// The style sheet that contains this rule.
        /// </summary>
        ///
        /// <value>
        /// The parent style sheet.
        /// </value>

        public ICSSStyleSheet ParentStyleSheet
        {
            get;
            protected set;
        }

        /// <summary>
        /// If this rule is contained inside another rule (e.g. a style rule inside an @media block),
        /// this is the containing rule. If this rule is not nested inside any other rules, this returns
        /// null.
        /// </summary>
        ///
        /// <value>
        /// The parent rule.
        /// </value>

        public ICSSRule ParentRule
        {
            get;
            protected set;
        }
    }
}
