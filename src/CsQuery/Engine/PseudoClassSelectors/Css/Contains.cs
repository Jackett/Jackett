using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsQuery.ExtensionMethods.Internal;

namespace CsQuery.Engine.PseudoClassSelectors
{
    /// <summary>
    /// Matches elements that have children containing the specified text.
    /// </summary>
    ///
    /// <url>
    /// http://api.jquery.com/contains-selector/
    /// </url>

    public class Contains : PseudoSelectorFilter
    {
        /// <summary>
        /// Return elements from the selection that contain the text in the parameter
        /// </summary>
        ///
        /// <param name="selection">
        /// A sequence of elements
        /// </param>
        ///
        /// <returns>
        /// The elements from the sequence that contain the text
        /// </returns>

        public override IEnumerable<IDomObject> Filter(IEnumerable<IDomObject> selection)
        {
            foreach (IDomObject el in selection)
            {
                if (ContainsText((IDomElement)el, Parameters[0]))
                {
                    yield return el;
                }
            }
        }

        /// <summary>
        /// Test whether a single element contains the text passed in the selector's parameter
        /// </summary>
        ///
        /// <param name="element">
        /// The element to test.
        /// </param>
        ///
        /// <returns>
        /// true if it contains the text, false if not.
        /// </returns>

        public override bool Matches(IDomObject element)
        {
            return element is IDomContainer ?
                ContainsText(element, Parameters[0]) :
                false;
        }

        private bool ContainsText(IDomObject source, string text)
        {
            foreach (IDomObject e in source.ChildNodes)
            {
                if (e.NodeType == NodeType.TEXT_NODE)
                {
                    if (((IDomText)e).NodeValue.IndexOf(text) >= 0)
                    {
                        return true;
                    }
                }
                else if (e.NodeType == NodeType.ELEMENT_NODE)
                {
                    if (ContainsText((IDomElement)e, text))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// The maximum number of parameters that this selector can accept (1)
        /// </summary>
        ///
        /// <value>
        /// An integer.
        /// </value>

        public override int MaximumParameterCount
        {
            get
            {
                return 1;
            }
        }

        /// <summary>
        /// The minimum number of parameters that this selector requires (1)
        /// </summary>
        ///
        /// <value>
        /// An integer.
        /// </value>

        public override int MinimumParameterCount
        {
            get
            {
                return 1;
            }
        }

        /// <summary>
        /// A value to determine how to parse the string for a parameter at a specific index.
        /// </summary>
        ///
        /// <param name="index">
        /// Zero-based index of the parameter.
        /// </param>
        ///
        /// <returns>
        /// Always returns OptionallyQuoted
        /// </returns>

        protected override QuotingRule ParameterQuoted(int index)
        {
            return QuotingRule.OptionallyQuoted;
        }
        
    }
}
