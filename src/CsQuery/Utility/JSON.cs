using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using System.Text.RegularExpressions;
using CsQuery.ExtensionMethods;
using CsQuery.ExtensionMethods.Internal;
using System.Reflection;

namespace CsQuery.Utility
{
    /// <summary>
    /// Methods for working with JSON. 
    /// </summary>
    /// 
    public static class JSON
    {
        #region constructor and internal data

        static JSON()
        {
            escapeLookup = new char[127];
            escapeLookup['b'] = (char)8;
            escapeLookup['f'] = (char)12;
            escapeLookup['n'] = (char)10;
            escapeLookup['e'] = (char)13;
            escapeLookup['t'] = (char)9;
            escapeLookup['v'] = (char)11;
            escapeLookup['"'] = '"';
            escapeLookup['\\'] = '\\';

        }
        private static char[] escapeLookup;
       
       
        #endregion

        #region public methods

        /// <summary>
        /// Convert an object to JSON using the default handling of the serializer.
        /// </summary>
        ///
        /// <param name="obj">
        /// The object.
        /// </param>
        ///
        /// <returns>
        /// JSON representation of the object.
        /// </returns>

        public static string ToJSON(object obj)
        {
            JsonSerializer serializer = new JsonSerializer();
            return serializer.Serialize(obj);

        }

        /// <summary>
        /// Parse JSON into a typed object.
        /// </summary>
        ///
        /// <typeparam name="T">
        /// The type of object to reutrn.
        /// </typeparam>
        /// <param name="json">
        /// The JSON string.
        /// </param>
        ///
        /// <returns>
        /// An object of type T populated with the data from the json source
        /// </returns>

        public static T ParseJSON<T>(string json)
        {
            return (T)ParseJSON(json, typeof(T));
        }

        /// <summary>
        /// Parse JSON into a typed object.
        /// </summary>
        ///
        /// <param name="json">
        /// The JSON string
        /// </param>
        /// <param name="type">
        /// The type of object to return
        /// </param>
        ///
        /// <returns>
        /// An object of the specified type
        /// </returns>

        public static object ParseJSON(string json, Type type)
        {
            if (typeof(IDynamicMetaObjectProvider).GetTypeInfo().IsAssignableFrom(type))
            {
                return ParseJSONObject(json);
            }
            else if (Objects.IsNativeType(type))
            {
                return ParseJSONValue(json, type);
            } else {
                IJsonSerializer serializer = new JsonSerializer();
                object output = serializer.Deserialize(json, type);
                return output;
            }
        }

        /// <summary>
        /// Parse a JSON object or nameless JSON value into a dynamic object, or single typed value.
        /// </summary>
        ///
        /// <param name="json">
        /// The JSON string
        /// </param>
        ///
        /// <returns>
        /// An object
        /// </returns>

        public static object ParseJSON(string json)
        {
            if (String.IsNullOrEmpty(json))
            {
                return null;
            } else {
                return ParseJSONValue(json);
            }
        }

        /// <summary>
        /// Parse a single JSON value to a C# value of the specified type., if the
        /// value is another object, an object or array.
        /// </summary>
        ///
        /// <typeparam name="T">
        /// The type of data to return
        /// </typeparam>
        /// <param name="jsonValue">
        /// A string that represents a single nameless JSON value.
        /// </param>
        ///
        /// <returns>
        /// An object of the CLR datatype matching the value.
        /// </returns>
        ///
        /// <exception cref="ArgumentException">
        /// Thrown when the argument was not a valid JSON value.
        /// </exception>

        public static T ParseJSONValue<T>(string jsonValue)
        {
            return (T)ParseJSONValue(jsonValue, typeof(T));
        }

        /// <summary>
        /// Parse a single JSON value to a C# value (string, bool, int, double, datetime) or, if the value is
        /// another object, an object or array.
        /// </summary>
        ///
        /// <exception cref="ArgumentException">
        /// Thrown when the argument was not a valid JSON value
        /// </exception>
        ///
        /// <param name="jsonValue">
        /// A string that represents a single nameless JSON value
        /// </param>
        ///
        /// <returns>
        /// An object of the CLR datatype matching the value
        /// </returns>

        public static object ParseJSONValue(string jsonValue)
        {
            object value;
            if (!TryParseJSONValue(jsonValue,typeof(object), out value))
            {
                throw new ArgumentException("The value '" + jsonValue + "' could not be parsed, it doesn't seem to be something that should be a JSON value");
            }
            return value;
        }

