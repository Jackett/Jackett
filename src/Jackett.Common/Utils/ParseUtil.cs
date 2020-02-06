using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;

namespace Jackett.Common.Utils
{
    public static class ParseUtil
    {
        private static readonly Regex InvalidXmlChars = new Regex(
            @"(?<![\uD800-\uDBFF])[\uDC00-\uDFFF]|[\uD800-\uDBFF](?![\uDC00-\uDFFF])|[\x00-\x08\x0B\x0C\x0E-\x1F\x7F-\x9F\uFEFF\uFFFE\uFFFF]",
            RegexOptions.Compiled);

        private static readonly Regex ImdbId = new Regex(@"^(?:tt)?(\d{1,8})$", RegexOptions.Compiled);

        public static string NormalizeSpace(string s) => s.Trim();

        public static string NormalizeMultiSpaces(string s) => new Regex(@"\s+").Replace(NormalizeSpace(s), " ");

        public static string NormalizeNumber(string s)
        {
            var normalized = NormalizeSpace(s);
            normalized = normalized.Replace("-", "0");
            normalized = normalized.Replace(",", "");
            return normalized;
        }

        public static string RemoveInvalidXmlChars(string text) =>
            string.IsNullOrEmpty(text) ? "" : InvalidXmlChars.Replace(text, "");

        public static double CoerceDouble(string str) => double.Parse(
            NormalizeNumber(str), NumberStyles.Any, CultureInfo.InvariantCulture);

        public static float CoerceFloat(string str) => float.Parse(
            NormalizeNumber(str), NumberStyles.Any, CultureInfo.InvariantCulture);

        public static int CoerceInt(string str) => int.Parse(
            NormalizeNumber(str), NumberStyles.Any, CultureInfo.InvariantCulture);

        public static long CoerceLong(string str) => long.Parse(
            NormalizeNumber(str), NumberStyles.Any, CultureInfo.InvariantCulture);

        public static bool TryCoerceDouble(string str, out double result) => double.TryParse(
            NormalizeNumber(str), NumberStyles.Any, CultureInfo.InvariantCulture, out result);

        public static bool TryCoerceFloat(string str, out float result) => float.TryParse(
            NormalizeNumber(str), NumberStyles.Any, CultureInfo.InvariantCulture, out result);

        public static bool TryCoerceInt(string str, out int result) => int.TryParse(
            NormalizeNumber(str), NumberStyles.Any, CultureInfo.InvariantCulture, out result);

        public static bool TryCoerceLong(string str, out long result) => long.TryParse(
            NormalizeNumber(str), NumberStyles.Any, CultureInfo.InvariantCulture, out result);

        public static string GetArgumentFromQueryString(string url, string argument)
        {
            if (url == null || argument == null)
                return null;
            var qsStr = url.Split(
                new[]
                {
                    '?'
                }, 2)[1];
            qsStr = qsStr.Split(
                new[]
                {
                    '#'
                }, 2)[0];
            var qs = QueryHelpers.ParseQuery(qsStr);
            return qs[argument].FirstOrDefault();
        }

        public static long? GetLongFromString(string str)
        {
            if (str == null)
                return null;
            var idRegEx = new Regex(@"(\d+)", RegexOptions.Compiled);
            var idMatch = idRegEx.Match(str);
            if (!idMatch.Success)
                return null;
            var id = idMatch.Groups[1].Value;
            return CoerceLong(id);
        }

        public static int? GetImdbID(string imdbstr)
        {
            if (imdbstr == null)
                return null;
            var match = ImdbId.Match(imdbstr);
            return !match.Success ? null : (int?)int.Parse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture);
        }

        public static string GetFullImdbID(string imdbstr)
        {
            var imdbid = GetImdbID(imdbstr);
            return imdbid == null ? null : $"tt{((int)imdbid):D7}";
        }
    }
}
