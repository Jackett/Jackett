using System;
using System.Collections.Generic;
using System.Collections;
using System.Text.RegularExpressions;
using System.Linq;
using System.Dynamic;
using System.Text;
using System.Reflection;
using CsQuery.ExtensionMethods.Internal;
using CsQuery.Utility;

namespace CsQuery.ExtensionMethods
{
    /// <summary>
    /// Some extension methods that come in handy when working with CsQuery
    /// </summary>
    public static class ExtensionMethods
    {

        #region string extension methods

        /// <summary>
        /// Perform a substring replace using a regular expression.
        /// </summary>
        ///
        /// <param name="input">
        /// The target of the replacement.
        /// </param>
        /// <param name="pattern">
        /// The pattern to match.
        /// </param>
        /// <param name="replacement">
        /// The replacement string.
        /// </param>
        ///
        /// <returns>
        /// A new string.
        /// </returns>

        public static String RegexReplace(this String input, string pattern, string replacement)
        {
            return input.RegexReplace(Objects.Enumerate(pattern), Objects.Enumerate(replacement));
        }

        /// <summary>
        /// Perform a substring replace using a regular expression and one or more patterns
        /// </summary>
        ///
        /// <exception cref="ArgumentException">
        /// Thrown when the list of replacements is not the same length as the list of patterns.
        /// </exception>
        ///
        /// <param name="input">
        /// The target of the replacement.
        /// </param>
        /// <param name="patterns">
        /// The patterns.
        /// </param>
        /// <param name="replacements">
        /// The replacements.
        /// </param>
        ///
        /// <returns>
        /// A new string.
        /// </returns>

        public static String RegexReplace(this String input, IEnumerable<string> patterns, IEnumerable<string> replacements)
        {
            List<string> patternList = new List<string>(patterns);
            List<string> replacementList = new List<string>(replacements);
            if (replacementList.Count != patternList.Count)
            {
                throw new ArgumentException("Mismatched pattern and replacement lists.");
            }

            for (var i = 0; i < patternList.Count; i++)
            {
                input = Regex.Replace(input, patternList[i], replacementList[i]);
            }

            return input;
        }

        /// <summary>
        /// Perform a substring replace using a regular expression.
        /// </summary>
        ///
        /// <param name="input">
        /// The target of the replacement.
        /// </param>
        /// <param name="pattern">
        /// The pattern to match.
        /// </param>
        /// <param name="evaluator">
        /// The evaluator.
        /// </param>
        ///
        /// <returns>
        /// A new string.
        /// </returns>

        public static string RegexReplace(this String input, string pattern, MatchEvaluator evaluator)
        {

            return Regex.Replace(input, pattern, evaluator);
        }

        /// <summary>
        /// Test whether the regular expression pattern matches the string.
        /// </summary>
        ///
        /// <param name="input">
        /// The string to test
        /// </param>
        /// <param name="pattern">
        /// The pattern
        /// </param>
        ///
        /// <returns>
        /// true if the pattern matches, false if not.
        /// </returns>

        public static bool RegexTest(this String input, string pattern)
        {
            return Regex.IsMatch(input, pattern);
        }


        #endregion

        #region IEnumerable<T> extension methods

        /// <summary>
        /// Append an element to the end of a sequence.
        /// </summary>
        ///
        /// <typeparam name="T">
        /// Generic type parameter.
        /// </typeparam>
        /// <param name="list">
        /// The list to act on.
        /// </param>
        /// <param name="element">
        /// The element to append.
        /// </param>
        ///
        /// <returns>
        /// The combined sequence.
        /// </returns>

        public static IEnumerable<T> Concat<T>(this IEnumerable<T> list, T element)
        {
            if (list != null)
            {
                foreach (var item in list)
                {
                    yield return item;
                }
            }

            if (element != null)
            {
                yield return element;
            }
        }

