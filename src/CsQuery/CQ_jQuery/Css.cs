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
using CsQuery.StringScanner;

namespace CsQuery
{
    public partial class CQ
    {

        /// <summary>
        /// Set one or more CSS properties for the set of matched elements from JSON data.
        /// </summary>
        ///
        /// <param name="map">
        /// An object whose properties names represent css property names, or a string that is valid JSON
        /// data that represents an object of css style names/values.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/css/#css2
        /// </url>

        public CQ CssSet(object map)
        {
            IDictionary<string, object> dict;
            if (Objects.IsJson(map))
            {
                dict = ParseJSON<IDictionary<string, object>>((string)map);
            }
            else
            {
                dict = Objects.ToExpando(map);
            }
            foreach (IDomElement e in Elements)
            {
                foreach (var item in dict)
                {
                    e.Style[item.Key] = StringOrNull(item.Value);
                }
            }
            return this;
        }

        /// <summary>
        /// Set one or more CSS properties for the set of matched elements.
        /// </summary>
        ///
        /// <remarks>
        /// By default, this method will validate that the CSS style name and value are valid CSS3. To
        /// assing a style without validatoin, use the overload of this method and set the "strict"
        /// parameter to false.
        /// </remarks>
        ///
        /// <param name="name">
        /// The name of the style.
        /// </param>
        /// <param name="value">
        /// The value of the style.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/css/#css2
        /// </url>

        public CQ Css(string name, IConvertible value)
        {
            string style = String.Empty;

            foreach (IDomElement e in Elements)
            {
                e.Style[name] = StringOrNull(value);
            }
            return this;
        }

        /// <summary>
        /// Get the value of a style property for the first element in the set of matched elements, and
        /// converts to a numeric type T. Any numeric type strings are ignored when converting to numeric
        /// values.
        /// </summary>
        ///
        /// <typeparam name="T">
        /// The type. This should probably be a numeric type, but the method will attempt to convert to
        /// any IConvertible type passed.
        /// </typeparam>
        /// <param name="style">
        /// The name of the CSS style to retrieve.
        /// </param>
        ///
        /// <returns>
        /// A value of type T.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/css/#css1
        /// </url>

        public T Css<T>(String style) where T : IConvertible
        {
            IDomElement el = FirstElement();
            if (el == null)
            {
                return default(T);
            }


            if (Objects.IsNumericType(typeof(T)))
            {
                IStringScanner scanner = Scanner.Create(el.Style[style] ?? "");
                T num;
                if (scanner.TryGetNumber<T>(out num))
                {
                    return num;
                }
                else
                {
                    return default(T);
                }
            }
            else
            {
                return (T)Objects.ChangeType(el.Style[style] ?? "", typeof(T));
            }
        }

        /// <summary>
        /// Get the value of a style property for the first element in the set of matched elements.
        /// </summary>
        ///
        /// <param name="style">
        /// The name of the CSS style.
        /// </param>
        ///
        /// <returns>
        /// A string of the value of the named CSS style.
        /// </returns>

        public string Css(string style)
        {
            IDomElement el = FirstElement();
            string def = null;
            if (el != null)
            {
                def = el.Style[style];
                switch (style)
                {
                    case "display":
                        if (String.IsNullOrEmpty(def))
                        {
                            def = el.IsBlock ? "block" : "inline";
                        }
                        break;
                    case "opacity":
                        if (String.IsNullOrEmpty(def))
                        {
                            def = "1";
                        }
                        break;
                }
            }
            return def;

        }

        private string StringOrNull(object value)
        {

            if (value==null) {
                return null;
            } else {
                string text = value.ToString();
                return text=="" ? 
                    null : 
                    text;
            }
        }

    }
}
