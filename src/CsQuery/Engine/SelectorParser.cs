using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsQuery.Utility;
using CsQuery.StringScanner;
using CsQuery.StringScanner.Patterns;

namespace CsQuery.Engine
{
    /// <summary>
    /// A class to parse a CSS selector string into a sequence of Selector objects
    /// </summary>
    public class SelectorParser
    {
        #region private properties

        private IStringScanner scanner;
        private Selector Selectors;
        private SelectorClause _Current;
        TraversalType NextTraversalType = TraversalType.All;
        CombinatorType NextCombinatorType = CombinatorType.Root;

        /// <summary>
        /// The currently active selector clause in the selector construction process. If none is active,
        /// a new one is started.
        /// </summary>

        protected SelectorClause Current
        {
            get
            {
                if (_Current == null)
                {
                    _Current = new SelectorClause();
                }
                return _Current;
            }
        } 

        #endregion

        #region public methods

        /// <summary>
        /// Parse the string, and return a sequence of Selector objects
        /// </summary>
        /// <param name="selector"></param>
        /// <returns></returns>
        public Selector Parse(string selector)
        {
            Selectors = new Selector();

            string sel = (selector ?? String.Empty).Trim();

            if (IsHtml(selector))
            {
                Current.Html = sel;
                Current.SelectorType = SelectorType.HTML;
                Selectors.Add(Current);
                return Selectors;
            }
            
            scanner = Scanner.Create(sel);

            while (!scanner.Finished)
            {
                switch (scanner.Current)
                {
                    case '*':
                        StartNewSelector(SelectorType.All);
                        scanner.Next();
                        break;
                    case '<':
                        // not selecting - creating html
                        Current.Html = sel;
                        scanner.End();
                        break;
                    case ':':
                        scanner.Next();
                        string key = scanner.Get(MatchFunctions.PseudoSelector).ToLower();
                        switch (key)
                        {
                            case "input":
                                AddTagSelector("input");
                                AddTagSelector("textarea",true);
                                AddTagSelector("select",true);
                                AddTagSelector("button",true);
                                break;
                            case "text":
                                StartNewSelector(SelectorType.AttributeValue | SelectorType.Tag);
                                Current.Tag = "input";
                                Current.AttributeSelectorType = AttributeSelectorType.Equals;
                                Current.AttributeName = "type";
                                Current.AttributeValue = "text";
                                
                                StartNewSelector(SelectorType.AttributeValue | SelectorType.Tag, CombinatorType.Grouped, Current.TraversalType);
                                Current.Tag = "input";
                                Current.AttributeSelectorType = AttributeSelectorType.NotExists;
                                Current.AttributeName = "type";

                                Current.SelectorType |= SelectorType.Tag;
                            Current.Tag = "input";
                                break;

                            case "checkbox":
                            case "radio":
                            case "button":
                            case "file":
                            case "image":
                            case "password":
                                AddInputSelector(key,"input");
                                break;
                            case "reset":
                            case "submit":
                                AddInputSelector(key);
                                break;
                            case "checked":
                            case "selected":
                            case "disabled":
                                StartNewSelector(SelectorType.AttributeValue);
                                Current.AttributeSelectorType = AttributeSelectorType.Exists;
                                Current.AttributeName = key;
                                break;
                            case "enabled":
                                StartNewSelector(SelectorType.AttributeValue);
                                Current.AttributeSelectorType = AttributeSelectorType.NotExists;
                                Current.AttributeName = "disabled";
                                break;
                        
                            case "first-letter":
                            case "first-line":
                            case "before":
                            case "after":
                                throw new NotImplementedException("The CSS pseudoelement selectors are not implemented in CsQuery.");
                            case "target":
                            case "link":
                            case "hover":
                            case "active":
                            case "focus":
                            case "visited":
                                throw new NotImplementedException("Pseudoclasses that require a browser aren't implemented.");

                            default:
                                if (!AddPseudoSelector(key)) {
                                
                                    throw new ArgumentException("Unknown pseudo-class :\"" + key + "\". If this is a valid CSS or jQuery selector, please let us know.");
                                }
                                break;
                        }
                        break;
                    case '.':
                        StartNewSelector(SelectorType.Class);
                        scanner.Next();
                        Current.Class = scanner.Get(MatchFunctions.CssClassName);
                        break;
                    case '#':

                        scanner.Next();
                        if (!scanner.Finished)
                        {
                            StartNewSelector(SelectorType.ID);
                            Current.ID = scanner.Get(MatchFunctions.HtmlIDValue());
                        }

                        break;
                    case '[':
                        StartNewSelector(SelectorType.AttributeValue);

                        IStringScanner innerScanner = scanner.ExpectBoundedBy('[', true).ToNewScanner();
                        
                        Current.AttributeName = innerScanner.Get(MatchFunctions.HTMLAttribute());
                        innerScanner.SkipWhitespace();

                        if (innerScanner.Finished)
                        {
                            Current.AttributeSelectorType = AttributeSelectorType.Exists;
                        }
                        else
                        {
                            string matchType = innerScanner.Get("=", "^=", "*=", "~=", "$=", "!=","|=");

                            // CSS allows [attr=] as a synonym for [attr]
                            if (innerScanner.Finished)
                            {
                                Current.AttributeSelectorType = AttributeSelectorType.Exists;
                            } 
                            else 
                            {
                                var rawValue = innerScanner.Expect(expectsOptionallyQuotedValue()).ToNewScanner();

                                Current.AttributeValue = rawValue.Finished ? 
                                    "" : 
                                    rawValue.Get(new EscapedString());

                                switch (matchType)
                                {

                                    case "=":
                                        Current.SelectorType |= SelectorType.AttributeValue;
                                        Current.AttributeSelectorType = AttributeSelectorType.Equals;
                                        break;
                                    case "^=":
                                        Current.SelectorType |= SelectorType.AttributeValue;
                                        Current.AttributeSelectorType = AttributeSelectorType.StartsWith;
                                        // attributevalue starts with "" matches nothing
                                        if (Current.AttributeValue == "")
                                        {
                                            Current.AttributeValue = "" + (char)0;
                                        }
                                        break;
                                    case "*=":
                                        Current.SelectorType |= SelectorType.AttributeValue;
                                        Current.AttributeSelectorType = AttributeSelectorType.Contains;
                                        break;
                                    case "~=":
                                        Current.SelectorType |= SelectorType.AttributeValue;
                                        Current.AttributeSelectorType = AttributeSelectorType.ContainsWord;
                                        break;
                                    case "$=":
                                        Current.SelectorType |= SelectorType.AttributeValue;
                                        Current.AttributeSelectorType = AttributeSelectorType.EndsWith;
                                        break;
                                    case "!=":
                                        Current.AttributeSelectorType = AttributeSelectorType.NotEquals;
                                        // must matched manually - missing also validates as notEquals
                                        break;
                                    case "|=":
                                        Current.SelectorType |= SelectorType.AttributeValue;
                                        Current.AttributeSelectorType = AttributeSelectorType.StartsWithOrHyphen;

                                        break;
                                    default:
                                        throw new ArgumentException("Unknown attibute matching operator '" + matchType + "'");
                                }
                            }
                        }

                        break;
                    case ',':
                        FinishSelector();
                        NextCombinatorType = CombinatorType.Root;
                        NextTraversalType = TraversalType.All;
                        scanner.NextNonWhitespace();
                        break;
                    case '+':
                        StartNewSelector(TraversalType.Adjacent);
                        scanner.NextNonWhitespace();
                        break;
                    case '~':
                        StartNewSelector(TraversalType.Sibling);
                        scanner.NextNonWhitespace();
                        break;
                    case '>':
                        StartNewSelector(TraversalType.Child);
                        // This is a wierd thing because if you use the > selector against a set directly, the meaning is "filter" 
                        // whereas if it is used in a combination selector the meaning is "filter for 1st child"
                        //Current.ChildDepth = (Current.CombinatorType == CombinatorType.Root ? 0 : 1);
                        Current.ChildDepth = 1;
                        scanner.NextNonWhitespace();
                        break;
                    case ' ':
                        // if a ">" or "," is later found, it will be overridden.
                        scanner.NextNonWhitespace();
                        NextTraversalType = TraversalType.Descendent;
                        break;
                    default:

                        string tag = "";
                        if (scanner.TryGet(MatchFunctions.HTMLTagSelectorName(), out tag))
                        {
                            AddTagSelector(tag);
                        }
                        else
                        {
                            if (scanner.Index == 0)
                            {
                                Current.Html = sel;
                                Current.SelectorType = SelectorType.HTML;
                                scanner.End();
                            }
                            else
                            {
                                throw new ArgumentException(scanner.LastError);
                            }

                        }

                        break;
                }
            }
            // Close any open selectors
            FinishSelector();
            if (Selectors.Count == 0)
            {
                var empty = new SelectorClause
                {
                    SelectorType = SelectorType.None,
                    TraversalType = TraversalType.Filter
                };
                Selectors.Add(empty);
                
            }
            return Selectors;
        }
        #endregion

