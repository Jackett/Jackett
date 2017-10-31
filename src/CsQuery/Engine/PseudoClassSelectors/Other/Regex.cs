using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CsQuery.ExtensionMethods;

namespace CsQuery.Engine.PseudoClassSelectors
{
    /// <summary>
    /// Port of James Padolsey's regex jQuery selector: http://james.padolsey.com/javascript/regex-selector-for-jquery/
    /// </summary>

    class RegexExtension : PseudoSelectorFilter
    {
        // original code: 
        // 
        // jQuery.expr[':'].regex = function(elem, index, match) {
        //    var matchParams = match[3].split(','),
        //        validLabels = /^(data|css):/,
        //        attr = {
        //            method: matchParams[0].match(validLabels) ? 
        //                        matchParams[0].split(':')[0] : 'attr',
        //            property: matchParams.shift().replace(validLabels,'')
        //        },
        //        regexFlags = 'ig',
        //        regex = new RegExp(matchParams.join('').replace(/^\s+|\s+$/g,''), regexFlags);
        //    return regex.test(jQuery(elem)[attr.method](attr.property));
        // }

        private enum Modes
        {
            Data = 1,
            Css = 2,
            Attr = 3
        }

        private string Property;
        private Modes Mode;
        private Regex Expression;

        public override bool Matches(IDomObject element)
        {

            switch (Mode)
            {
                case Modes.Attr:
                    return Expression.IsMatch(element[Property] ?? "");
                case Modes.Css:
                    return Expression.IsMatch(element.Style[Property] ?? "");
                case Modes.Data:
                    return Expression.IsMatch(element.Cq().DataRaw(Property) ?? "");
                default:
                    throw new NotImplementedException();
            }
        }

        private void Configure()
        {
            var validLabels = new Regex("^(data|css):");

            if (validLabels.IsMatch(Parameters[0]))
            {
                string[] subParm = Parameters[0].Split(':');
                string methodName = subParm[0];

                if (methodName == "data")
                {
                    Mode = Modes.Data;
                }
                else if (methodName == "css")
                {
                    Mode = Modes.Css;
                }
                else
                {
                    throw new ArgumentException("Unknown mode for regex pseudoselector.");
                }
                Property = subParm[1];
            }
            else
            {
                Mode = Modes.Attr;
                Property = Parameters[0];
            }

            // The expression trims whitespace the same way as the original
            // Trim() would work just as well but left this way to demonstrate
            // the CsQuery "RegexReplace" extension method

            Expression = new Regex(Parameters[1].RegexReplace(@"^\s+|\s+$", ""), RegexOptions.IgnoreCase | RegexOptions.Multiline);
        }


        // We override "Arguments" to do some setup when this selector is first created, rather than
        // parse the arguments for each iteration. This technique should be used universally to parse
        // arguments. Selectors with no arguments by definition should have no instance-specific
        // configuration to do, so there would be no point in overriding this, nor would it be called
        // if no arguments were passed. 

        public override string Arguments
        {
            get
            {
                return base.Arguments;
            }
            set
            {
                base.Arguments = value;
                Configure();
            }
        }

        /// <summary>
        /// Allow but do not require quotes around the parameters for :regex.
        /// </summary>
        ///
        /// <param name="index">
        /// Zero-based index of the parameter.
        /// </param>
        ///
        /// <returns>
        /// OptionallyQuoted.
        /// </returns>

        protected override QuotingRule ParameterQuoted(int index)
        {
            return QuotingRule.OptionallyQuoted;
        }

        public override int MaximumParameterCount
        {
            get
            {
                return 2;
            }
        }
        public override int MinimumParameterCount
        {
            get
            {
                return 2;
            }
        }
        public override string Name
        {
            get
            {
                return "regex";
            }
        }

    }
}