        /// <summary>
        /// Parse a JSON value to a C# CLR object of the type requested.
        /// </summary>
        ///
        /// <exception cref="ArgumentException">
        /// Thrown when the value could not be converted to the specified type
        /// </exception>
        ///
        /// <param name="jsonValue">
        /// The JSON value.
        /// </param>
        /// <param name="type">
        /// The target type.
        /// </param>
        ///
        /// <returns>
        /// An object of the type specfiied.
        /// </returns>

        public static object ParseJSONValue(string jsonValue, Type type)
        {
            object value;
            if (!TryParseJSONValue(jsonValue, typeof(object), out value)) {
                throw new ArgumentException("The value '" + jsonValue + "' could not be parsed to type '" + type.ToString() + "'");
            }
            return value;
        }

        /// <summary>
        /// Parse a JSON value to a C# value into the best matching CLR type for that JSON value type
        /// </summary>
        ///
        /// <param name="jsonValue">
        /// The JSON value.
        /// </param>
        /// <param name="value">
        /// [out] The value.
        /// </param>
        ///
        /// <returns>
        /// true if successful, false if not.
        /// </returns>

        public static bool TryParseJSONValue(string jsonValue, out object value)
        {
            return TryParseJSONValue(jsonValue, typeof(object), out value);
        }


        /// <summary>
        /// Parse a JSON value to a C# value of the type requested.
        /// </summary>
        ///
        /// <param name="jsonValue">
        /// The JSON value.
        /// </param>
        /// <param name="type">
        /// The target type.
        /// </param>
        /// <param name="value">
        /// [out] The value.
        /// </param>
        ///
        /// <returns>
        /// true if successful, false if not
        /// </returns>

        public static bool TryParseJSONValue(string jsonValue, Type type, out object value)
        {
            bool success = false;
            bool isObject = type==typeof(object);

            try {
                // don't try to guess when explicitly requesting a string
                if (type == typeof(string))
                {
                    if (IsJsonString(jsonValue))
                    {
                        value= ParseJsonString(jsonValue);
                    }
                    else
                    {
                        value=jsonValue;
                    }
                    return true;
                }

                if (TryParseJsonValueImpl(jsonValue, out value))
                {
                    value = isObject ?
                        value :
                        Convert.ChangeType(value, type);
                    return true;
                }
                if (!success)
                {
                    // It's not a string, see what we can get out of it
                    int integer;
                    if (int.TryParse(jsonValue, out integer))
                    {
                        value = type.GetTypeInfo().IsEnum ?
                            Enum.Parse(type, integer.ToString()) :
                                isObject ?
                                    integer :
                                    Convert.ChangeType(integer, type);
                        return true;
                    }
                    else
                    {
                        double dbl;
                        if (double.TryParse(jsonValue, out dbl))
                        {
                            value = isObject ?
                                        dbl :
                                        Convert.ChangeType(dbl, type);
                            return true;
                        }
                        else
                        {
                            bool boolean;
                            if (bool.TryParse(jsonValue, out boolean))
                            {
                                value = isObject ?
                                            boolean :
                                            Convert.ChangeType(boolean, type);
                                return true;
                            }
                        }
                    }

                }
            }
            catch(InvalidCastException) {
                value = null;
                return false;
            }
            value = null;
            return false;
            
        }

        /// <summary>
        /// The value represents a JSON date (MS format)
        /// </summary>
        ///
        /// <param name="jsonValue">
        /// The JSON value
        /// </param>
        ///
        /// <returns>
        /// true if JSON date, false if not.
        /// </returns>

        public static bool IsJsonDate(string jsonValue)
        {
            return jsonValue.Length >= 7 && jsonValue.Substring(0, 7) == "\"\\/Date";
        }

        /// <summary>
        /// The value represents a JSON object, e.g. is bounded by curly braces.
        /// </summary>
        ///
        /// <param name="jsonValue">
        /// the JSON value
        /// </param>
        ///
        /// <returns>
        /// true if JSON object, false if not.
        /// </returns>

        public static bool IsJsonObject(string jsonValue)
        {
            return jsonValue != null && jsonValue.StartsWith("{") && jsonValue.EndsWith("}");
        }

        /// <summary>
        /// The value represents a JSON string, e.g. is bounded by double-quotes.
        /// </summary>
        ///
        /// <param name="jsonValue">
        /// The JSON value
        /// </param>
        ///
        /// <returns>
        /// true if JSON string, false if not.
        /// </returns>

        public static bool IsJsonString(string jsonValue)
        {
            return jsonValue.StartsWith("\"") && jsonValue.EndsWith("\"");
        }

