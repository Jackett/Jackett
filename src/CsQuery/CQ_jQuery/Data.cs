using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using CsQuery.Utility;
using CsQuery.ExtensionMethods;
using CsQuery.ExtensionMethods.Internal;
using CsQuery.Engine;
using CsQuery.Implementation;

namespace CsQuery
{
    public partial class CQ
    {
        /// <summary>
        /// Returns all values at named data store for the first element in the jQuery collection, as set
        /// by data(name, value). Put another way, this method constructs an object based on the names
        /// and values of any attributes starting with "data-".
        /// </summary>
        ///
        /// <returns>
        /// A dynamic object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/data/#data2
        /// </url>

        public IDynamicMetaObjectProvider Data()
        {
            var dataObj = new JsObject();
            IDictionary<string, object> data = dataObj;
            IDomElement obj = FirstElement();
            if (obj != null)
            {

                foreach (var item in obj.Attributes)
                {
                    if (item.Key.StartsWith("data-"))
                    {
                        object value;
                        if (JSON.TryParseJSONValue(item.Value, typeof(object), out value))
                        {
                            data[item.Key.Substring(5)] = value;
                        }
                        else
                        {
                            data[item.Key.Substring(5)] = item.Value;
                        }
                    }
                }
                return dataObj;
            }
            else
            {
                return null;
            }

        }

        /// <summary>
        /// Store arbitrary data associated with the specified element, and render it as JSON on the
        /// element in a format that can be read by the jQuery "Data()" methods.
        /// </summary>
        ///
        /// <param name="key">
        /// The name of the key to associate with this data object.
        /// </param>
        /// <param name="data">
        /// An string to be associated with the key.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/data/#data1
        /// </url>

        public CQ Data(string key, string data)
        {
            foreach (IDomElement e in Elements)
            {
                e.SetAttribute("data-" + key, data);
            }
            return this;
        }

        /// <summary>
        /// Store arbitrary data associated with the specified element, and render it as JSON on the
        /// element in a format that can be read by the jQuery "Data()" methods.
        /// </summary>
        ///
        /// <remarks>
        /// Though the jQuery "Data" methods are designed to read the HTML5 "data-" attributes like the
        /// CsQuery version, jQuery Data keeps its data in an internal data store that is unrelated to
        /// the element attributes. This is not particularly necessary when working in C# since you have
        /// many other framework options for managing data. Rather, this method has been implemented to
        /// simplify passing data back and forth between the client and server. You should be able to use
        /// CsQuery's Data methods to set arbitrary objects as data, and read them directly from the
        /// client using the jQuery data method. Bear and mind that because CsQuery intends to write
        /// every object you assign using "Data" as a JSON string on a "data-" attribute, there's a lot
        /// of conversion going on which will probably have imperfect results if you just try to use it
        /// as a way to attach an object to an element. It's therefore advised that you think of it as a
        /// way to get data to the client primarily.
        /// </remarks>
        ///
        /// <param name="key">
        /// The name of the key to associate with this data object.
        /// </param>
        /// <param name="data">
        /// An string containing properties to be mapped to JSON data.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/data/#data1
        /// </url>

        public CQ Data(string key, object data)
        {
            string json = JSON.ToJSON(data);
            if (JSON.IsJsonString(json))
            {
                json = JSON.ParseJSONValue<string>(json);
            }
            foreach (IDomElement e in Elements)
            {
                e.SetAttribute("data-" + key, json);
            }
            return this;
        }

        /// <summary>
        /// Convert an object to JSON and stores each named property as a data element.
        /// </summary>
        ///
        /// <remarks>
        /// Because of conflicts with the overloaded signatures compared to the jQuery API, the general
        /// Data method that maps an entire object has been implemented as DataSet.
        /// 
        /// Though the jQuery "Data" methods are designed to read the HTML5 "data-" attributes like the
        /// CsQuery version, jQuery Data keeps its data in an internal data store that is unrelated to
        /// the element attributes. This is not particularly necessary when working in C# since you have
        /// many other framwork options for managing data. Rather, this method has been implemented to
        /// simplify passing data back and forth between the client and server. You should be able to use
        /// CsQuery's Data methods to set arbitrary objects as data, and read them directly from the
        /// client using the jQuery data method. Bear and mind that because CsQuery intends to write
        /// every object you assign using "Data" as a JSON string on a "data-" attribute, there's a lot
        /// of conversion going on which will probably have imperfect results if you just try to use it
        /// as a way to attach an object to an element. It's therefore advised that you think of it as a
        /// way to get data to the client primarily.
        /// </remarks>
        ///
        /// <param name="data">
        /// An object containing properties which will be mapped to data attributes.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/data/#data1
        /// </url>

