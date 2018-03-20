using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Jackett.Common.Helpers
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
    }
}
