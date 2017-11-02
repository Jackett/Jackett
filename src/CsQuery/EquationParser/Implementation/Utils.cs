using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

// TODO this should be fully commented; however it's not part of the main public API

#pragma warning disable 1591
#pragma warning disable 1570


namespace CsQuery.EquationParser.Implementation
{
    public static class Utils
    {
        public static bool IsIntegralType<T>()
        {
            return IsIntegralType(typeof(T));
        }
        public static bool IsIntegralType(IConvertible value)
        {
            return IsIntegralType(value.GetType());
        }
        public static bool IsIntegralType(Type type)
        {
            return type == typeof(Int16) ||
                type == typeof(Int32) ||
                type == typeof(Int64) ||
                type == typeof(UInt16) ||
                type == typeof(UInt32) ||
                type == typeof(UInt64) ||
                type == typeof(char) ||
                type == typeof(byte) ||
                type == typeof(bool);
        }
        public static bool IsIntegralValue(IConvertible value)
        {
            bool result = false;
            if (IsIntegralType(value))
            {
                result=true;
            } else {
                try
                {
                    double dblVal = (double)Convert.ChangeType(value, typeof(double));
                    double intVal = Math.Floor(dblVal);
                    return intVal == dblVal;
                }
                catch
                {
                    result = false;
                }
            }
            return result;

        }
        public static bool IsNumericType<T>()
        {
            return IsNumericType(typeof(T));
        }
        public static bool IsNumericType(object obj)
        {
            Type t = GetUnderlyingType(obj.GetType());
            return IsNumericType(t);
        }

        /// <summary>
        /// Test if the type is a numeric primitive type, e.g. all except string, char &amp; bool.
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
            return type.GetTypeInfo().IsPrimitive && !(type == typeof(string) || type == typeof(char) || type == typeof(bool));
        }

        /// <summary>
        /// Any primitive type that can be converted to a number, e.g. all except string. This just
        /// returns any primitive type that is not IEnumerable.
        /// </summary>
        ///
        /// <param name="type">
        /// The type to test.
        /// </param>
        ///
        /// <returns>
        /// true if numeric convertible, false if not.
        /// </returns>

        public static bool IsNumericConvertible(Type type)
        {
            return type.GetTypeInfo().IsPrimitive && !(type  == typeof(string));
        }

        /// <summary>
        /// Test if the value is a string or char type
        /// </summary>
        ///
        /// <param name="value">
        /// The value to test
        /// </param>
        ///
        /// <returns>
        /// true if text or char, false if not.
        /// </returns>

        public static bool IsText(object value)
        {
            Type t  = value.GetType();
            return t == typeof(string) || t == typeof(char);
        }

        /// <summary>
        /// Factory to return a function object based on a name
        /// </summary>
        ///
        /// <exception cref="ArgumentException">
        /// Thrown when the named function is not known.
        /// </exception>
        ///
        /// <typeparam name="T">
        /// The return type of the function
        /// </typeparam>
        /// <param name="functionName">
        /// Name of the function.
        /// </param>
        ///
        /// <returns>
        /// The function&lt; t&gt;
        /// </returns>

        public static IFunction GetFunction<T>(string functionName)
        {
            bool IsTyped = typeof(T) == typeof(IConvertible);
            switch (functionName)
            {
                case "abs":
                    return new Functions.Abs();
                default:
                    throw new ArgumentException("Undefined function '" + functionName + "'");

            }
        }
        /// <summary>
        /// If the value is an operand, returns it, otherwise creates the right kind of operand
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static IOperand EnsureOperand(IConvertible value)
        {
            if (value is IOperand)
            {
                return (IOperand)value;
            }
            else if (value is string)
            {
                //TODO: parse quotes and return a string liteeral if need be
                return Equations.CreateVariable((string)value);
            }
            else
            {
                return Equations.CreateLiteral(value);
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


        public static IEnumerable<T> EmptyEnumerable<T>()
        {
            yield break;
        }
    }
}
