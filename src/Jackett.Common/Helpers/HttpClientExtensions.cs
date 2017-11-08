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
    }
}