        public CQ DataSet(object data)
        {
            JsObject obj = CQ.ToExpando(data);
            foreach (var kvp in obj)
            {
                Data(kvp.Key, kvp.Value);
            }
            return this;
        }

        /// <summary>
        /// Returns an object or value at named data store for the first element in the jQuery collection,
        /// as set by data(name, value).
        /// </summary>
        ///
        /// <param name="key">
        /// The named key to identify the data, resulting in access to an attribute named "data-{key}".
        /// </param>
        ///
        /// <returns>
        /// An object representing the stored data. This could be a value type, or a POCO with properties
        /// each containing other objects or values, depending on the data that was initially set.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/data/#data2
        /// </url>

        public object Data(string key)
        {
            string data = First().Attr("data-" + key);

            if (!String.IsNullOrEmpty(data))
            {
                object value;
                if (JSON.TryParseJSONValue(data, typeof(object), out value))
                {
                    return value;
                }
            }

            // default to returning the raw string representation if it's not a parseable JSON value
            return data;
        }

        /// <summary>
        /// Returns an object or value at named data store for the first element in the jQuery collection,
        /// as set by data(name, value).
        /// </summary>
        ///
        /// <typeparam name="T">
        /// The type to which to cast the data. This type should match the type used when setting the
        /// data initially, or be a type that is compatible with the JSON data structure stored in the
        /// data attribute.
        /// </typeparam>
        /// <param name="key">
        /// The name of the key to associate with this data object.
        /// </param>
        ///
        /// <returns>
        /// An object of type T.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/data/#data2
        /// </url>

        public T Data<T>(string key)
        {
            string data = First().Attr("data-" + key);

            try
            {
                if (!String.IsNullOrEmpty(data))
                {
                    object value;
                    if (JSON.TryParseJSONValue(data, typeof(T), out value))
                    {
                        return (T)value;
                    }
                }
           
                    return (T)Convert.ChangeType(data, typeof(T));
            }
            catch (FormatException)
            {
                throw new InvalidCastException(String.Format("The value '{0}' can't be cast as type {1}", data,typeof(T).ToString()));
            }
        }

        /// <summary>
        /// Remove all data- attributes from the element.
        /// </summary>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/removeData/
        /// </url>

        public CQ RemoveData()
        {
            return RemoveData((string)null);
        }

        /// <summary>
        /// Remove a previously-stored piece of data identified by a key.
        /// </summary>
        ///
        /// <param name="key">
        /// A string naming the piece of data to delete, or pieces of data if the string has multiple
        /// values separated by spaces.
        /// </param>
        ///
        /// <returns>
        /// THe current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/removeData/
        /// </url>

        public CQ RemoveData(string key)
        {
            foreach (IDomElement el in Elements)
            {
                List<string> toRemove = new List<string>();
                foreach (var kvp in el.Attributes)
                {
                    bool match = String.IsNullOrEmpty(key) ?
                        kvp.Key.StartsWith("data-") :
                        kvp.Key == "data-" + key;
                    if (match)
                    {
                        toRemove.Add(kvp.Key);
                    }
                }
                foreach (string attr in toRemove)
                {
                    el.RemoveAttribute(attr);
                }
            }
            return this;
        }


        /// <summary>
        /// Remove all data from an element.
        /// </summary>
        ///
        /// <param name="keys">
        /// An array or space-separated string naming the pieces of data to delete.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/removeData/
        /// </url>

        public CQ RemoveData(IEnumerable<string> keys)
        {
            foreach (var key in keys)
            {
                RemoveData(key);

            }
            return this;
        }




        /// <summary>
        /// Returns data as a string, with no attempt to parse it from JSON. This is the equivalent of
        /// using the Attr("data-{key}") method.
        /// </summary>
        ///
        /// <param name="key">
        /// The key identifying the data.
        /// </param>
        ///
        /// <returns>
        /// A string.
        /// </returns>

        public string DataRaw(string key)
        {
            return First().Attr("data-" + key);
        }

        /// <summary>
        /// Determine whether an element has any jQuery data associated with it.
        /// </summary>
        ///
        /// <returns>
        /// true if there is any data, false if not.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/jQuery.hasData/
        /// </url>

        public bool HasData()
        {
            foreach (IDomElement el in Elements)
            {
                foreach (var kvp in el.Attributes)
                {
                    if (kvp.Key.StartsWith("data-"))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        
    }
}
