using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.Implementation
{
    /// <summary>
    /// A CSS style rule.
    /// </summary>
    ///
    /// <url>
    /// http://www.w3.org/TR/DOM-Level-2-Style/css.html#CSS-CSSStyleRule
    /// </url>

    public class CSSStyleRule: CSSRule, ICSSStyleRule
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        ///
        /// <param name="parentStyleSheet">
        /// The parent style sheet.
        /// </param>
        /// <param name="parentRule">
        /// The parent rule.
        /// </param>

        public CSSStyleRule(ICSSStyleSheet parentStyleSheet, ICSSRule parentRule):
            base(parentStyleSheet,parentRule)
        {
            ParentStyleSheet = parentStyleSheet;
            ParentRule = parentRule;
        }

        /// <summary>
        /// The textual representation of the selector for the rule set. The implementation may have
        /// stripped out insignificant whitespace while parsing the selector.
        /// </summary>

        public string SelectorText
        {
            get;set;
        }

        /// <summary>
        /// The declaration-block of this rule set.
        /// </summary>

        public ICSSStyleDeclaration Style
        {
            get;
            set;
        }

        /// <summary>
        /// The parsable textual representation of the rule. This reflects the current state of the rule
        /// and not its initial value.
        /// </summary>

        public override string CssText
        {
            get
            {
                return SelectorText + " " + Style.ToString();
            }
            set
            {
                int splitPos = value.IndexOf("{");
                if (splitPos > 0)
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}
