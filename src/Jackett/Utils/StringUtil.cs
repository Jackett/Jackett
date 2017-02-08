using AngleSharp.Dom;
using AngleSharp.Html;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
        public static string StripNonAlphaNumeric(string str, string replacement = "")
        {
            return StripRegex(str, "[^a-zA-Z0-9 -]", replacement);
        }

        public static string StripRegex(string str, string regex, string replacement = "")
        {
            Regex rgx = new Regex(regex);
            str = rgx.Replace(str, replacement);
            return str;
        }

        public static string FromBase64(string str)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(str));
        }

        public static string PostDataFromDict(IEnumerable<KeyValuePair<string, string>> dict)
        {
            return new FormUrlEncodedContent(dict).ReadAsStringAsync().Result;
        }

        public static string Hash(string input)
        {
            // Use input string to calculate MD5 hash
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            // Convert the byte array to hexadecimal string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("X2"));
            }
            return sb.ToString();
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

        public static string GetQueryString(this NameValueCollection collection, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;
            return string.Join("&", collection.AllKeys.Select(a => a + "=" + HttpUtility.UrlEncode(collection[a], encoding)));
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
