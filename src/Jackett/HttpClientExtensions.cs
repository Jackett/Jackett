using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Jackett
{
    public static class HttpClientExtensions
    {
        public static async Task<string> GetStringAsync(this HttpClient client, string uri, int retries)
        {
            Exception exception = null;
            try
            {
                return await client.GetStringAsync(uri);
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            if (retries > 0)
                return await client.GetStringAsync(uri, --retries);
            throw exception;
        }

        public static string ChromeUserAgent(this HttpClient client)
        {
            return "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2272.118 Safari/537.36";
        }
    }
}
