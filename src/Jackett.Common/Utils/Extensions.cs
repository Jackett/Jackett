using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Jackett.Common.Utils
{
    public class NonNullException : Exception
    {
        public NonNullException() : base("Parameter cannot be null")
        {
        }
    }

    public class NonNull<T> where T : class
    {
        public NonNull(T val)
        {
            if (val == null)
                new NonNullException();

            Value = val;
        }

        public static implicit operator T(NonNull<T> n)
        {
            return n.Value;
        }

        private T Value;
    }

    public static class GenericConversionExtensions
    {
        public static IEnumerable<T> ToEnumerable<T>(this T obj)
        {
            return new T[] { obj };
        }

        public static NonNull<T> ToNonNull<T>(this T obj) where T : class
        {
            return new NonNull<T>(obj);
        }
    }

    public static class EnumerableExtension
    {
        public static string AsString(this IEnumerable<char> chars)
        {
            return String.Concat(chars);
        }

        public static bool IsEmpty<T>(this IEnumerable<T> collection)
        {
            return collection.Count() > 0;
        }

        public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> list)
        {
            return list.SelectMany(x => x);
        }
    }

    public static class StringExtension
    {
        public static bool IsNullOrEmptyOrWhitespace(this string str)
        {
            return string.IsNullOrEmpty(str) || string.IsNullOrWhiteSpace(str);
        }

        public static DateTime ToDateTime(this string str)
        {
            return DateTime.Parse(str);
        }

        public static Uri ToUri(this string str)
        {
            return new Uri(str);
        }
    }

    public static class CollectionExtension
    {
        public static bool IsEmpty<T>(this ICollection<T> obj)
        {
            return obj.Count == 0;
        }

        public static bool IsEmptyOrNull<T>(this ICollection<T> obj)
        {
            return obj == null || obj.IsEmpty();
        }
    }

    public static class XElementExtension
    {
        public static XElement First(this XElement element, string name)
        {
            return element.Descendants(name).First();
        }

        public static string FirstValue(this XElement element, string name)
        {
            return element.First(name).Value;
        }
    }

    public static class KeyValuePairsExtension
    {
        public static IDictionary<Key, Value> ToDictionary<Key, Value>(this IEnumerable<KeyValuePair<Key, Value>> pairs)
        {
            return pairs.ToDictionary(x => x.Key, x => x.Value);
        }
    }

    public static class ParseExtension
    {
        public static T? TryParse<T>(this string value) where T : struct
        {
            var type = typeof(T);
            var parseMethods = type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).Where(m => m.Name == "Parse");
            var parseMethod = parseMethods.Where(m =>
            {
                var parameters = m.GetParameters();
                var hasOnlyOneParameter = parameters.Count() == 1;
                var firstParameterIsString = parameters.First().ParameterType == typeof(string);

                return hasOnlyOneParameter && firstParameterIsString;
            }).First();
            if (parseMethod == null)
                return null;
            try
            {
                var val = parseMethod.Invoke(null, new object[] { value });
                return (T)val;
            }
            catch
            {
                return null;
            }
        }
    }

    public static class TaskExtensions
    {
        public static Task<IEnumerable<TResult>> Until<TResult>(this IEnumerable<Task<TResult>> tasks, TimeSpan timeout)
        {
            var timeoutTask = Task.Delay(timeout);
            var aggregateTask = Task.WhenAll(tasks);
            var anyTask = Task.WhenAny(timeoutTask, aggregateTask);
            var continuation = anyTask.ContinueWith((_) =>
            {
                var completedTasks = tasks.Where(t => t.Status == TaskStatus.RanToCompletion);
                var results = completedTasks.Select(t => t.Result);
                return results;
            });

            return continuation;
        }
    }
}
