using AngleSharp.Dom;
using AngleSharp.Html;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Jackett.Utils
{
    public static class StringUtil
    {
        public static string StripNonAlphaNumeric(this string str, string replacement = "")
        {
            return StripRegex(str, "[^a-zA-Z0-9 -]", replacement);
        }

        public static string StripRegex(string str, string regex, string replacement = "")
        {
            Regex rgx = new Regex(regex);
            str = rgx.Replace(str, replacement);
            return str;
        }

        // replaces culture specific characters with the corresponding base characters (e.g. è becomes e).
        public static String RemoveDiacritics(String s)
        {
            String normalizedString = s.Normalize(NormalizationForm.FormD);
            StringBuilder stringBuilder = new StringBuilder();

            for (int i = 0; i < normalizedString.Length; i++)
            {
                Char c = normalizedString[i];
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    stringBuilder.Append(c);
            }

            return stringBuilder.ToString();
        }

        public static string FromBase64(string str)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(str));
        }

        public static string PostDataFromDict(IEnumerable<KeyValuePair<string, string>> dict)
        {
            return new FormUrlEncodedContent(dict).ReadAsStringAsync().Result;
        }

        /// <summary>
        /// Convert an array of bytes to a string of hex digits
        /// </summary>
        /// <param name="bytes">array of bytes</param>
        /// <returns>String of hex digits</returns>
        public static string HexStringFromBytes(byte[] bytes)
        {
            var sb = new StringBuilder();
            foreach (byte b in bytes)
            {
                var hex = b.ToString("x2");
                sb.Append(hex);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Compute hash for string encoded as UTF8
        /// </summary>
        /// <param name="s">String to be hashed</param>
        /// <returns>40-character hex string</returns>
        public static string HashSHA1(string s)
        {
            var sha1 = SHA1.Create();

            byte[] bytes = Encoding.UTF8.GetBytes(s);
            byte[] hashBytes = sha1.ComputeHash(bytes);

            return HexStringFromBytes(hashBytes);
        }

        public static string Hash(string s)
        {
            // Use input string to calculate MD5 hash
            MD5 md5 = System.Security.Cryptography.MD5.Create();

            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(s);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            return HexStringFromBytes(hashBytes);
        }

        public static string GetExceptionDetails(this Exception exception)
        {
            var properties = exception.GetType()
                                    .GetProperties();
            var fields = properties
                             .Select(property => new
                             {
                                 Name = property.Name,
                                 Value = property.GetValue(exception, null)
                             })
                             .Select(x => String.Format(
                                 "{0} = {1}",
                                 x.Name,
                                 x.Value != null ? x.Value.ToString() : String.Empty
                             ));
            return String.Join("\n", fields);
        }

        static char[] MakeValidFileName_invalids;

        /// <summary>Replaces characters in <c>text</c> that are not allowed in 
        /// file names with the specified replacement character.</summary>
        /// <param name="text">Text to make into a valid filename. The same string is returned if it is valid already.</param>
        /// <param name="replacement">Replacement character, or null to simply remove bad characters.</param>
        /// <param name="fancy">Whether to replace quotes and slashes with the non-ASCII characters ” and ⁄.</param>
        /// <returns>A string that can be used as a filename. If the output string would otherwise be empty, returns "_".</returns>
        public static string MakeValidFileName(string text, char? replacement = '_', bool fancy = true)
        {
            StringBuilder sb = new StringBuilder(text.Length);
            var invalids = MakeValidFileName_invalids ?? (MakeValidFileName_invalids = Path.GetInvalidFileNameChars());
            bool changed = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (invalids.Contains(c))
                {
                    changed = true;
                    var repl = replacement ?? '\0';
                    if (fancy)
                    {
                        if (c == '"') repl = '”'; // U+201D right double quotation mark
                        else if (c == '\'') repl = '’'; // U+2019 right single quotation mark
                        else if (c == '/') repl = '⁄'; // U+2044 fraction slash
                    }
                    if (repl != '\0')
                        sb.Append(repl);
                }
                else
                    sb.Append(c);
            }
            if (sb.Length == 0)
                return "_";
            return changed ? sb.ToString() : text;
        }

        public static string GetQueryString(this NameValueCollection collection, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;
            return string.Join("&", collection.AllKeys.Select(a => a + "=" + HttpUtility.UrlEncode(collection[a], encoding)));
        }

        public static string GetQueryString(this ICollection<KeyValuePair<string, string>> collection, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;
            return string.Join("&", collection.Select(a => a.Key + "=" + HttpUtility.UrlEncode(a.Value, encoding)));
        }

        public static void Add(this ICollection<KeyValuePair<string, string>> collection, string key, string value)
        {
            collection.Add(new KeyValuePair<string, string>(key, value));
        }

        public static string ToHtmlPretty(this IElement element)
        {
            if (element == null)
                return "<NULL>";

            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);
            var formatter = new PrettyMarkupFormatter();
            element.ToHtml(sw, formatter);
            return sb.ToString();
        }



        public static string GenerateRandom(int length)
        {
            var chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            var randBytes = new byte[length];
            using (var rngCsp = new RNGCryptoServiceProvider())
            {
                rngCsp.GetBytes(randBytes);
                var key = "";
                foreach (var b in randBytes)
                {
                    key += chars[b % chars.Length];
                }
                return key;
            }
        }
    }
}
