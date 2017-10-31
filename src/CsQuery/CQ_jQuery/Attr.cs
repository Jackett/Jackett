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

        /// <summary>
        /// Get the value of an attribute for the first element in the set of matched elements.
        /// </summary>
        ///
        /// <param name="name">
        /// The name of the attribute to get.
        /// </param>
        ///
        /// <returns>
        /// A string of the attribute value, or null if the attribute does not exist.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/attr/#attr1
        /// </url>

        public string Attr(string name)
        {
            if (Length > 0 && !string.IsNullOrEmpty(name))
            {
                name = name.ToLower();

                string value;
                var el = this[0];
                switch (name)
                {
                    case "class":
                        return el.ClassName;
                    case "style":
                        return el.Style.ToString();
                    default:
                        if (el.TryGetAttribute(name, out value))
                        {
                            if (HtmlData.IsBoolean(name))
                            {
                                // Pre-1.6 and 1.6.1+ compatibility: always return the name of the attribute if it exists for
                                // boolean attributes
                                return name;
                            }
                            else
                            {

                                return value;
                            }
                        }
                        else if (name == "value" &&
                          (el.NodeName == "INPUT" || el.NodeName == "SELECT" || el.NodeName == "OPTION"))
                        {
                            return Val();
                        }
                        else if (name == "value" && el.NodeName == "TEXTAREA")
                        {
                            return el.TextContent;
                        }
                        break;
                }

            }
            return null;
        }

        /// <summary>
        /// Get the value of an attribute for the first element in the set of matched elements.
        /// </summary>
        ///
        /// <remarks>
        /// This is a CsQuery extension. Attribute values are always stored as strings internally, in
        /// line with their being created and represented as HTML string data. This method simplifies
        /// converting to another type such as integer for attributes that represent strongly-type values.
        /// </remarks>
        ///
        /// <typeparam name="T">
        /// Type to which the attribute value should be converted.
        /// </typeparam>
        /// <param name="name">
        /// The name of the attribute to get.
        /// </param>
        ///
        /// <returns>
        /// A strongly-typed value representing the attribute, or default(T) if the attribute does not
        /// exist.
        /// </returns>

        public T Attr<T>(string name)
        {
            string value;
            if (Length > 0 && this[0].TryGetAttribute(name, out value))
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            return default(T);
        }

        /// <summary>
        /// Set one or more attributes for the set of matched elements.
        /// </summary>
        ///
        /// <exception cref="InvalidOperationException">
        /// Thrown when attemting to change the type of an INPUT element that already exists on the DOM.
        /// </exception>
        ///
        /// <param name="name">
        /// THe attribute name.
        /// </param>
        /// <param name="value">
        /// The value to set.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>

        public CQ Attr(string name, IConvertible value)
        {

            // Make sure attempts to pass a JSON string end up a the right place
            
            if (Objects.IsJson(name) && value is bool)
            {
                return AttrSet(name, (bool)value);
            }

            // jQuery 1.7 compatibility
            bool isBoolean = HtmlData.IsBoolean(name);
            if (isBoolean)
            {
                // Using attr with empty string should set a property to "true. But prop() itself requires a truthy value. Check for this specifically.
                if (value is string && (string)value == String.Empty)
                {
                    value = true;
                }
                SetProp(name, value);
                return this;
            }

            string val;
            if (value is bool)
            {
                val = value.ToString().ToLower();
            }
            else
            {
                val = GetValueString(value);
            }

            foreach (IDomElement e in Elements)
            {
                if ((e.NodeNameID == HtmlData.tagINPUT || e.NodeNameID == HtmlData.tagBUTTON) && name == "type"
                    && !e.IsFragment)
                {
                    throw new InvalidOperationException("Can't change type of \"input\" elements that have already been added to a DOM");
                }
                e.SetAttribute(name, val);
            }
            return this;
        }

        /// <summary>
        /// Map an object to a set of attributes name/values and set those attributes on each object in
        /// the selection set.
        /// </summary>
        ///
        /// <remarks>
        /// The jQuery API uses the same method "Attr" for a wide variety of purposes. For Attr and Css
        /// methods, the overloads that we would like to use to match all the ways the method is used in
        /// the jQuery API don't work out in the strongly-typed world of C#. To resolved this, the
        /// methods AttrSet and CssSet were created for methods where an object or a string of JSON are
        /// passed (a map) to set multiple methods.
        /// </remarks>
        ///
        /// <param name="map">
        /// An object whose properties names represent attribute names, or a string that is valid JSON
        /// data that represents an object of attribute names/values.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/attr/#attr2
        /// </url>

        public CQ AttrSet(object map)
        {
            return AttrSet(map, false);
        }

        /// <summary>
        /// Map an object to attributes, optionally using "quickSet" to set other properties in addition
        /// to the attributes.
        /// </summary>
        ///
        /// <param name="map">
        /// An object whose properties names represent attribute names, or a string that is valid JSON
        /// data that represents an object of attribute names/values.
        /// </param>
        /// <param name="quickSet">
        /// If true, set any css from a sub-map object passed with "css", html from "html", inner text
        /// from "text", and css from "width" and "height" properties.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>

        public CQ AttrSet(object map, bool quickSet = false)
        {
            IDictionary<string, object> dict;

            string cssString = map as string;
            if (cssString != null && Objects.IsJson((cssString)))
            {
                dict = ParseJSON<IDictionary<string, object>>(cssString);
            }
            else
            {
                dict = Objects.ToExpando(map);
            }

            foreach (IDomElement el in Elements)
            {
                foreach (var kvp in dict)
                {
                    if (quickSet)
                    {
                        string name = kvp.Key.ToLower();
                        switch (name)
                        {
                            case "css":
                                Select(el).CssSet(Objects.ToExpando(kvp.Value));
                                break;
                            case "html":
                                Select(el).Html(kvp.Value.ToString());
                                break;
                            case "height":
                            case "width":
                                // for height and width, do not set attributes - set css
                                Select(el).Css(name, kvp.Value.ToString());
                                break;
                            case "text":
                                Select(el).Text(kvp.Value.ToString());
                                break;
                            default:
                                el.SetAttribute(kvp.Key, kvp.Value.ToString());
                                break;
                        }
                    }
                    else
                    {
                        el.SetAttribute(kvp.Key, kvp.Value.ToString());
                    }
                }
            }
            return this;
        }

        /// <summary>
        /// Remove an attribute from each element in the set of matched elements.
        /// </summary>
        ///
        /// <param name="name">
        /// The attribute name to remove.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/removeAttr/
        /// </url>

        public CQ RemoveAttr(string name)
        {
            foreach (IDomElement e in Elements)
            {
                switch (name)
                {
                    case "class":
                        e.ClassName = "";
                        break;
                    case "style":
                        e.Style = null;
                        break;
                    default:
                        e.RemoveAttribute(name);
                        break;
                }
            }
            return this;
        }

        /// <summary>
        /// Remove a property from the set of matched elements.
        /// </summary>
        ///
        /// <remarks>
        /// In CsQuery, there is no distinction between an attribute and a property. In a real browser
        /// DOM, this method will actually remove a property from an element, causing consequences such
        /// as the inability to set it later. In CsQuery, the DOM is stateless and is simply a
        /// representation of the HTML that created it. This method is included for compatibility, but
        /// causes no special behavior.
        /// </remarks>
        ///
        /// <param name="name">
        /// The property (attribute) name to remove.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/removeProp/
        /// </url>

        public CQ RemoveProp(string name)
        {
            return RemoveAttr(name);
        }
    }
}
