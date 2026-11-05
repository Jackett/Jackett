using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Jackett.Common.Utils
{
    public static class CookieUtil
    {
        // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Set-Cookie
        // NOTE: we are not checking non-ascii characters and we should
        private static readonly Regex _CookieRegex = new Regex(@"([^\(\)<>@,;:\\""/\[\]\?=\{\}\s]+)=([^,;\\""\s]+)");
        private static readonly Regex _CookieDateIgnoredRegex = new Regex(@"^\s*(mon|tue|wed|thu|fri|sat|sun)[a-z]*,?\s+|\s*\b(gmt|utc)([+-]00:?00)?$", RegexOptions.IgnoreCase);
        private static readonly char[] InvalidKeyChars = { '(', ')', '<', '>', '@', ',', ';', ':', '\\', '"', '/', '[', ']', '?', '=', '{', '}', ' ', '\t', '\n' };
        private static readonly char[] InvalidValueChars = { '"', ',', ';', '\\', ' ', '\t', '\n' };

        // cookie headers are passed around as strings, this value marks a cookie deleted by the server
        public const string ExpiredCookieValue = "__jackett_expired__";

        public static Dictionary<string, string> CookieHeaderToDictionary(string cookieHeader)
        {
            var cookieDictionary = new Dictionary<string, string>();
            if (cookieHeader == null)
                return cookieDictionary;
            var matches = _CookieRegex.Match(cookieHeader);
            while (matches.Success)
            {
                if (matches.Groups.Count > 2 && matches.Groups[2].Value == ExpiredCookieValue)
                    cookieDictionary.Remove(matches.Groups[1].Value);
                else if (matches.Groups.Count > 2)
                    cookieDictionary[matches.Groups[1].Value] = matches.Groups[2].Value;
                matches = matches.NextMatch();
            }
            return cookieDictionary;
        }

        public static string CookieDictionaryToHeader(Dictionary<string, string> cookieDictionary)
        {
            if (cookieDictionary == null)
                return "";
            foreach (var kv in cookieDictionary)
                if (kv.Key.IndexOfAny(InvalidKeyChars) > -1 || kv.Value.IndexOfAny(InvalidValueChars) > -1)
                    throw new FormatException($"The cookie '{kv.Key}={kv.Value}' is malformed.");
            return string.Join("; ", cookieDictionary.Select(kv => kv.Key + "=" + kv.Value));
        }

        /// <summary>
        /// Convert a Set-Cookie header into a "name=value;" pair. If the server expired the cookie
        /// (Max-Age &lt;= 0 or Expires in the past) the value is replaced with ExpiredCookieValue.
        /// </summary>
        public static string SetCookieToCookiePair(string setCookie)
        {
            var attributes = setCookie.Split(';');
            var nameSplit = attributes[0].IndexOf('=');
            if (nameSplit == -1)
                return attributes[0] + ";";

            bool? maxAgeExpired = null;
            bool? expiresExpired = null;
            foreach (var attribute in attributes.Skip(1))
            {
                var kv = attribute.Split(new[] { '=' }, 2);
                var key = kv[0].Trim();
                var value = kv.Length > 1 ? kv[1].Trim() : "";

                if (key.Equals("max-age", StringComparison.OrdinalIgnoreCase) && long.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var maxAge))
                    maxAgeExpired = maxAge <= 0;
                else if (key.Equals("expires", StringComparison.OrdinalIgnoreCase) && TryParseCookieDate(value, out var expires))
                    expiresExpired = expires < DateTime.UtcNow;
            }

            // Max-Age has precedence over Expires (RFC 6265)
            return (maxAgeExpired ?? expiresExpired) == true
                ? attributes[0].Substring(0, nameSplit) + "=" + ExpiredCookieValue + ";"
                : attributes[0] + ";";
        }

        private static bool TryParseCookieDate(string value, out DateTime date)
        {
            // DateTime.TryParse rejects "UTC", "GMT+0000", the asctime format and a wrong weekday, all seen in the wild.
            // Browsers ignore the weekday (RFC 6265).
            value = _CookieDateIgnoredRegex.Replace(value, "");
            const DateTimeStyles styles = DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal | DateTimeStyles.AllowWhiteSpaces;
            return DateTime.TryParse(value, CultureInfo.InvariantCulture, styles, out date) ||
                   DateTime.TryParseExact(value, "MMM d HH:mm:ss yyyy", CultureInfo.InvariantCulture, styles, out date);
        }

        /// <summary>
        /// Remove all the cookies from a CookieContainer. That includes all domains and protocols.
        /// </summary>
        /// <param name="cookieJar">A cookie container</param>
        public static void RemoveAllCookies(CookieContainer cookieJar)
        {
            var table = (Hashtable)cookieJar
                                   .GetType()
                                   .InvokeMember("m_domainTable", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance, null, cookieJar, Array.Empty<object>());

            foreach (var tableKey in table.Keys)
            {
                var domain = (string)tableKey;

                if (domain.StartsWith("."))
                {
                    domain = domain.Substring(1);
                }

                var list = (SortedList)table[tableKey]
                                        .GetType()
                                        .InvokeMember("m_list", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance, null, table[tableKey], Array.Empty<object>());

                foreach (var listKey in list.Keys)
                {
                    foreach (Cookie cookie in cookieJar.GetCookies(new Uri($"http://{domain}{listKey}")))
                    {
                        cookie.Expired = true;
                    }

                    foreach (Cookie cookie in cookieJar.GetCookies(new Uri($"https://{domain}{listKey}")))
                    {
                        cookie.Expired = true;
                    }
                }

                foreach (Cookie cookie in cookieJar.GetCookies(new Uri($"http://{domain}")))
                {
                    cookie.Expired = true;
                }

                foreach (Cookie cookie in cookieJar.GetCookies(new Uri($"https://{domain}")))
                {
                    cookie.Expired = true;
                }
            }
        }

    }
}
