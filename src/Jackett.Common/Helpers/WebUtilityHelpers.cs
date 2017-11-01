using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Jacket.Common.Helpers
{
    public static class WebUtilityHelpers
    {
        //TODO: https://docs.microsoft.com/en-us/dotnet/api/system.text.codepagesencodingprovider?view=netcore-2.0
        //https://www.nuget.org/packages/System.Text.Encoding.CodePages/
        //Tests?

        internal static string UrlEncode(string searchString, Encoding encoding)
        {
            if (string.IsNullOrEmpty(searchString))
            {
                return string.Empty;
            }

            byte[] bytes = encoding.GetBytes(searchString);
            return encoding.GetString(WebUtility.UrlEncodeToBytes(bytes,0, bytes.Length));
        }

        internal static string UrlDecode(string searchString, Encoding encoding)
        {
            if (string.IsNullOrEmpty(searchString))
            {
                return string.Empty;
            }

            var inputBytes = encoding.GetBytes(searchString);
            return encoding.GetString(WebUtility.UrlDecodeToBytes(inputBytes, 0, inputBytes.Length));
        }
    }
}
