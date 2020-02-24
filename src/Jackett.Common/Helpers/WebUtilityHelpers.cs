using System.Net;
using System.Text;

namespace Jackett.Common.Helpers
{
    public static class WebUtilityHelpers
    {
        public static string UrlEncode(string searchString, Encoding encoding)
        {
            if (string.IsNullOrEmpty(searchString))
            {
                return string.Empty;
            }

            var bytes = encoding.GetBytes(searchString);
            return encoding.GetString(WebUtility.UrlEncodeToBytes(bytes, 0, bytes.Length));
        }

        public static string UrlDecode(string searchString, Encoding encoding)
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
