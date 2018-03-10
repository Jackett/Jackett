using System;
using System.Text.RegularExpressions;

namespace Jackett.Common.Utils
{
    public static class BrowserUtil
    {
        public static string ChromeUserAgent
        {
            get {
                // When updating these make sure they are not detected by the incapsula bot detection engine (e.g. kickasstorrent indexer)
                if (System.Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    return "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36";
                }
                else
                {
                    return "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) 63.0.3239.132 Safari/537.36";
                }
            }
        }

        // This can be used to decode e-mail addresses protected by cloudflare
        public static string DecodeCloudFlareProtectedEmail(string input)
        {
            var key = Convert.ToInt32(input.Substring(0, 2), 16);
            string result = "";
            for (var i = 2; i < input.Length - 1; i += 2)
            {
                var hexChar = input.Substring(i, 2);
                var intChar = Convert.ToInt32(hexChar, 16) ^ key;
                var strChar = Convert.ToChar(intChar);
                result += strChar;
            }
            return result;
        }

        // decode cloudflare protected emails in a HTML document
        public static string DecodeCloudFlareProtectedEmailFromHTML(string html)
        {
            Regex CFEMailRegex = new Regex("<span class=\"__cf_email__\" data-cfemail=\"(\\w+)\">\\[email&#160;protected\\]<\\/span><script data-cfhash='[\\w]+' type=\"text\\/javascript\">.*?<\\/script>", RegexOptions.Compiled);
            var CFEMailRegexMatches = CFEMailRegex.Match(html);

            while (CFEMailRegexMatches.Success)
            {
                string all = CFEMailRegexMatches.Groups[0].Value;
                string cfemail = CFEMailRegexMatches.Groups[1].Value;
                var decoded = DecodeCloudFlareProtectedEmail(cfemail);
                html = html.Replace(all, decoded);
                CFEMailRegexMatches = CFEMailRegexMatches.NextMatch();
            }
            return html;
        }
    }
}
