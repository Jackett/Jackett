using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsQuery.Utility;
using CsQuery.ExtensionMethods;
using CsQuery.ExtensionMethods.Internal;
using CsQuery.Engine;
using CsQuery.Implementation;
using CsQuery.HtmlParser;

namespace CsQuery
{
    public partial class CQ
    {

        #region public methods

        /// <summary>
        /// Get the current value of the first element in the set of matched elements, and try to convert
        /// to the specified type.
        /// </summary>
        ///
        /// <typeparam name="T">
        /// The type to which the value should be converted.
        /// </typeparam>
        ///
        /// <returns>
        /// A value or object of type T.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/val/#val1
        /// </url>

        public T Val<T>()
        {
            string val = Val();
            return Objects.Convert<T>(val);
        }

        /// <summary>
        /// Gets the current value of the first element in the selection set, converted to the specified
        /// type, or if the selection set is empty, the default value for the specified type.
        /// </summary>
        ///
        /// <typeparam name="T">
        /// The type to which the value should be converted.
        /// </typeparam>
        ///
        /// <returns>
        /// A value or object of type T.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/val/#val1
        /// </url>
        
        public T ValOrDefault<T>()
        {
            string val = Val();
            T outVal;
            if (Objects.TryConvert<T>(val, out outVal))
            {
                return outVal;
            }
            else
            {
                return (T)Objects.DefaultValue(typeof(T));
            }
        }

        /// <summary>
        /// Get the current value of the first element in the set of matched elements. When using Val()
        /// to access an OPTION group with the "multiple" flag set, this method with return a comma-
        /// separated string (rather than the array returned by jQuery) of each selected option. When
        /// there is no "value" property on an option, the text returned for the value of each selected
        /// option is the inner text of the OPTION element.
        /// </summary>
        ///
        /// <returns>
        /// A string of the value.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/val/#val1
        /// </url>

        public string Val()
        {
            if (Length > 0)
            {
                IDomElement e = this.Elements.First();
                switch (e.NodeNameID)
                {
                    case HtmlData.tagTEXTAREA:
                        return e.Value;
                    case HtmlData.tagINPUT:
                        string val = e.GetAttribute("value", String.Empty);
                        switch (e.GetAttribute("type", String.Empty))
                        {
                            case "radio":
                            case "checkbox":
                                if (String.IsNullOrEmpty(val))
                                {
                                    val = "on";
                                }
                                break;
                            default:
                                break;
                        }
                        return val;
                    case HtmlData.tagSELECT:
                        string result = String.Empty;
                      
                        var sel = (HTMLSelectElement)e;

                        if (!sel.Multiple)
                        {
                            return sel.Value;
                        }
                        else
                        {
                            var selList = sel.ChildElementsOfTag<IHTMLOptionElement>(HtmlData.tagOPTION);
                            result = String.Join(",", selList
                                .Where(item => item.HasAttribute("selected") && !item.Disabled)
                                .Select(item => item.Value ?? item.TextContent));
                            return result;
                        }
                        
                    case HtmlData.tagOPTION:
                        val = e.GetAttribute("value");
                        return val ?? e.TextContent;
                    default:
                        return e.GetAttribute("value", String.Empty);
                }
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Set the value of each element in the set of matched elements. If a comma-separated value is
        /// passed to a multiple select list, then it will be treated as an array.
        /// </summary>
        ///
        /// <param name="value">
        /// A string of text or an array of strings corresponding to the value of each matched element to
        /// set as selected/checked.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/val/#val2
        /// </url>

        public CQ Val(object value)
        {
            bool first = true;
            string val = GetValueString(value);
            foreach (IDomElement e in Elements)
            {
                switch (e.NodeNameID)
                {
                    case HtmlData.tagTEXTAREA:
                        // should we delete existing children first? they should not exist
                        e.TextContent = val;
                        break;
                    case HtmlData.tagINPUT:
                        switch (e.GetAttribute("type", String.Empty))
                        {
                            case "checkbox":
                            case "radio":
                                if (first)
                                {
                                    SetOptionSelected(Elements, value, true);
                                }
                                break;
                            default:
                                e.SetAttribute("value", val);
                                break;
                        }
                        break;
                    case HtmlData.tagSELECT:
                        if (first)
                        {
                            var multiple = e.HasAttribute("multiple");
                            SetOptionSelected(e.ChildElements, value, multiple);
                        }
                        break;
                    default:
                        e.SetAttribute("value", val);
                        break;
                }
                first = false;

            }
            return this;
        }

        #endregion

        #region private methods

        /// <summary>
        /// Returns: null if the value is null; if it's sequence, the concatenated string of each
        /// object's ToString(); or finally the object itself its string representation if not a string.
        /// </summary>
        ///
        /// <param name="value">
        /// The object to process
        /// </param>
        ///
        /// <returns>
        /// The value string.
        /// </returns>

        protected string GetValueString(object value)
        {
            return value == null ? null :
                (value is string ? 
                    (string)value :
                    (value is IEnumerable ?
                        Objects.Join((IEnumerable)value) : 
                        value.ToString()
                    )
                );
        }

        #endregion
    }
}
