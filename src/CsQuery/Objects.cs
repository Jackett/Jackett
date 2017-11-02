using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using System.Reflection;
using System.ComponentModel;
using CsQuery.ExtensionMethods;
using CsQuery.ExtensionMethods.Internal;
using CsQuery.Utility;

namespace CsQuery
{
    /// <summary>
    /// A set of utility functions for testing objects. 
    /// </summary>
    public static class Objects
    {
        #region Constructor
        static Objects()
        {
            IgnorePropertyNames = new HashSet<string>();
            var info = typeof(object).GetTypeInfo().GetMembers();
            foreach (var member in info)
            {
                IgnorePropertyNames.Add(member.Name);

            }

        }

        static HashSet<string> IgnorePropertyNames;

        #endregion

        #region Methods that test object properties


        /// <summary>
        /// Returns true of the type is a generic nullable type OR string
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsNullableType(Type type)
        {
            return type == typeof(string) ||
                (type.GetTypeInfo().IsGenericType && type.GetTypeInfo().GetGenericTypeDefinition() == typeof(Nullable<>));
        }

        /// <summary>
        /// Returns true if the object is a string, and appears to be JSON, e.g. it starts with a single
        /// curly brace.
        /// </summary>
        ///
        /// <param name="obj">
        /// The object to test.
        /// </param>
        ///
        /// <returns>
        /// true if json, false if not.
        /// </returns>

        public static bool IsJson(object obj)
        {
            string text = obj as string;
            return text != null && text.StartsWith("{") && !text.StartsWith("{{");
        }

        /// <summary>
        /// Tests whether an object is a common immutable, specifically, value types, strings, and null.
        /// KeyValuePairs are specifically excluded. (Why?)
        /// </summary>
        ///
        /// <param name="obj">
        /// .
        /// </param>
        ///
        /// <returns>
        /// true if immutable, false if not.
        /// </returns>

        public static bool IsImmutable(object obj)
        {
            return obj == null ||
                obj is string ||
                (obj is ValueType && !(Objects.IsKeyValuePair(obj)));
        }

        /// <summary>
        /// Returns false if this is a value type, null string, or enumerable (but not Extendable)
        /// </summary>
        ///
        /// <param name="obj">
        /// .
        /// </param>
        ///
        /// <returns>
        /// true if extendable type, false if not.
        /// </returns>

        public static bool IsExtendableType(object obj)
        {
            // Want to allow enumerable types since we can treat them as objects. Exclude arrays.
            // This is tricky. How do we know if something should be treated as an object or enumerated? Do both somehow?
            return Objects.IsExpando(obj) || (!IsImmutable(obj) && !(obj is IEnumerable));
        }

        /// <summary>
        /// Returns true when a value is "truthy" using same logic as Javascript.
        ///   null = false; empty string = false; "0" string = true; 0 numeric = false; false boolean =
        ///   false.
        /// </summary>
        ///
        /// <param name="obj">
        /// The object to test.
        /// </param>
        ///
        /// <returns>
        /// true if truthy, false if not.
        /// </returns>

        public static bool IsTruthy(object obj)
        {
            if (obj == null) return false;
            if (obj is string)
            {
                return !String.IsNullOrEmpty((string)obj);
            }
            if (obj is bool)
            {
                return (bool)obj;
            }
            if (Objects.IsNumericType(obj.GetType()))
            {
                // obj is IConvertible if IsNumericType already
                return System.Convert.ToDouble(obj) != 0.0;
            }

            return true;
        }

        /// <summary>
        /// Returns true if the object is a primitive numeric type, that is, any primtive except string
        /// &amp; char.
        /// </summary>
        ///
        /// <param name="type">
        /// The type to test.
        /// </param>
        ///
        /// <returns>
        /// true if numeric type, false if not.
        /// </returns>

        public static bool IsNumericType(Type type)
        {
            Type t = GetUnderlyingType(type);
            return t.GetTypeInfo().IsPrimitive && !(t == typeof(string) || t == typeof(char) || t == typeof(bool));
        }

        /// <summary>
        /// Returns true if the value is a Javascript native type (string, number, bool, datetime)
        /// </summary>
        ///
        /// <param name="type">
        /// The type to test
        /// </param>
        ///
        /// <returns>
        /// true if a Javascript native type, false if not.
        /// </returns>

        public static bool IsNativeType(Type type)
        {
            Type t = GetUnderlyingType(type);
            var ti = t.GetTypeInfo();
            return ti.IsEnum || ti.IsValueType || ti.IsPrimitive || t == typeof(string);
        }

        /// <summary>
        /// Combine elements of an array into a single string, separated by a comma.
        /// </summary>
        ///
        /// <param name="array">
        /// The array to join.
        /// </param>
        ///
        /// <returns>
        /// A string separated by a comma.
        /// </returns>

        public static string Join(Array array)
        {
            return Join(toStringList(array), ",");
        }

        /// <summary>
        /// Combine elements of a sequenceinto a single string, separated by a comma.
        /// </summary>
        ///
        /// <param name="list">
        /// A list of objects.
        /// </param>
        ///
        /// <returns>
        /// A string containging the string representation of each object in the sequence separated by a
        /// comma.
        /// </returns>

        public static string Join(IEnumerable list)
        {
            return Join(toStringList(list), ",");
        }

        /// <summary>
        /// Test if an object is "Expando-like", e.g. is an IDictionary&lt;string,object&gt;.
        /// </summary>
        ///
        /// <param name="obj">
        /// The object to test.
        /// </param>
        ///
        /// <returns>
        /// true if expando, false if not.
        /// </returns>

