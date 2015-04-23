using CurlSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Jackett
{
    public class CurlHelper
    {
        private const string ChromeUserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/42.0.2311.90 Safari/537.36";

        public static CurlHelper Shared = new CurlHelper();

        private BlockingCollection<CurlRequest> curlRequests;

        public bool IsSupported { get; private set; }

        private class CurlRequest
        {
            public TaskCompletionSource<string> TaskCompletion { get; private set; }

            public string Url { get; private set; }

            public string Cookies { get; private set; }

            public string Referer { get; private set; }

            public HttpMethod Method { get; private set; }

            public string PostData { get; set; }

            public CurlRequest(HttpMethod method, string url, string cookies = null, string referer = null)
            {
                TaskCompletion = new TaskCompletionSource<string>();
                Method = method;
                Url = url;
                Cookies = cookies;
                Referer = referer;
            }
        }

        public async Task<string> GetStringAsync(string url, string cookies = null, string referer = null)
        {
            var curlRequest = new CurlRequest(HttpMethod.Get, url, cookies, referer);
            curlRequests.Add(curlRequest);
            var result = await curlRequest.TaskCompletion.Task;
            return result;
        }

        public CurlHelper()
        {
            try
            {
                Curl.GlobalInit(CurlInitFlag.All);
                IsSupported = true;
            }
            catch (Exception ex)
            {
                IsSupported = false;
            }
            Task.Run((Action)CurlServicer);
        }

        private void CurlServicer()
        {
            curlRequests = new BlockingCollection<CurlRequest>();
            foreach (var curlRequest in curlRequests.GetConsumingEnumerable())
            {
                PerformCurl(curlRequest);
            }
        }

        private void PerformCurl(CurlRequest curlRequest)
        {
            var headerBuffers = new List<byte[]>();
            var contentBuffers = new List<byte[]>();
            try
            {
                using (var easy = new CurlEasy())
                {
                    easy.Url = curlRequest.Url;
                    easy.BufferSize = 64 * 1024;
                    //easy.Encoding = "UTF8";
                    easy.WriteFunction = (byte[] buf, int size, int nmemb, object data) =>
                    {
                        contentBuffers.Add(buf);
                        return size * nmemb;
                    };
                    easy.HeaderFunction = (byte[] buf, int size, int nmemb, object extraData) =>
                    {
                        headerBuffers.Add(buf);
                        return size * nmemb;
                    };

                    if (!string.IsNullOrEmpty(curlRequest.Cookies))
                        easy.Cookie = curlRequest.Cookies;

                    if (curlRequest.Method == HttpMethod.Post)
                    {
                        easy.Post = true;
                        easy.PostFields = curlRequest.PostData;
                        easy.PostFieldSize = Encoding.UTF8.GetByteCount(curlRequest.PostData);
                    }

                    easy.Perform();
                }

                var headerBytes = Combine(headerBuffers.ToArray());
                var headerString = Encoding.UTF8.GetString(headerBytes);

                var contentBytes = Combine(contentBuffers.ToArray());
                var result = Encoding.UTF8.GetString(contentBytes);
                curlRequest.TaskCompletion.SetResult(result);
            }
            catch (Exception ex)
            {
                curlRequest.TaskCompletion.TrySetException(ex);
            }
        }

        public static byte[] Combine(params byte[][] arrays)
        {
            byte[] ret = new byte[arrays.Sum(x => x.Length)];
            int offset = 0;
            foreach (byte[] data in arrays)
            {
                Buffer.BlockCopy(data, 0, ret, offset, data.Length);
                offset += data.Length;
            }
            return ret;
        }
    }
}