        /// <summary>
        /// Return the zero-based index of the first item in a sequence where the predicate returns true
        /// </summary>
        ///
        /// <typeparam name="T">
        /// Type of object in the sequence
        /// </typeparam>
        /// <param name="list">
        /// The sequence to search through.
        /// </param>
        /// <param name="predicate">
        /// The predicate.
        /// </param>
        ///
        /// <returns>
        /// The zero-based position in the list where the item was found, or -1 if it was not found.
        /// </returns>

        public static int IndexOf<T>(this IEnumerable<T> list, Func<T, bool> predicate) {
            int index = 0;
            var enumerator = list.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (predicate(enumerator.Current)) {
                    return index;
                }
                index++;
            }
            return -1;
        }

        /// <summary>
        /// Return the zero-based index of the first item in a sequence where the predicate returns true,
        /// and return the matched item as an output parameter.
        /// </summary>
        ///
        /// <typeparam name="T">
        /// Generic type parameter.
        /// </typeparam>
        /// <param name="list">
        /// The sequence to search through.
        /// </param>
        /// <param name="predicate">
        /// The predicate.
        /// </param>
        /// <param name="item">
        /// [out] The matched item.
        /// </param>
        ///
        /// <returns>
        /// The zero-based position in the list where the item was found, or -1 if it was not found.
        /// </returns>

