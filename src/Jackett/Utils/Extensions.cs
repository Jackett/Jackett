using System;
using System.Collections.Generic;

namespace Jackett.Utils
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

    public static class ToEnumerableExtension
    {
        public static IEnumerable<T> ToEnumerable<T>(this T obj)
        {
            return new T[] { obj };
        }
    }

    public static class ToNonNullExtension
    {
        public static NonNull<T> ToNonNull<T>(this T obj) where T : class
        {
            return new NonNull<T>(obj);
        }
    }

    public static class StringExtension
    {
        public static bool IsNullOrEmptyOrWhitespace(this string str)
        {
            return string.IsNullOrEmpty(str) || string.IsNullOrWhiteSpace(str);
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
}
