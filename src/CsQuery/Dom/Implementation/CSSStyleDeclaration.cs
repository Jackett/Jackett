using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using CsQuery.HtmlParser;
using CsQuery.StringScanner;
using CsQuery.ExtensionMethods;
using CsQuery.ExtensionMethods.Internal;

namespace CsQuery.Implementation
{

    /// <summary>
    /// CSS style declaration.
    /// </summary>

    public class CSSStyleDeclaration : IDictionary<string, string>, IEnumerable<KeyValuePair<string, string>>, ICSSStyleDeclaration
    {
        #region constructors

        /// <summary>
        /// Create a new CSSStyleDeclaration object with no styles.
        /// </summary>
        
        public CSSStyleDeclaration()
        {

        }

        /// <summary>
        /// Create a new CSSStyleDeclaration object for the text.
        /// </summary>
        ///
        /// <param name="cssText">
        /// The parsable textual representation of the declaration block (excluding the surrounding curly
        /// braces). Setting this attribute will result in the parsing of the new value and resetting of
        /// all the properties in the declaration block including the removal or addition of properties.
        /// </param>

        public CSSStyleDeclaration(string cssText)
        {
            SetStyles(cssText, true);
        }

        /// <summary>
        /// Create a new CSSStyleDeclaration object for the text.
        /// </summary>
        ///
        /// <param name="cssText">
        /// The parsable textual representation of the declaration block (excluding the surrounding curly
        /// braces). Setting this attribute will result in the parsing of the new value and resetting of
        /// all the properties in the declaration block including the removal or addition of properties.
        /// </param>
        /// <param name="validate">
        /// When true, validate against CSS3 rules.
        /// </param>

        public CSSStyleDeclaration(string cssText, bool validate)
        {
            SetStyles(cssText, validate);
        }

        /// <summary>
        /// Create a new CSSStyleDeclaration object thatis a child of another rule.
        /// </summary>
        ///
        /// <param name="parentRule">
        /// The parent rule.
        /// </param>

        public CSSStyleDeclaration(ICSSRule parentRule)
        {
            ParentRule = parentRule;
        }

        #endregion

        #region private properties

        private IDictionary<ushort, string> _Styles;
        private string _QuickSetValue;

        /// <summary>
        /// Inner dictionary of the styles. Note: The presence of a value for _Styles is used to indicate
        /// that a "style" attribute exists. Therefore any code which accesses Styles should always check
        /// HasStylesAttribute first to avoid creation of the attribute as a side effect of testing it.
        /// 
        /// This is a bit brittle but necessary because of QuickSetValue: we need the Styles dictionary
        /// to be created automatically in order for it to be populated with QuickSetValue when its
        /// access is required.
        /// </summary>

        protected IDictionary<ushort, string> Styles
        {
            get
            {
                if (_Styles == null)
                {
                    _Styles = new Dictionary<ushort, string>();
                    if (QuickSetValue != null)
                    {
                        AddStyles(QuickSetValue, false);
                        QuickSetValue = null;
                    }
                }
                return _Styles;
            }
            set
            {
                _Styles = value;
            }
        }

        /// <summary>
        /// For fast DOM creation - since styles are not indexed or validated. If they are ever accessed
        /// by style name, they will be parsed on demand.
        /// </summary>

        private string QuickSetValue
        {
            get
            {
                return _QuickSetValue;
            }
            set
            {
                bool hadStyleAttribute = HasStyleAttribute;
                _QuickSetValue = value;
                DoOnHasStyleAttributeChanged(HasStyleAttribute);
            }
        }

        #endregion

        #region public properties

        /// <summary>
        /// The CSS rule that contains this declaration block or null if this CSSStyleDeclaration is not
        /// attached to a CSSRule.
        /// </summary>

        public ICSSRule ParentRule { get; protected set; }

        /// <summary>
        /// Event queue for all listeners interested in OnHasStylesChanged events.
        /// </summary>

        public event EventHandler<CSSStyleChangedArgs> OnHasStylesChanged;

        /// <summary>
        /// The number of properties that have been explicitly set in this declaration block.
        /// </summary>