        public static bool IsExpando(object obj)
        {
            return (obj is IDictionary<string, object>);
        }

        /// <summary>
        /// Test if an object is a an IDictionary&lt;string,object&gt; that is empty.
        /// </summary>
        ///
        /// <param name="obj">
        /// The object to test
        /// </param>
        ///
        /// <returns>
        /// true if empty expando, false if not.
        /// </returns>

        public static bool IsEmptyExpando(object obj)
        {
            return IsExpando(obj) && ((IDictionary<string, object>)obj).Count == 0;
        }

        /// <summary>
        /// Test if an object is a KeyValuePair&lt;,&gt; (e.g. of any types)
        /// </summary>
        ///
        /// <param name="obj">
        /// The object to test
        /// </param>
        ///
        /// <returns>
        /// true if key value pair, false if not.
        /// </returns>

        public static bool IsKeyValuePair(object obj)
        {
            Type valueType = obj.GetType();
            var valueTypeInfo = valueType.GetTypeInfo();
            if (valueTypeInfo.IsGenericType)
            {
                Type baseType = valueTypeInfo.GetGenericTypeDefinition();
                if (baseType == typeof(KeyValuePair<,>))
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region type conversion methods

        /// <summary>
        /// Coerce a javascript object into a Javascript type (null, bool, int, double, datetime, or string). If you know what the 
        /// type should be, then use Convert instead.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static IConvertible Coerce(object value)
        {
            if (value == null)
            {
                return null;
            }
            Type realType = GetUnderlyingType(value.GetType());

            if (realType == typeof(bool) || realType == typeof(DateTime) || realType == typeof(double))
            {
                return (IConvertible)value;
            }
            else if (IsNumericType(value.GetType()))
            {
                return Support.NumberToDoubleOrInt((IConvertible)value);
            }

            string stringVal = value.ToString();

            double doubleVal;
            DateTime dateTimeVal;

            if (stringVal == "false")
            {
                return false;
            }
            else if (stringVal == "true")
            {
                return true;
            }
            else if (stringVal == "undefined" || stringVal == "null")
            {
                return null;
            }
            else if (Double.TryParse(stringVal, out doubleVal))
            {
                return Support.NumberToDoubleOrInt(doubleVal);
            }
            else if (DateTime.TryParse(stringVal, out dateTimeVal))
            {
                return dateTimeVal;
            }
            else
            {
                return stringVal;
            }


        }

        /// <summary>
        /// Convert an object of any value type to the specified type using any known means.
        /// </summary>
        ///
        /// <exception cref="InvalidCastException">
        /// Thrown when an object cannot be cast to a required type.
        /// </exception>
        ///
        /// <typeparam name="T">
        /// Generic type parameter.
        /// </typeparam>
        /// <param name="value">
        /// The object to convert
        /// </param>
        ///
        /// <returns>
        /// An object of the target type
        /// </returns>

        public static T Convert<T>(object value)
        {
            T output;

            if (!TryConvert<T>(value, out output))
            {
                throw new InvalidCastException("Unable to convert to type " + typeof(T).ToString());
            }
            return output;
        }

        /// <summary>
        /// Convert an object of any value type to the specified type using any known means.
        /// </summary>
        ///
        /// <exception cref="InvalidCastException">
        /// Thrown when an object cannot be cast to a required type.
        /// </exception>
        ///
        /// <param name="value">
        /// The object to convert
        /// </param>
        /// <param name="type">
        /// The target type
        /// </param>
        ///
        /// <returns>
        /// An object of the target type
        /// </returns>

        public static object Convert(object value, Type type)
        {
            object output;
            if (!TryConvert(value, out output, type, Objects.DefaultValue(type)))
            {
                throw new InvalidCastException("Unable to convert to type " + type.ToString());
            }
            return output;
        }

        /// <summary>
        /// Convert an object of any value type to the specified type using any known means.
        /// </summary>
        ///
        /// <typeparam name="T">
        /// Generic type parameter.
        /// </typeparam>
        /// <param name="value">
        /// The object to convert.
        /// </param>
        /// <param name="defaultValue">
        /// (optional) the default value.
        /// </param>
        ///
        /// <returns>
        /// An object of the target type.
        /// </returns>

        public static T Convert<T>(object value, T defaultValue)
        {
            T output;
            if (!TryConvert<T>(value, out output))
            {
                output = defaultValue;
            }
            return output;
        }

        /// <summary>
        /// Try to convert any object to the specified type
        /// </summary>
        ///
        /// <typeparam name="T">
        /// The target type
        /// </typeparam>
        /// <param name="value">
        /// The object or value to convert.
        /// </param>
        /// <param name="typedValue">
        /// [out] The typed value.
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        public static bool TryConvert<T>(object value, out T typedValue)
        {
            object outVal;
            if (TryConvert(value, out outVal, typeof(T)))
            {
                typedValue = (T)outVal;
                return true;
            }
            else
            {

                typedValue = (T)DefaultValue(typeof(T));
                return false;
            }
        }

        /// <summary>
        /// Try to convert an object or value to a specified type, using a default value if the
        /// conversion fails.
        /// </summary>
        ///
        /// <param name="value">
        /// The object or value to convert.
        /// </param>
        /// <param name="typedValue">
        /// [out] The typed value.
        /// </param>
        /// <param name="type">
        /// The type to convert to
        /// </param>
        /// <param name="defaultValue">
        /// (optional) the default value.
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        public static bool TryConvert(object value, out object typedValue, Type type, object defaultValue = null)
        {
            typedValue = null;
            object output = defaultValue;
            bool success = false;
            Type realType;
            string stringVal = value == null ? String.Empty : value.ToString().ToLower().Trim();
            if (type == typeof(string))
            {
                typedValue = value == null ? null : value.ToString();
                return true;
            }
            else if (IsNullableType(type))
            {
                if (stringVal == String.Empty)
                {
                    typedValue = null;
                    return true;
                }
                realType = GetUnderlyingType(type);
            }
            else
            {
                if (stringVal == String.Empty)
                {
                    typedValue = Objects.DefaultValue(type);
                    return false;
                }
                realType = type;
            }


            if (realType == value.GetType())
            {
                output = value;
                success = true;
            }
            else if (realType == typeof(bool))
            {
                bool result;
                success = TryStringToBool(stringVal, out result);
                if (success)
                {
                    output = result;
                }
            }
            else if (realType.GetTypeInfo().IsEnum)
            {
                output = Enum.Parse(realType, stringVal);
                success = true;
            }
            else if (realType == typeof(int)
                || realType == typeof(long)
                || realType == typeof(float)
                || realType == typeof(double)
                || realType == typeof(decimal))
            {
                object val;

                if (TryParseNumber(stringVal, out val, realType))
                {
                    output = val;
                    success = true;
                }
            }
            else if (realType == typeof(DateTime))
            {
                DateTime val;
                if (DateTime.TryParse(stringVal, out val))
                {
                    output = val;
                    success = true;
                }
            }
            else
            {
                output = value;
            }

            // cast the ou

            if (output != null
                && output.GetType() != realType)
            {

                if (realType is IConvertible)
                {
                    try
                    {
                        typedValue = System.Convert.ChangeType(output, realType);
                        success = true;
                    }
                    catch
                    {
                        typedValue = output ?? DefaultValue(realType);
                    }
                }
                if (!success)
                {
                    typedValue = output ?? DefaultValue(realType);
                }

            }
            else
            {
                typedValue = output ?? DefaultValue(realType);
            }
            return success;
        }

        /// <summary>
        /// Returns an Object with the specified Type and whose value is equivalent to the specified
        /// object.
        /// </summary>
        ///
        /// <remarks>
        /// This method exists as a workaround to System.Convert.ChangeType(Object, Type) which does not
        /// handle nullables as of version 2.0 (2.0.50727.42) of the .NET Framework. The idea is that
        /// this method will be deleted once Convert.ChangeType is updated in a future version of the
        /// .NET Framework to handle nullable types, so we want this to behave as closely to
        /// Convert.ChangeType as possible. This method was written by Peter Johnson at:
        /// http://aspalliance.com/author.aspx?uId=1026.
        /// </remarks>
        ///
        /// <exception cref="ArgumentNullException">
        /// Thrown when one or more required arguments are null.
        /// </exception>
        ///
        /// <param name="value">
        /// An Object that implements the IConvertible interface.
        /// </param>
        /// <param name="conversionType">
        /// The Type to which value is to be converted.
        /// </param>
        ///
        /// <returns>
        /// An object whose Type is conversionType (or conversionType's underlying type if conversionType
        /// is Nullable&lt;&gt;) and whose value is equivalent to value. -or- a null reference, if value
        /// is a null reference and conversionType is not a value type.
        /// </returns>

        public static object ChangeType(object value, Type conversionType)
        {
            // Note: This if block was taken from Convert.ChangeType as is, and is needed here since we're
            // checking properties on conversionType below.
            if (conversionType == null)
            {
                throw new ArgumentNullException("conversionType");
            } // end if

            // If it's not a nullable type, just pass through the parameters to Convert.ChangeType

            var conversionInfo = conversionType.GetTypeInfo();
            if (conversionInfo.IsGenericType &&
              conversionInfo.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                // It's a nullable type, so instead of calling Convert.ChangeType directly which would throw a
                // InvalidCastException (per http://weblogs.asp.net/pjohnson/archive/2006/02/07/437631.aspx),
                // determine what the underlying type is
                // If it's null, it won't convert to the underlying type, but that's fine since nulls don't really
                // have a type--so just return null
                // Note: We only do this check if we're converting to a nullable type, since doing it outside
                // would diverge from Convert.ChangeType's behavior, which throws an InvalidCastException if
                // value is null and conversionType is a value type.
                if (value == null)
                {
                    return null;
                } // end if

                // It's a nullable type, and not null, so that means it can be converted to its underlying type,
                // so overwrite the passed-in conversion type with this underlying type
                NullableConverter nullableConverter = new NullableConverter(conversionType);
                conversionType = nullableConverter.UnderlyingType;
            } // end if

            // Now that we've guaranteed conversionType is something Convert.ChangeType can handle (i.e. not a
            // nullable type), pass the call on to Convert.ChangeType
            return System.Convert.ChangeType(value, conversionType);
        }

        /// <summary>
        /// Try to parse a string into a valid number
        /// </summary>
        ///
        /// <exception cref="InvalidCastException">
        /// Thrown when parsing fails
        /// </exception>
        ///
        /// <param name="value">
        /// The value to parse
        /// </param>
        /// <param name="number">
        /// [out] The parsed value type
        /// </param>
        /// <param name="T">
        /// The Type to process.
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        public static bool TryParseNumber(string value, out object number, Type T)
        {
            double val;
            number = 0;
            if (double.TryParse(value, out val))
            {
                if (T == typeof(int))
                {
                    number = System.Convert.ToInt32(Math.Round(val));
                }
                else if (T == typeof(long))
                {
                    number = System.Convert.ToInt64(Math.Round(val));
                }
                else if (T == typeof(double))
                {
                    number = System.Convert.ToDouble(val);
                }
                else if (T == typeof(decimal))
                {
                    number = System.Convert.ToDecimal(val);
                }
                else if (T == typeof(float))
                {
                    number = System.Convert.ToSingle(val);
                }
                else
                {
                    throw new InvalidCastException("Unhandled type for TryParseNumber: " + T.GetType().ToString());
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion

        #region Other methods
        /// <summary>
        /// Enumerate the values of the properties of an object to a sequence of type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static IEnumerable<T> EnumerateProperties<T>(object obj)
        {
            return EnumerateProperties<T>(obj, new Type[] {  });
        }

        /// <summary>
        /// Enumerate the values of the properties of an object to a sequence of type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="ignoreAttributes">All properties with an attribute of these types will be ignored</param>
        /// <returns></returns>
        public static IEnumerable<T> EnumerateProperties<T>(object obj, IEnumerable<Type> ignoreAttributes)
        {
            HashSet<Type> IgnoreList = new HashSet<Type>();
            if (ignoreAttributes != null)
            {
                IgnoreList.AddRange(ignoreAttributes);
            }

            IDictionary<string, object> source;

            if (obj is IDictionary<string, object>)
            {
                source = (IDictionary<string, object>)obj;
            }
            else
            {
                source = Objects.ToExpando<JsObject>(obj, false, ignoreAttributes);
            }
            foreach (KeyValuePair<string, object> kvp in source)
            {
                if (typeof(T) == typeof(KeyValuePair<string, object>))
                {
                    yield return (T)(object)(new KeyValuePair<string, object>(kvp.Key,
                         kvp.Value is IDictionary<string, object> ?
                            ToExpando((IDictionary<string, object>)kvp.Value) :
                            kvp.Value));

                }
                else
                {
                    yield return Objects.Convert<T>(kvp.Value);
                }
            }

        }


        /// <summary>
        /// Return the default value for a type.
        /// </summary>
        ///
        /// <param name="type">
        /// The type
        /// </param>
        ///
        /// <returns>
        /// An value or null
        /// </returns>

        public static object DefaultValue(Type type)
        {
            return type.GetTypeInfo().IsValueType ?
                CreateInstance(type) : null;
        }

        /// <summary>
        /// Creates an instance of a type
        /// </summary>
        ///
        /// <param name="type">
        /// The type
        /// </param>
        ///
        /// <returns>
        /// The new instance.
        /// </returns>

        public static object CreateInstance(Type type)
        {
            return Activator.CreateInstance(type);
        }

        /// <summary>
        /// Creates an instance of type
        /// </summary>
        ///
        /// <typeparam name="T">
        /// Generic type parameter.
        /// </typeparam>
        ///
        /// <returns>
        /// The new instance&lt; t&gt;
        /// </returns>

        public static T CreateInstance<T>() where T : class
        {
            return Activator.CreateInstance<T>();
        }

        /// <summary>
        /// Returns a sequence containing a single element, the object passed by parameter.
        /// </summary>
        ///
        /// <typeparam name="T">
        /// The type of object.
        /// </typeparam>
        /// <param name="obj">
        /// The object to add to the sequence.
        /// </param>
        ///
        /// <returns>
        /// A sequence with one element.
        /// </returns>

        public static IEnumerable<T> Enumerate<T>(T obj)
        {
            if (obj != null)
            {
                yield return obj;
            }
        }

        /// <summary>
        /// Returns an enumeration composed of each object in the parameter list.
        /// </summary>
        ///
        /// <typeparam name="T">
        /// The generic type of the enumeration.
        /// </typeparam>
        /// <param name="obj">
        /// The sequence of objects.
        /// </param>
        ///
        /// <returns>
        /// An enumerator that allows foreach to be used to process enumerate&lt; t&gt; in this
        /// collection.
        /// </returns>

        public static IEnumerable<T> Enumerate<T>(params T[] obj)
        {
            return obj;
        }

        /// <summary>
        /// Enumerates a sequence of objects
        /// </summary>
        ///
        /// <param name="obj">
        /// The sequence
        /// </param>
        ///
        /// <returns>
        /// An enumeration.
        /// </returns>

        public static IEnumerable Enumerate(params object[] obj)
        {
            return obj;
        }

        /// <summary>
        /// Returns an empty sequence of the specified type.
        /// </summary>
        ///
        /// <typeparam name="T">
        /// The generic type of the sequence.
        /// </typeparam>
        ///
        /// <returns>
        /// An empty sequence.
        /// </returns>

        public static IEnumerable<T> EmptyEnumerable<T>()
        {
            yield break;
        }

        /// <summary>
        /// Convert (recursively) an IDictionary&lt;string,object&gt; to a dynamic object.
        /// </summary>
        ///
        /// <typeparam name="T">
        /// Generic type parameter.
        /// </typeparam>
        /// <param name="obj">
        /// The source dicationary
        /// </param>
        ///
        /// <returns>
        /// A new dynamic object
        /// </returns>

        public static T Dict2Dynamic<T>(IDictionary<string, object> obj) where T : IDynamicMetaObjectProvider, new()
        {
            return Dict2Dynamic<T>(obj, false);
        }

        /// <summary>
        /// Combine elements of a sequence into a single string, separated by separator.
        /// </summary>
        ///
        /// <param name="list">
        /// The source sequence.
        /// </param>
        /// <param name="separator">
        /// The separator.
        /// </param>
        ///
        /// <returns>
        /// A string.
        /// </returns>

        public static string Join(IEnumerable<string> list, string separator)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string item in list)
            {
                sb.Append(sb.Length == 0 ? item : separator + item);
            }
            return sb.ToString();
        }

        private static IEnumerable<string> toStringList(IEnumerable source)
        {
            foreach (var item in source)
            {
                yield return item.ToString();
            }
        }


        #endregion

        #region private methods

        /// <summary>
        /// Deal with datetime values
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static object ParseValue(object value)
        {
            object result;
            if (value != null && value.GetType().GetTypeInfo().IsAssignableFrom(typeof(DateTime)))
            {
                result = DateTime.SpecifyKind((DateTime)value, DateTimeKind.Utc).ToLocalTime();
            }
            else
            {
                result = value;
            }

            return result;
        }

        /// <summary>
        /// Takes a default deserialized value from JavaScriptSerializer and parses it into expando
        /// objects. This will convert inner array types to strongly-typed arrays; inner object types to
        /// dynamic objects; and inner date/time value strings to real datetime values.
        /// </summary>
        ///
        /// <typeparam name="T">
        /// The target type.
        /// </typeparam>
        /// <param name="value">
        /// The value.
        /// </param>
        /// <param name="convertDates">
        /// When true, date values will be parsed also. (This is likely problematic because of different
        /// date conventions).
        /// </param>
        ///
        /// <returns>
        /// The deserialized converted value&lt; t&gt;
        /// </returns>

        private static object ConvertDeserializedValue<T>(object value, bool convertDates) where T : IDynamicMetaObjectProvider, new()
        {
            if (value is IDictionary<string, object>)
            {
                return Dict2Dynamic<T>((IDictionary<string, object>)value);
            }
            else if (value is IEnumerable && !(value is string))
            {
                // JSON arrays are returned as ArrayLists of values or IDictionary<string,object> by
                // JavaScriptSerializer We will convert them to arrays that are either strongly typed, or
                // object[]. We do this by seeing if everything is the same type first while adding it to an
                // object list, then constructing an array. 

                IList<object> objectList = new List<object>();

                Type onlyType = null;
                bool same = true;
                foreach (var val in (IEnumerable)value)
                {
                    if (same)
                    {
                        if (onlyType == null)
                        {
                            onlyType = val.GetType();
                        }
                        else
                        {
                            same = onlyType == val.GetType();
                        }
                    }
                    objectList.Add(val);
                }

                Array array;
                if (onlyType != null)
                {
                    // This means a single type was found, and we can create a strongly typed array
                    // If it's a list of objects, map again to the default dynamic type

                    if (typeof(IDictionary<string, object>).GetTypeInfo().IsAssignableFrom(onlyType))
                    {
                        array = Array.CreateInstance(Config.DynamicObjectType, objectList.Count);
                    }
                    else
                    {
                        array = Array.CreateInstance(onlyType, objectList.Count);
                    }
                }
                else
                {
                    array = Array.CreateInstance(Config.DynamicObjectType, objectList.Count);
                }

                // copy values from list to the arraty
                for (int index = 0; index < objectList.Count; index++)
                {
                    array.SetValue(ConvertDeserializedValue<T>(objectList[index], true), index);
                }

                return array;

            }
            else if (convertDates)
            {
                return ParseValue(value);
            }
            else
            {
                return value;
            }


        }



        /// <summary>
        /// Return the proper type for an object (ignoring nullability)
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Type GetUnderlyingType(Type type)
        {

            if (type != typeof(string) && IsNullableType(type))
            {
                return Nullable.GetUnderlyingType(type);
            }
            else
            {
                return type;
            }

        }

        #endregion

        #region Object/Expando Manipulation

        // TODO: the implementation needs to go to another class

        /// <summary>
        /// Convert any IDictionary&lt;string,object&gt; into an expandoobject recursively.
        /// </summary>
        ///
        /// <typeparam name="T">
        /// The type of target to create. It must implementing IDynamicMetaObjectProvider; if it is
        /// actually the interface IDynamicMetaObjectProvider, then the default dynamic object type will
        /// be created.
        /// </typeparam>
        /// <param name="obj">
        /// The source dictionary
        /// </param>
        /// <param name="convertDates">
        /// .
        /// </param>
        ///
        /// <returns>
        /// .
        /// </returns>

        public static T Dict2Dynamic<T>(IDictionary<string, object> obj, bool convertDates) where T : IDynamicMetaObjectProvider, new()
        {
            T returnObj = new T();
            if (obj != null)
            {
                IDictionary<string, object> dict = (IDictionary<string, object>)returnObj;
                foreach (KeyValuePair<string, object> kvp in obj)
                {
                    dict[kvp.Key] = ConvertDeserializedValue<T>(kvp.Value, convertDates);
                }
            }
            return returnObj;
        }

        /// <summary>
        /// Map properties of inputObjects to target. If target is an expando object, it will be updated.
        /// If not, a new one will be created including the properties of target and inputObjects.
        /// </summary>
        ///
        /// <param name="deep">
        /// When true, will clone properties that are objects.
        /// </param>
        /// <param name="target">
        /// The target of the mapping, or null to create a new target
        /// </param>
        /// <param name="inputObjects">
        /// One or more objects that are the source of the mapping
        /// </param>
        ///
        /// <returns>
        /// The target object itself, if non-null, or a new dynamic object, if the target is null
        /// </returns>

        public static object Extend(bool deep, object target, params object[] inputObjects)
        {
            return ExtendImpl(null, deep, target, inputObjects);
        }

        private static object ExtendImpl(HashSet<object> parents, bool deep, object target, params object[] inputObjects)
        {
            if (deep && parents == null)
            {
                parents = new HashSet<object>();
                parents.Add(target);
            }
            // Add all non-null parameters to a processing queue
            Queue<object> inputs = new Queue<object>(inputObjects);
            Queue<object> sources = new Queue<object>();
            HashSet<object> unique = new HashSet<object>();

            while (inputs.Count > 0)
            {
                object src = inputs.Dequeue();
                if (src is string && Objects.IsJson(src))
                {
                    src = CQ.ParseJSON((string)src);
                }
                if (!Objects.IsExpando(src) && Objects.IsExtendableType(src) && src is IEnumerable)
                {
                    foreach (var innerSrc in (IEnumerable)src)
                    {
                        inputs.Enqueue(innerSrc);
                    }
                }
                if (!Objects.IsImmutable(src) && unique.Add(src))
                {
                    sources.Enqueue(src);
                }
            }

            // Create a new empty object if there's no existing target -- same as using {} as the jQuery parameter

            if (target == null)
            {
                target = Activator.CreateInstance(Config.DynamicObjectType);
            }

            else if (!Objects.IsExtendableType(target))
            {
                throw new InvalidCastException("Target type '" + target.GetType().ToString() + "' is not valid for CsQuery.Extend.");
            }

            //sources = sources.Dequeue();
            object source;
            while (sources.Count > 0)
            {
                source = sources.Dequeue();

                if (Objects.IsExpando(source))
                {
                    // Expando object -- copy/clone it
                    foreach (var kvp in (IDictionary<string, object>)source)
                    {

                        AddExtendKVP(deep, parents, target, kvp.Key, kvp.Value);
                    }
                }
                else if (!Objects.IsExtendableType(source) && source is IEnumerable)
                {
                    // For enumerables, treat each value as another object. Append to the operation list 
                    // This check is after the Expand check since Expandos are elso enumerable
                    foreach (object obj in ((IEnumerable)source))
                    {
                        sources.Enqueue(obj);
                        continue;
                    }
                }
                else
                {
                    // treat it as a regular object - try to copy fields/properties
                    IEnumerable<MemberInfo> members = source.GetType().GetTypeInfo().GetMembers();

                    object value;
                    foreach (var member in members)
                    {
                        if (!IgnorePropertyNames.Contains(member.Name))
                        {
                            // 2nd condition skips index properties
                            if (member is PropertyInfo)
                            {
                                PropertyInfo propInfo = (PropertyInfo)member;
                                if (!propInfo.CanRead || propInfo.GetIndexParameters().Length > 0)
                                {
                                    continue;
                                }
                                value = ((PropertyInfo)member).GetGetMethod().Invoke(source, null);
                            }
                            else if (member is FieldInfo)
                            {
                                FieldInfo fieldInfo = (FieldInfo)member;
                                if (!fieldInfo.IsPublic || fieldInfo.IsStatic)
                                {
                                    continue;
                                }
                                value = fieldInfo.GetValue(source);
                            }
                            //else if (member is MethodInfo)
                            //{
                            //    // Attempt to identify anonymous types which are implemented as methods with no parameters and 
                            //    // names starting with "get_". This is not really ideal, but I don't know a better way to identify
                            //    // them, and I think it's also reasonably safe to invoke any methods named with "get_" anyway.
                            //    MethodInfo methodInfo = (MethodInfo)member;
                            //    if (methodInfo.IsStatic || !methodInfo.IsPublic || methodInfo.IsAbstract || methodInfo.IsConstructor ||
                            //        !(methodInfo.Name.StartsWith("get_")) || methodInfo.GetParameters().Length>0) {
                            //        continue;
                            //    }
                            //    value = methodInfo.Invoke(source,null);
                            //} 
                            else
                            {
                                //It's a method or something we don't know how to handle. Skip it.
                                continue;
                            }
                            AddExtendKVP(deep, parents, target, member.Name, value);

                        }
                    }
                }

            }
            return target;
        }

        /// <summary>
        /// Converts a regular object to a dynamic object, or returns the source object if it is already
        /// a dynamic object.
        /// </summary>
        ///
        /// <param name="source">
        /// 
        /// </param>
        ///
        /// <returns>
        /// source as a JsObject.
        /// </returns>

        public static JsObject ToExpando(object source)
        {
            return ToExpando(source, false);
        }

        /// <summary>
        /// Converts this object to a dynamic object of type T.
        /// </summary>
        ///
        /// <typeparam name="T">
        /// The type of dynamic object to create; must inherit IDynamicMetaObjectProvider and
        /// IDictionary&lt;string,object&gt;
        /// </typeparam>
        /// <param name="source">
        /// The object to convert.
        /// </param>
        ///
        /// <returns>
        /// The given data converted to a T.
        /// </returns>

        public static T ToExpando<T>(object source) where T : IDynamicMetaObjectProvider, IDictionary<string, object>, new()
        {
            return ToExpando<T>(source, false);
        }

        /// <summary>
        /// Converts a regular object to an expando object, or returns the source object if it is already
        /// an expando object. If "deep" is true, child properties are cloned rather than referenced.
        /// </summary>
        ///
        /// <param name="source">
        /// The object to convert
        /// </param>
        /// <param name="deep">
        /// When true, will clone properties that are objects.
        /// </param>
        ///
        /// <returns>
        /// The given data converted to a JsObject.
        /// </returns>

        public static JsObject ToExpando(object source, bool deep)
        {
            return ToExpando<JsObject>(source, deep);
        }

        /// <summary>
        /// Converts this object to an expando object of type T.
        /// </summary>
        ///
        /// <typeparam name="T">
        /// The type of object; must inherit IDynamicMetaObjectProvider and IDictionary&lt;string,
        /// object&gt;
        /// </typeparam>
        /// <param name="source">
        /// The object to convert
        /// </param>
        /// <param name="deep">
        /// When true, will clone properties that are objects.
        /// </param>
        ///
        /// <returns>
        /// The given data converted to a T.
        /// </returns>

        public static T ToExpando<T>(object source, bool deep) where T : IDictionary<string, object>, IDynamicMetaObjectProvider, new()
        {
            return ToExpando<T>(source, deep, new Type[] { });
        }

        /// <summary>
        /// Converts this object to an expando object of type T.
        /// </summary>
        ///
        /// <typeparam name="T">
        /// Generic type parameter.
        /// </typeparam>
        /// <param name="source">
        /// The object to convert.
        /// </param>
        /// <param name="deep">
        /// When true, will clone properties that are objects.
        /// </param>
        /// <param name="ignoreAttributes">
        /// A sequence of Attribute objects that, when any is found on a property, indicate that it should be ignored.
        /// </param>
        ///
        /// <returns>
        /// The given data converted to a T.
        /// </returns>

        public static T ToExpando<T>(object source, bool deep, IEnumerable<Type> ignoreAttributes) where T : IDictionary<string, object>, IDynamicMetaObjectProvider, new()
        {
            if (Objects.IsExpando(source) && !deep)
            {
                return Objects.Dict2Dynamic<T>((IDictionary<string, object>)source);
            }
            else
            {
                return ToNewExpando<T>(source, deep, ignoreAttributes);
            }
        }

        /// <summary>
        /// Clone an object. For value types, returns the value. For reference types, coverts to a
        /// dynamic object.
        /// </summary>
        ///
        /// <param name="obj">
        /// The source object.
        /// </param>
        ///
        /// <returns>
        /// The value passed or a new dynamic object.
        /// </returns>

        public static object CloneObject(object obj)
        {
            return CloneObject(obj, false);
        }

        /// <summary>
        /// Clone an object. For value types, returns the value. For reference types, coverts to a dynamic object. 
        /// </summary>
        ///
        /// <param name="obj">
        /// The source object.
        /// </param>
        /// <param name="deep">
        /// When true, will clone properties that are objects.
        /// </param>
        ///
        /// <returns>
        /// The value passed or a new dynamic object.
        /// </returns>

        public static object CloneObject(object obj, bool deep)
        {
            // a value type

            if (Objects.IsImmutable(obj))
            {
                return obj;
            }
            else if (obj.GetType().IsArray ||
                IsExpando(obj))
            {
                // CloneList hanldes IDictionary&lt;string,object&gt; types. 


                return ((IEnumerable)obj).CloneList(deep);
            }
            else
            {
                // TODO: check for existence of a "clone" method
                // convert regular objects to expando objects
                return (ToExpando(obj, true));
            }
        }

        /// <summary>
        /// Remove a property from a dynamic object, or return a copy of the object a a new dynamic object without the property.
        /// </summary>
        ///
        /// <exception cref="InvalidOperationException">
        /// Thrown when the requested operation is invalid.
        /// </exception>
        ///
        /// <param name="obj">
        /// The source object
        /// </param>
        /// <param name="property">
        /// The property to delete
        /// </param>
        ///
        /// <returns>
        /// A new dynamic object
        /// </returns>

        public static object DeleteProperty(object obj, string property)
        {
            if (IsImmutable(obj))
            {
                throw new ArgumentException("The object is a value type, it has no deletable properties.");
            }
            else if (IsExpando(obj))
            {
                IDictionary<string, object> dict = (IDictionary<string, object>)obj;
                dict.Remove(property);
                return dict;
            }
            else
            {
                var target = CloneObject(obj);

                return DeleteProperty(target, property);
            }
        }


        /// <summary>
        /// Implementation of "Extend" functionality
        /// </summary>
        /// <param name="deep"></param>
        /// <param name="parents"></param>
        /// <param name="target"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        private static void AddExtendKVP(bool deep, HashSet<object> parents, object target, string name, object value)
        {
            IDictionary<string, object> targetDict = null;
            if (Objects.IsExpando(target))
            {
                targetDict = (IDictionary<string, object>)target;
            }
            if (deep)
            {
                // Prevent recursion by seeing if this value has been added to the object already.
                // Though jQuery skips such elements, we could get away with this because we clone everything
                // during deep copies. The recursing property wouldn't exist yet when we cloned it.

                // for non-expando objects, we still want to add it & skip - but we can't remove a property
                if (Objects.IsExtendableType(value)
                    && !parents.Add(value))
                {
                    if (targetDict != null)
                    {
                        targetDict.Remove(name);
                    }
                    return;
                }

                object curValue;
                if (Objects.IsExtendableType(value)
                    && targetDict != null
                    && targetDict.TryGetValue(name, out curValue))
                {
                    //targetDic[name]=Extend(parents,true, null, curValue.IsExtendableType() ? curValue : null, value);
                    value = ExtendImpl(parents, true, null, Objects.IsExtendableType(curValue) ? curValue : null, value);

                }
                else
                {
                    // targetDic[name] = deep ? value.Clone(true) : value;
                    value = CloneObject(value, true);
                }
            }

            if (targetDict != null)
            {
                targetDict[name] = value;
            }
            else
            {
                // It's a regular object. It cannot be extended, but set any same-named properties.
                IEnumerable<MemberInfo> members = target.GetType().GetTypeInfo().GetMembers();

                foreach (var member in members)
                {
                    if (member.Name.Equals(name, StringComparison.CurrentCulture))
                    {
                        if (member is PropertyInfo)
                        {
                            PropertyInfo propInfo = (PropertyInfo)member;
                            if (!propInfo.CanWrite)
                            {
                                continue;
                            }
                            propInfo.GetSetMethod().Invoke(target,new object[] {value});

                        }
                        else if (member is FieldInfo)
                        {
                            FieldInfo fieldInfo = (FieldInfo)member;
                            if (fieldInfo.IsStatic || !fieldInfo.IsPublic || fieldInfo.IsLiteral || fieldInfo.IsInitOnly)
                            {
                                continue;
                            }
                            fieldInfo.SetValue(target, value);
                        }
                        else
                        {
                            //It's a method or something we don't know how to handle. Skip it.
                            continue;
                        }
                    }
                }
            }

        }

        /// <summary>
        /// Implementation of object>expando
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="deep"></param>
        /// <param name="ignoreAttributes"></param>
        /// <returns></returns>
        private static T ToNewExpando<T>(object source, bool deep, IEnumerable<Type> ignoreAttributes) where T : IDynamicMetaObjectProvider, IDictionary<string, object>, new()
        {
            if (source == null)
            {
                return default(T);
            }
            HashSet<Type> IgnoreList = new HashSet<Type>(ignoreAttributes ?? new Type[0]);

            if (source is string && Objects.IsJson(source))
            {
                source = Utility.JSON.ParseJSON((string)source);
            }

            if (Objects.IsExpando(source))
            {
                return (T)Objects.CloneObject(source, deep);
            }
            else if (source is IDictionary)
            {
                T dict = new T();
                IDictionary sourceDict = (IDictionary)source;
                IDictionary itemDict = (IDictionary)source;
                foreach (var key in itemDict.Keys)
                {
                    string stringKey = key.ToString();
                    if (dict.ContainsKey(stringKey))
                    {
                        throw new InvalidCastException("The key '" + key + "' could not be added because the same key already exists. Conversion of the source object's keys to strings did not result in unique keys.");
                    }
                    dict.Add(stringKey, itemDict[key]);
                }
                return (T)dict;
            }
            else if (!Objects.IsExtendableType(source))
            {
                throw new InvalidCastException("Conversion to ExpandObject must be from a JSON string, an object, or an ExpandoObject");
            }

            T target = new T();
            IDictionary<string, object> targetDict = (IDictionary<string, object>)target;

            IEnumerable<MemberInfo> members = source.GetType().GetTypeInfo().GetMembers(BindingFlags.Public | BindingFlags.Instance);
            foreach (var member in members)
            {
                if (!IgnorePropertyNames.Contains(member.Name))
                {
                    foreach (object attrObj in member.GetCustomAttributes(false))
                    {
                        Attribute attr = (Attribute)attrObj;
                        if (IgnoreList.Contains(attr.GetType()))
                        {
                            goto NextAttribute;
                        }
                    }
                    string name = member.Name;


                    object value = null;
                    bool skip = false;

                    if (member is PropertyInfo)
                    {
                        PropertyInfo propInfo = (PropertyInfo)member;
                        if (propInfo.GetIndexParameters().Length == 0 &&
                            propInfo.CanRead)
                        {

                            // wrap this because we are testing every single property - if it doesn't work we don't want to use it
                            try
                            {
                                value = ((PropertyInfo)member).GetGetMethod().Invoke(source, null);
                            }
                            catch
                            {
                                skip = true;
                            }
                        }
                    }
                    else if (member is FieldInfo)
                    {
                        value = ((FieldInfo)member).GetValue(source);
                    }
                    else
                    {
                        continue;
                    }
                    if (!skip)
                    {
                        targetDict[name] = deep ? Objects.CloneObject(value, true) : value;
                    }
                }
            NextAttribute: { }
            }

            return target;
        }

        /// <summary>
        /// Try to parse an english or numeric string into a boolean value
        /// </summary>
        /// <param name="value"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private static bool TryStringToBool(string value, out bool result)
        {
            switch (value)
            {
                case "on":
                case "yes":
                case "true":
                case "enabled":
                case "active":
                case "1":
                    result = true;
                    return true;
                case "off":
                case "no":
                case "false":
                case "disabled":
                case "0":
                    result = false;
                    return true;
            }
            result = false;
            return false;
        }


        #endregion

        #region Factory methods

        /// <summary>
        /// Creates a new text node.
        /// </summary>
        ///
        /// <param name="text">
        /// The text.
        /// </param>
        ///
        /// <returns>
        /// The new text node.
        /// </returns>

        public static IDomText CreateTextNode(string text)
        {
            return new Implementation.DomText(text);
        }

        /// <summary>
        /// Creates a comment node.
        /// </summary>
        ///
        /// <param name="comment">
        /// The comment.
        /// </param>
        ///
        /// <returns>
        /// The new comment.
        /// </returns>

        public static IDomComment CreateComment(string comment)
        {
            return new Implementation.DomComment(comment);
        }

        /// <summary>
        /// Creates a new empty document.
        /// </summary>
        ///
        /// <returns>
        /// The new document.
        /// </returns>

        public static IDomDocument CreateDocument()
        {
            return new Implementation.DomDocument();
        }

        /// <summary>
        /// Creates CDATA node
        /// </summary>
        ///
        /// <param name="data">
        /// The data.
        /// </param>
        ///
        /// <returns>
        /// The new CDATA node
        /// </returns>

        public static IDomCData CreateCData(string data)
        {
            return new Implementation.DomCData();
        }

        /// <summary>
        /// Creates a new, empty fragment node.
        /// </summary>
        ///
        /// <returns>
        /// The new fragment.
        /// </returns>

        public static IDomFragment CreateFragment()
        {
            return new Implementation.DomFragment();
        }



        #endregion
    }
}