        /// <summary>
        /// The value represents a JSON array, e.g. is bounded by square brackets.
        /// </summary>
        ///
        /// <param name="jsonValue">
        /// The JSON value
        /// </param>
        ///
        /// <returns>
        /// true if JSON array, false if not.
        /// </returns>

        public static bool IsJsonArray(string jsonValue)
        {
            return jsonValue.StartsWith("[") && jsonValue.EndsWith("]");
        }
       
        #endregion

        #region private methods

        /// <summary>
        /// Try to parse a JSON value into a value type or, if the value represents an object or array,
        /// an object. This method does not address numeric types, leaving that up to a caller, so that
        /// they can map to specific numeric casts if desired.
        /// </summary>
        ///
        /// <exception cref="ArgumentException">
        /// Thrown when the value was not a valid JSON value.
        /// </exception>
        ///
        /// <param name="jsonValue">
        /// The JSON value
        /// </param>
        /// <param name="value">
        /// [out] the convert and typecast CLR value
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        private static bool TryParseJsonValueImpl(string jsonValue, out object value)
        {
            var obj = jsonValue.Trim();
            if (String.IsNullOrEmpty(obj))
            {
                throw new ArgumentException("No value passed, not a valid json value.");
            }
            else if (obj == "null" || obj == "undefined")
            {
                value = null;
            }
            else if (obj == "{}")
            {
                value = new JsObject();
            }
            else if (IsJsonObject(obj))
            {
                value = ParseJSONObject(obj);
            }
            else if (IsJsonDate(obj))
            {
                value = FromJSDateTime(obj);
            }
            else if (IsJsonString(obj))
            {
                value = ParseJsonString(obj);
            }
            else if (IsJsonArray(obj))
            {
                value = ParseJsonArray(obj);
            }
            else
            {
                value = null;
                return false;
            }
            return true;
        }

        private static DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        private static DateTime FromJSDateTime(string jsDateTime)
        {
            Regex regex = new Regex(@"^""\\/Date\((?<ticks>-?[0-9]+)\)\\/""");

            string ticks = regex.Match(jsDateTime).Groups["ticks"].Value;

            DateTime dt = unixEpoch.AddMilliseconds(Convert.ToDouble(ticks));
            return DateTime.SpecifyKind(dt, DateTimeKind.Utc).ToLocalTime();

        }
        /// <summary>
        /// Deserialize javscript, then transform to an ExpandObject
        /// </summary>
        /// <param name="objectToDeserialize"></param>
        /// <returns></returns>
        private static JsObject ParseJSONObject(string objectToDeserialize)
        {
            JsonSerializer serializer = new JsonSerializer();
            Dictionary<string, object> dict = (Dictionary<string, object>)serializer.Deserialize(objectToDeserialize, typeof(Dictionary<string, object>));

            return Objects.Dict2Dynamic<JsObject>(dict, true);
        }

        private  static string ParseJsonString(string input)
        {

            string obj = input.Substring(1, input.Length - 2);
            StringBuilder output = new StringBuilder();
            int pos = 0;
            while (pos < obj.Length)
            {
                char cur = obj[pos];
                if (cur == '\\')
                {
                    cur = obj[++pos];
                    char unescaped = escapeLookup[(byte)obj[pos]];
                    if (unescaped > 0)
                    {
                        output.Append(unescaped);
                    }
                    else
                    {
                        output.Append(cur);
                    }
                }
                else
                {
                    output.Append(cur);
                }
                pos++;
            }
            return output.ToString();
        }

         private static object ParseJsonArray(string input)
        {
            string obj = input.Substring(1, input.Length - 2);
            List<object> list = new List<object>();
            Type oneType=null;
            bool typed=true;
            string[] elements = obj.Split(new char[] {','},StringSplitOptions.RemoveEmptyEntries);
            for (int i=0;i<elements.Length;i++) {
                string el = elements[i];
                object json = ParseJSONValue(el);
                
                if (i == 0)
                {
                    oneType = json.GetType();
                }
                else if (typed)
                {
                    if (json.GetType() != oneType)
                    {
                        oneType = typeof(object);
                        typed = false;
                    }
                }
                list.Add(json);
            }
            if (typed)
            {
                Type listType = typeof(List<>).GetTypeInfo().MakeGenericType(new Type[] { oneType });
                IList typedList = (IList)Objects.CreateInstance(listType);
                foreach (var item in list)
                {
                    typedList.Add(item);
                }
                return typedList;
            }
            else
            {
                return list;
            }

        }
        #endregion
    }
}
