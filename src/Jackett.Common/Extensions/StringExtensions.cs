using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Jackett.Common.Extensions
{
    public static class StringExtensions
    {
        public static bool ContainsIgnoreCase(this string source, string contains) => source != null && contains != null && CultureInfo.InvariantCulture.CompareInfo.IndexOf(source, contains, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;

        public static bool ContainsIgnoreCase(this IEnumerable<string> source, string value) => source.Contains(value, StringComparer.InvariantCultureIgnoreCase);
    }
}