        public int Length
        {
            get
            {
                return HasStyleAttribute ? Styles.Count : 0;
            }
        }

        /// <summary>
        /// The parsable textual representation of the declaration block (excluding the surrounding curly
        /// braces). Setting this attribute will result in the parsing of the new value and resetting of
        /// all the properties in the declaration block including the removal or addition of properties.
        /// </summary>

        public string CssText
        {
            get
            {
                return this.ToString();
            }
            set
            {
                SetStyles(value);
            }
        }

        /// <summary>
        /// True if there is at least one style.
        /// </summary>
        public bool HasStyles
        {
            get
            {
                return HasStyleAttribute && Styles.Count > 0;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this object has a style attribute. (The attribute can be
        /// empty - this can be true while HasStyles is false).
        /// </summary>

        public bool HasStyleAttribute
        {
            get
            {
                return QuickSetValue != null || _Styles != null;
            }
        }
        /// <summary>
        /// Gets the number of styles in this collection.
        /// </summary>

        public int Count
        {
            get { return !HasStyleAttribute ? 0 : Styles.Count; }
        }

        /// <summary>
        /// Gets a value indicating whether this object is read only. For CSSStyleDeclarations, this is
        /// always false.
        /// </summary>

        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Gets the style name (keys) for all the styles in this collection
        /// </summary>

        public ICollection<string> Keys
        {
            get
            {
                List<string> keys = new List<string>();

                if (HasStyleAttribute)
                {
                    foreach (var kvp in Styles)
                    {
                        keys.Add(HtmlData.TokenName(kvp.Key));
                    }
                }

                return keys;
            }
        }      

        /// <summary>
        ///Gets the style name values for all the styles in this collection
        /// </summary>

        public ICollection<string> Values
        {
            get {
                if (HasStyleAttribute)
                {
                    return Styles.Values;
                }
                else
                {
                    return new List<string>();
                }
            }
        }
        /// <summary>
        /// Get or set the named style
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string this[string name]
        {
            get
            {
                return GetStyle(name);
            }
            set
            {
                SetStyle(name, value, true);
            }
        }

        /// <summary>
        /// Get or set the named style, optionally enabling strict mode.
        /// </summary>
        ///
        /// <param name="name">
        /// The named style
        /// </param>
        /// <param name="strict">
        /// When true, validate for CSS3
        /// </param>
        ///
        /// <returns>
        /// The indexed item.
        /// </returns>

        public string this[string name, bool strict]
        {
            set
            {
                SetStyle(name, value, strict);
            }
        }

        /// <summary>
        /// Gets or sets the CSS height.
        /// </summary>

        public string Height
        {
            get
            {
                return GetStyle("height");
            }
            set
            {
                SetStyle("height", value,true);
            }
        }

        /// <summary>
        /// Gets or sets the CSS width.
        /// </summary>

        public string Width
        {
            get
            {
                return GetStyle("width");
            }
            set
            {
                SetStyle("width", value, true);
            }
        }

        #endregion

        #region public methods

        /// <summary>
        /// Create a clone of this CSSStyleDeclaration object bound to the owner passed.
        /// </summary>
        ///
        /// <returns>
        /// CSSStyleDeclaration.
        /// </returns>

        public CSSStyleDeclaration Clone()
        {
            CSSStyleDeclaration clone = new CSSStyleDeclaration();

            if (QuickSetValue != null)
            {
                clone.QuickSetValue = QuickSetValue;
            }
            else
            {
                IDictionary<ushort, string> styles = new Dictionary<ushort, string>();
                foreach (KeyValuePair<ushort, string> kvp in Styles)
                {
                    styles.Add(kvp);
                }
                clone.Styles = styles;
            }
            return clone;
        }

        /// <summary>
        /// Sets all the styles from a single CSS style string. Any existing styles will be erased.
        /// Styles will be validated and an error thrown if an invalid style is attempted.
        /// </summary>
        ///
        /// <param name="styles">
        /// A legal HTML style string.
        /// </param>

        public void SetStyles(string styles)
        {
            SetStyles(styles, true);
        }

        /// <summary>
        /// Sets all the styles from a single CSS style string. Any existing styles will be erased. This
        /// method is used by DomElementFactory (not in strict mode).
        /// </summary>
        ///
        /// <param name="styles">
        /// A legal HTML style string.
        /// </param>
        /// <param name="strict">
        /// When true, the styles will be validated and an error thrown if any are not valid.
        /// </param>

        public void SetStyles(string styles, bool strict)
        {
            _Styles = null;

            if (!strict)
            {
                QuickSetValue = styles;
            }
            else
            {
                AddStyles(styles, strict);
            }
        }

        /// <summary>
        /// Add one or more styles to this element. Unlike SetStyle, existing styles are not affected,
        /// except for existing styles of the same name.
        /// </summary>
        ///
        /// <param name="styles">
        /// The CSS style string
        /// </param>
        /// <param name="strict">
        /// When true, the styles will be validated as CSS3 before adding.
        /// </param>

        public void AddStyles(string styles, bool strict)
        {
            foreach (string style in styles.SplitClean(';'))
            {
                int index = style.IndexOf(":");
                string stName;
                string stValue;
                if (index > 0)
                {
                    stName = style.Substring(0, index).Trim();
                    stValue = style.Substring(index + 1).Trim();
                    if (!strict)
                    {
                        SetRaw(stName, stValue);
                    }
                    else
                    {
                        Add(stName, stValue);
                    }
                }
            }
        }

        /// <summary>
        /// Remove a single named style.
        /// </summary>
        ///
        /// <param name="name">
        /// The name of the style to remove
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        public bool Remove(string name)
        {
            return HasStyleAttribute ?
                Styles.Remove(HtmlData.Tokenize(name)) :
                false;
        }

        /// <summary>
        /// Removes the style from the style descriptor for this element.
        /// </summary>
        ///
        /// <param name="name">
        /// The name.
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails. this can only fail if the style was not present.
        /// </returns>

        public bool RemoveStyle(string name)
        {
            return Remove(name);
        }
        /// <summary>
        /// Add a single style
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Add(string name, string value)
        {
            SetStyle(name, value, true);
        }
        /// <summary>
        /// Remove all styles
        /// </summary>
        public void Clear()
        {
            if (HasStyleAttribute)
            {
                Styles.Clear();
            }
        }
        /// <summary>
        /// Returns true if the named style is defined
        /// </summary>
        /// <param name="styleName"></param>
        /// <returns></returns>
        public bool HasStyle(string styleName)
        {
            return HasStyleAttribute ?
                Styles.ContainsKey(HtmlData.Tokenize(styleName)) :
                false;
        }
        /// <summary>
        /// Sets style setting with no parsing
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void SetRaw(string name, string value)
        {
            bool hadStyleAttribute = HasStyleAttribute;
            Styles[HtmlData.Tokenize(name)] = value;
            DoOnHasStyleAttributeChanged(hadStyleAttribute);
        }

        /// <summary>
        /// Try to get the value of the named style.
        /// </summary>
        ///
        /// <param name="name">
        /// The name of the style
        /// </param>
        /// <param name="value">
        /// [out] The value.
        /// </param>
        ///
        /// <returns>
        /// true if the named style is defined, false if not.
        /// </returns>

        public bool TryGetValue(string name, out string value)
        {
            if (HasStyleAttribute)
            {
                return Styles.TryGetValue(HtmlData.Tokenize(name), out value);
            }
            else
            {
                value = null;
                return false;
            }
        }

        /// <summary>
        /// Gets a style by name
        /// </summary>
        ///
        /// <param name="name">
        /// The style name
        /// </param>
        ///
        /// <returns>
        /// The style, or null if it is not defined.
        /// </returns>

        public string GetStyle(string name)
        {
            string value = null;
            if (HasStyleAttribute) {
                Styles.TryGetValue(HtmlData.Tokenize(name), out value);
            } 
            return value;
        }

        /// <summary>
        /// Sets a named style, validating its format.
        /// </summary>
        ///
        /// <param name="name">
        /// The style name
        /// </param>
        /// <param name="value">
        /// The style value
        /// </param>
        ///
        /// <exception cref="ArgumentException">
        /// Thrown if the style name and value are not valid CSS
        /// </exception>

        public void SetStyle(string name, string value)
        {
            SetStyle(name, value, true);
        }

        /// <summary>
        /// Sets a named style, validating its format.
        /// </summary>
        ///
        /// <exception cref="ArgumentException">
        /// Thrown if the style name and value are not valid CSS
        /// </exception>
        ///
        /// <param name="name">
        /// The style name.
        /// </param>
        /// <param name="value">
        /// The style value.
        /// </param>
        /// <param name="strict">
        /// When true, the styles will be validated and an error thrown if any are not valid.
        /// </param>

        public void SetStyle(string name, string value, bool strict)
        {
            name = Utility.Support.FromCamelCase(name);
            if (value == null)
            {
                Remove(name);
                return;
            }

            value = value.Trim().Replace(";", String.Empty);
            name = name.Trim();
            CssStyle style = null;
            if (!HtmlStyles.StyleDefs.TryGetValue(name, out style))
            {
                if (strict)
                {
                    throw new ArgumentException("The style '" + name + "' is not valid (strict mode)");
                }
            }
            else
            {
                switch (style.Type)
                {
                    case CSSStyleType.UnitOption:
                        if (!style.Options.Contains(value))
                        {
                            try
                            {
                                value = ValidateUnitString(name, value);
                            }
                            catch
                            {
                                throw new ArgumentException("No valid unit data or option provided for attribue '"
                                    + name + "'. Valid options are: " + OptionList(style));
                            }
                        }
                        break;
                    case CSSStyleType.Option:
                        if (!style.Options.Contains(value.Replace("!important", String.Empty)))
                        {
                            throw new ArgumentException("The value '" + value + "' is not allowed for attribute '"
                                + name + "'. Valid options are: " + OptionList(style));
                        }
                        break;
                    case CSSStyleType.Unit:
                        value = ValidateUnitString(name, value);
                        break;
                    default:
                        // TODO: other formatting verification
                        break;
                }
            }
            SetRaw(name, value);
        }

        /// <summary>
        /// Returns the numeric value only of a style, ignoring units
        /// </summary>
        ///
        /// <param name="style">
        /// The style.
        /// </param>
        ///
        /// <returns>
        /// A double, or null if the style did not exist or did not contain a numeric value.
        /// </returns>

        public double? NumberPart(string style)
        {
            string st = GetStyle(style);
            if (st == null)
            {
                return null;
            }
            else
            {
                IStringScanner scanner = Scanner.Create(st);
                string numString;
                if (scanner.TryGet(MatchFunctions.Number(), out numString))
                {
                    double num;
                    if (double.TryParse(numString, out num))
                    {
                        return num;
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Return the formatted string representation of this style, as HTML, or null if there is no
        /// style attribute.
        /// </summary>
        ///
        /// <returns>
        /// A string.
        /// </returns>

        public override string ToString()
        {
            string style = HasStyleAttribute ? "" : null;

            if (HasStyleAttribute)
            {
                if (QuickSetValue != null)
                {
                    return QuickSetValue;
                }
                else
                {
                    bool first = true;
                    foreach (var kvp in Styles)
                    {
                        if (!first)
                        {
                            style += " ";
                        }
                        else
                        {
                            first = false;
                        }

                        style += HtmlData.TokenName(kvp.Key) + ": " + kvp.Value + ";";

                    }
                }
            }

            return style;
        }

        /// <summary>
        /// Return an enumerator that exposes each style name/value pair
        /// </summary>
        ///
        /// <returns>
        /// The enumerator.
        /// </returns>

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return stylesEnumerable().GetEnumerator();
        }
       


        #endregion

        #region private methods

        /// <summary>
        /// Returns the options for this style as a comma-separated list
        /// </summary>
        ///
        /// <param name="style">
        /// The style.
        /// </param>
        ///
        /// <returns>
        /// A comma-separated string
        /// </returns>

        protected string OptionList(CssStyle style)
        {

            string list = "";
            foreach (string item in style.Options)
            {
                list += (list == String.Empty ? String.Empty : ",") + "'" + item + "'";
            }
            return list;

        }

        /// <summary>
        /// Cleans/validates a CSS units string, or throws an error if not possible.
        /// </summary>
        ///
        /// <exception cref="ArgumentException">
        /// Thrown when one or more arguments have unsupported or illegal values.
        /// </exception>
        ///
        /// <param name="name">
        /// The style name.
        /// </param>
        /// <param name="value">
        /// The value to validate
        /// </param>
        ///
        /// <returns>
        /// A parsed string of the value
        /// </returns>

        protected string ValidateUnitString(string name,string value)
        {
            int pos = 0;
            value = value.Trim();
            StringBuilder outVal = new StringBuilder();
            //TODO: this is not comprehensive.
            string type = name=="opacity" ? "" : "px";
            bool important = value.Contains("!important");
            if (important)
                value = value.Replace("!important", String.Empty);

            if (String.IsNullOrEmpty(value))
            {
                return null;
            }
            int len = value.Length;
            char cur;
            while (pos < len)
            {
                cur = value[pos];
                if (!HtmlData.NumberChars.Contains(cur)) break;
                outVal.Append(cur);
                pos++;
            }
            while (pos > len)
            {
                if (value[pos] != ' ') break;
                pos++;
            }
            string remainder = value.Substring(pos).Trim();
            if (remainder != String.Empty)
            {

                if (HtmlData.Units.Contains(remainder))
                {
                    type = remainder;
                }
                else
                {
                    // unknown unit type - this is invalid, and we're in strict mode
                    throw new ArgumentException("Invalid unit data type for attribute, data: '" + value + "'");
                }
            }
            if (outVal.Length == 0)
            {
                throw new ArgumentException("No data provided for attribute, data: '" + value + "'");
            }
            outVal.Append(type);
            if (important)
                outVal.Append(" !important");
            return outVal.ToString();
        }

        private IEnumerable<KeyValuePair<string, string>> stylesEnumerable()
        {
            if (HasStyleAttribute)
            {
                foreach (var kvp in Styles)
                {
                    yield return new KeyValuePair<string, string>(HtmlData.TokenName(kvp.Key).ToLower(), kvp.Value);

                }
            }
        }

        private void DoOnHasStyleAttributeChanged(bool hadStyleAttribute)
        {
            if (hadStyleAttribute != HasStyleAttribute)
            {
                var evt = OnHasStylesChanged;
                if (evt != null)
                {
                    var args = new CSSStyleChangedArgs(HasStyleAttribute);
                    evt(this, args);
                }
            }
        }
        #endregion

        #region interface members
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        bool IDictionary<string, string>.ContainsKey(string key)
        {
            return HasStyleAttribute ?
                Styles.ContainsKey(HtmlData.Tokenize(key)) :
                false;
        }
        void ICollection<KeyValuePair<string, string>>.Add(KeyValuePair<string, string> item)
        {
            Add(item.Key, item.Value);
        }


        bool ICollection<KeyValuePair<string, string>>.Contains(KeyValuePair<string, string> item)
        {
            return HasStyleAttribute ?
                Styles.Contains(new KeyValuePair<ushort, string>(HtmlData.Tokenize(item.Key), item.Value)) :
                false;
        }

        void ICollection<KeyValuePair<string, string>>.CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            if (HasStyleAttribute && HasStyles)
            {
                array = new KeyValuePair<string, string>[Styles.Count];
                int index = 0;
                foreach (var kvp in Styles)
                {
                    array[index++] = new KeyValuePair<string, string>(HtmlData.TokenName(kvp.Key).ToLower(), kvp.Value);
                }
            }
        }

        bool ICollection<KeyValuePair<string, string>>.Remove(KeyValuePair<string, string> item)
        {
            if (HasStyleAttribute)
            {
                var kvp = new KeyValuePair<ushort, string>(HtmlData.Tokenize(item.Key), item.Value);
                return Styles.Remove(kvp);
            }
            else
            {
                return false;
            }
        }

        IEnumerator<KeyValuePair<string, string>> IEnumerable<KeyValuePair<string, string>>.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion        
    }
}
