using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsQuery.Utility;
using CsQuery.StringScanner;
using CsQuery.StringScanner.Patterns;
using CsQuery.ExtensionMethods.Internal;
using CsQuery.HtmlParser;
using CsQuery.Implementation;

namespace CsQuery.Engine
{
    /// <summary>
    /// Helper methods to perform matching against attribute-type selectors
    /// </summary>

    public static class AttributeSelectors
    {
        /// <summary>
        /// Test whether a single element matches a specific attribute selector.
        /// </summary>
        ///
        /// <param name="element">
        /// The element to test.
        /// </param>
        /// <param name="selector">
        /// The selector.
        /// </param>
        ///
        /// <returns>
        /// true if the element matches, false if not.
        /// </returns>

        public static bool Matches(IDomElement element, SelectorClause selector)
        {

            string value;
            bool match = ((DomElement)element).TryGetAttributeForMatching(selector.AttributeNameTokenID,out value);

            if (!match)
            {
                switch (selector.AttributeSelectorType)
                {
                    case AttributeSelectorType.Exists:
                        return false;
                    case AttributeSelectorType.NotEquals:
                    case AttributeSelectorType.NotExists:
                        return true;
                    default:
                        return false;
                }
            }
            else
            {
               // bool isCaseSensitive = HtmlData.
                switch (selector.AttributeSelectorType)
                {
                    case AttributeSelectorType.Exists:
                        return true;
                    case AttributeSelectorType.Equals:
                        return selector.AttributeValue.Equals(value,selector.AttributeValueStringComparison);

                    case AttributeSelectorType.StartsWith:
                        return value != null &&
                            value.Length >= selector.AttributeValue.Length &&
                            value.Substring(0, selector.AttributeValue.Length)
                                .Equals(selector.AttributeValue, selector.AttributeValueStringComparison);

                    case AttributeSelectorType.Contains:
                        return value != null && value.IndexOf(selector.AttributeValue,
                            selector.AttributeValueStringComparison)>=0;

                    case AttributeSelectorType.ContainsWord:
                        return value != null && ContainsWord(value, selector.AttributeValue, 
                                                           selector.AttributeValueStringComparer);

                    case AttributeSelectorType.NotEquals:
                        return !selector.AttributeValue
                                .Equals(value, selector.AttributeValueStringComparison);

                    case AttributeSelectorType.NotExists:
                        return false;

                    case AttributeSelectorType.EndsWith:
                        int len = selector.AttributeValue.Length;
                        return value != null && value.Length >= len &&
                            value.Substring(value.Length - len)
                                .Equals(selector.AttributeValue, 
                                        selector.AttributeValueStringComparison);

                    case AttributeSelectorType.StartsWithOrHyphen:
                        if (value == null)
                        {
                            return false;
                        }
                        int dashPos = value.IndexOf("-");
                        string beforeDash = value;

                        if (dashPos >= 0)
                        {
                            // match a dash that's included in the match attribute according to common browser behavior
                            beforeDash = value.Substring(0, dashPos);
                        }

                        return selector.AttributeValue.Equals(beforeDash,selector.AttributeValueStringComparison) || 
                            selector.AttributeValue.Equals(value,selector.AttributeValueStringComparison);

                    default:
                        throw new InvalidOperationException("No AttributeSelectorType set");

                }

            }
        }

        /// <summary>
        /// Test whether a sentence contains a word.
        /// </summary>
        ///
        /// <param name="sentence">
        /// The sentence.
        /// </param>
        /// <param name="word">
        /// The word.
        /// </param>
        /// <param name="comparer">
        /// The comparer.
        /// </param>
        ///
        /// <returns>
        /// true if it contains the word, false if not.
        /// </returns>

        private static bool ContainsWord(string sentence, string word, StringComparer comparer)
        {
            
            HashSet<string> words = new HashSet<string>(
                sentence.Trim().Split(CharacterData.charsHtmlSpaceArray, 
                    StringSplitOptions.RemoveEmptyEntries));

            return words.Contains(word, comparer);
        }
       
    }
}
