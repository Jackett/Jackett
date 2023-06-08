using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Jackett.Common.Extensions
{
    public static class StringExtensions
    {
        public static string Join(this IEnumerable<string> values, string separator) => string.Join(separator, values);

        public static bool IsNullOrWhiteSpace(this string text) => string.IsNullOrWhiteSpace(text);

        public static bool IsNotNullOrWhiteSpace(this string text) => !string.IsNullOrWhiteSpace(text);

        public static bool StartsWithIgnoreCase(this string text, string startsWith) => text.StartsWith(startsWith, StringComparison.InvariantCultureIgnoreCase);

        public static bool EndsWithIgnoreCase(this string text, string startsWith) => text.EndsWith(startsWith, StringComparison.InvariantCultureIgnoreCase);

        public static bool EqualsIgnoreCase(this string text, string equals) => text.Equals(equals, StringComparison.InvariantCultureIgnoreCase);

        public static bool ContainsIgnoreCase(this string source, string contains) => source != null && contains != null && CultureInfo.InvariantCulture.CompareInfo.IndexOf(source, contains, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;

        public static bool ContainsIgnoreCase(this IEnumerable<string> source, string value) => source.Contains(value, StringComparer.InvariantCultureIgnoreCase);

        public static bool IsAllDigits(this string input)
        {
            foreach (var c in input)
            {
                if (c < '0' || c > '9')
                {
                    return false;
                }
            }

            return true;
        }

        public static string Replace(this string text, int index, int length, string replacement)
        {
            text = text.Remove(index, length);
            text = text.Insert(index, replacement);

            return text;
        }
    }
}