        #region private methods

        /// <summary>
        /// Adds a named pseudo selector from the pseudoselector library.
        /// </summary>
        ///
        /// <param name="key">
        /// The pseudoselector name
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        private bool AddPseudoSelector(string key)
        {
            IPseudoSelector pseudoSel;
            if (PseudoSelectors.Items.TryGetInstance(key, out pseudoSel))
            {
                StartNewSelector(SelectorType.PseudoClass);
                Current.PseudoSelector = pseudoSel;

                if (!scanner.Finished && scanner.Current == '(')
                {
                    pseudoSel.Arguments = scanner.GetBoundedBy('(', true);
                }
                else
                {
                    pseudoSel.Arguments = null;
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        /*
         * The "And" combinator is used to create groups of selectors that are kept in the context of an active subselector.
         * e.g. unlike just adding another clause with CombinatorType.Root (or a ","), this joins them but acting as a single
         * selector.
         * */

        private void AddTagSelector(string tagName, bool combineWithPrevious=false) 
        {
            if (!combineWithPrevious) {
                StartNewSelector(SelectorType.Tag);
            } else {
                StartNewSelector(SelectorType.Tag,CombinatorType.Grouped,Current.TraversalType);
            }
            Current.Tag = tagName;
        }

        private void AddInputSelector(string type, string tag=null,bool combineWithPrevious=false)
        {

            if (!combineWithPrevious)
            {
                StartNewSelector(SelectorType.AttributeValue);

            }
            else
            {
                StartNewSelector(SelectorType.AttributeValue, CombinatorType.Grouped, Current.TraversalType);
            }

            if (tag!=null)
            {
                Current.SelectorType |= SelectorType.Tag;
                Current.Tag = tag;
            }

            Current.AttributeSelectorType = AttributeSelectorType.Equals;
            Current.AttributeName = "type";
            Current.AttributeValue = type;

            if (type == "button")
            {
                StartNewSelector(SelectorType.Tag, CombinatorType.Grouped, Current.TraversalType);
                Current.Tag = "button";
            }

        }
        /// <summary>
        /// A pattern for the operand of an attribute selector
        /// </summary>
        /// <returns></returns>
        protected IExpectPattern expectsOptionallyQuotedValue()
        {
            return new OptionallyQuoted("]");
        }

        /// <summary>
        /// Start a new chained filter selector of the specified type.
        /// </summary>
        ///
        /// <param name="selectorType">
        /// The selector type to start.
        /// </param>

        protected void StartNewSelector(SelectorType selectorType)
        {
            StartNewSelector(selectorType, NextCombinatorType, NextTraversalType);
        }

        /// <summary>
        /// Start a new selector that does not yet have a type specified
        /// </summary>
        /// <param name="combinatorType"></param>
        /// <param name="traversalType"></param>
        protected void StartNewSelector(CombinatorType combinatorType, TraversalType traversalType)
        {
            StartNewSelector(0, combinatorType, traversalType);
        }

        /// <summary>
        /// Start a new chained selector that does not yet have a type specified
        /// </summary>
        /// <param name="traversalType"></param>
        protected void StartNewSelector(TraversalType traversalType)
        {
            StartNewSelector(0, NextCombinatorType, traversalType);
        }

        /// <summary>
        /// Close the currently active selector. If it's partial (e.g. a descendant/child marker) then merge its into into the 
        /// new selector created.
        /// </summary>
        /// <param name="selectorType"></param>
        /// <param name="combinatorType"></param>
        /// <param name="traversalType"></param>
        protected void StartNewSelector(SelectorType selectorType,
            CombinatorType combinatorType,
            TraversalType traversalType)
        {

            // if a selector was not finished, do not overwrite the existing combinator & traversal types,
            // as they could have been changed by a descendant or child selector. The exception is when
            // the new selector is an explicit "all" type; we always 

            // a new selector will not have been started if there was an explicit "*" creating an all. However, if there's
            // anything other than a filter, we do actually want 


            if (Current.IsComplete &&
                Current.SelectorType != SelectorType.All || traversalType != TraversalType.Filter)
            {
                    FinishSelector();
                    Current.CombinatorType = combinatorType;
                    Current.TraversalType = traversalType;
                
            }

            Current.SelectorType = selectorType;
        }

        /// <summary>
        /// Finishes any open selector and clears the current selector
        /// </summary>
        protected void FinishSelector()
        {
            if (Current.IsComplete)
            {
                var cur = Current.Clone();
                Selectors.Add(cur);
            }
            Current.Clear();
            NextTraversalType = TraversalType.Filter;
            NextCombinatorType = CombinatorType.Chained;
        }

        /// <summary>
        /// Clear the currently open selector
        /// </summary>
        protected void ClearCurrent()
        {
            _Current = null;
        }

        /// <summary>
        /// Return true of the text appears to be HTML (e.g. starts with a caret)
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public bool IsHtml(string text)
        {
            return !String.IsNullOrEmpty(text) && text[0] == '<';
        }
        #endregion
    }
}