        public static int IndexOf<T>(this IEnumerable<T> list, Func<T, bool> predicate, out T item)
        {
            int index = 0;
            var enumerator = list.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (predicate(enumerator.Current))
                {
                    item = enumerator.Current;
                    return index;
                }
                index++;
            }
            item = default(T);
            return -1;
        }

        /// <summary>
        /// Return the last zero-based index of the first item in a sequence where the predicate returns true,
        /// and return the matched item as an output parameter.
        /// </summary>
        ///
        /// <typeparam name="T">
        /// Generic type parameter.
        /// </typeparam>
        /// <param name="list">
        /// The sequence to search through.
        /// </param>
        /// <param name="predicate">
        /// The predicate.
        /// </param>
        /// <param name="item">
        /// [out] The matched item.
        /// </param>
        ///
        /// <returns>
        /// The zero-based index of the last match, or -1 if not found
        /// </returns>

        public static int LastIndexOf<T>(this IEnumerable<T> list, Func<T, bool> predicate, out T item)
        {
            int index = 0;
            int foundIndex = -1;
            item = default(T);

            var enumerator = list.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (predicate(enumerator.Current))
                {
                    item = enumerator.Current;
                    foundIndex = index;
                }
                index++;
            }

            return foundIndex;
        }

        /// <summary>
        /// Return the zero-based index of item in a sequence.
        /// </summary>
        ///
        /// <typeparam name="T">
        /// The type of elements in the sequence.
        /// </typeparam>
        /// <param name="list">
        /// The sequence to search through.
        /// </param>
        /// <param name="target">
        /// The target collection.
        /// </param>
        ///
        /// <returns>
        /// The zero-based position in the list where the item was found, or -1 if it was not found.
        /// </returns>

        public static int IndexOf<T>(this IEnumerable<T> list, T target)
        {
            int index = 0;
            foreach (var item in list)
            {
                if (item.Equals(target))
                {
                    return index;
                }
                index++;
            }
            return -1;
        }


        /// <summary>
        /// Iterate over a sequence, calling the delegate for each element.
        /// </summary>
        ///
        /// <typeparam name="T">
        /// The type of object in the sequence.
        /// </typeparam>
        /// <param name="list">
        /// The sequence.
        /// </param>
        /// <param name="action">
        /// The action to invoke for each object.
        /// </param>

        public static void ForEach<T>(this IEnumerable<T> list, Action<T> action)
        {
            foreach (T obj in list)
            {
                action(obj);
            }
        }

        /// <summary>
        /// Iterate over a sequence, calling the delegate for each element. The delegate should accept
        /// two parameters, the object T and the index of the current iteration.
        /// </summary>
        ///
        /// <typeparam name="T">
        /// The type of object in the sequence.
        /// </typeparam>
        /// <param name="list">
        /// The sequence.
        /// </param>
        /// <param name="action">
        /// The action to invoke for each object.
        /// </param>

        public static void ForEach<T>(this IEnumerable<T> list, Action<T,int> action)
        {
            int index=0;
            foreach (T obj in list)
            {
                action(obj,index++);
            }
        }

        #endregion

        #region JSON extension methods

        /// <summary>
        /// Serailize the object to a JSON string
        /// </summary>
        /// <param name="objectToSerialize"></param>
        /// <returns></returns>
        
        public static string ToJSON(this object objectToSerialize)
        {
            return JSON.ToJSON(objectToSerialize);
        }
        /// <summary>
        /// Deserialize the JSON string to a typed object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objectToDeserialize"></param>
        /// <returns></returns>
        
        public static T ParseJSON<T>(this string objectToDeserialize)
        {
            return JSON.ParseJSON<T>(objectToDeserialize);
        }

        /// <summary>
        /// Deserialize the JSON string to a dynamic object or a single value.
        /// </summary>
        ///
        /// <param name="json">
        /// The JSON string.
        /// </param>
        ///
        /// <returns>
        /// A new object created from the json.
        /// </returns>

        public static object ParseJSON(this string json)
        {
            return JSON.ParseJSON(json);
        }

        #endregion

        #region Dynamic object extension methods
        /// <summary>
        /// Indicates whether a property exists on an ExpandoObject
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static bool HasProperty(this DynamicObject obj, string propertyName)
        {
            return ((IDictionary<string, object>)obj).ContainsKey(propertyName);
        }

        /// <summary>
        /// Return a typed value from a dynamic object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static T Get<T>(this DynamicObject obj, string name)
        {
            if (obj == null)
            {
                return default(T);
            }
            var dict = (IDictionary<string, object>)obj;
            object val;
            if (dict.TryGetValue(name, out val))
            {
                return Objects.Convert<T>(val);
            }
            else
            {
                return default(T);
            }
        }

        #endregion

        #region Miscellaneous / CsQuery specific

        /// <summary>
        /// Clone a sequence of elements to a new sequence
        /// </summary>
        ///
        /// <param name="source">
        /// The source sequence
        /// </param>
        ///
        /// <returns>
        /// A sequence containing a clone of each element in the source.
        /// </returns>

        public static IEnumerable<IDomObject> Clone(this IEnumerable<IDomObject> source)
        {
            foreach (var item in source)
            {
                yield return item.Clone();
            }
        }

        /// <summary>
        /// Reduce the set of matched elements to a subset beginning with the 0-based index provided.
        /// </summary>
        ///
        /// <param name="array">
        /// The array to act on.
        /// </param>
        /// <param name="start">
        /// The 0-based index at which to begin selecting.
        /// </param>
        /// <param name="end">
        /// The 0-based index of the element at which to stop selecting. The actual element at this
        /// position is not included in the result.
        /// </param>
        ///
        /// <returns>
        /// A new array of the same type as the original.
        /// </returns>
        
        public static Array Slice(this Array array, int start, int end)
        {
            // handle negative values

            if (start < 0)
            {
                start = array.Length + start;
                if (start < 0) { start = 0; }
            }
            if (end < 0)
            {
                end = array.Length + end;
                if (end < 0) { end = 0; }
            }
            if (end >= array.Length)
            {
                end = array.Length;
            }


            int length = end - start;

            Type arrayType = array.GetType().GetElementType();
            Array output =  Array.CreateInstance(arrayType,length);

            int newIndex = 0;
            for (int i=start;i<end;i++) {
                output.SetValue(array.GetValue(i), newIndex++);
            }

            return output;
        
        }

        /// <summary>
        /// Reduce the set of matched elements to a subset beginning with the 0-based index provided.
        /// </summary>
        ///
        /// <param name="array">
        /// The array to act on.
        /// </param>
        /// <param name="start">
        /// The 0-based index at which to begin selecting.
        /// </param>
        ///
        /// <returns>
        /// A new array of the same type as the original.
        /// </returns>

        public static Array Slice(this Array array, int start)
        {
            return Slice(array, start, array.Length);
        }

        #endregion
    }
    
}
