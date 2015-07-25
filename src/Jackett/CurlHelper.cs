using CurlSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using Jackett.Utils;
using System.Net;

namespace Jackett
{
    public class CurlHelper
    {
        private static readonly object instance = new object();

        static CurlHelper()
        {
            Engine.Logger.Debug("LibCurl init" + Curl.GlobalInit(CurlInitFlag.All).ToString());
            Engine.Logger.Debug("LibCurl version " + Curl.Version);
        }

        public class CurlRequest
        {

            public string Url { get; private set; }

            public string Cookies { get; private set; }

            public string Referer { get; private set; }

            public HttpMethod Method { get; private set; }

            public Dictionary<string, string> PostData { get; set; }

            public CurlRequest(HttpMethod method, string url, string cookies = null, string referer = null)
            {
                Method = method;
                Url = url;
                Cookies = cookies;
                Referer = referer;
            }
        }

        public class CurlResponse
        {
            public Dictionary<string, string> Headers { get; private set; }
            public List<string[]> HeaderList { get; private set; }
            public byte[] Content { get; private set; }
            public HttpStatusCode Status { get; private set;}
            public Dictionary<string, string> Cookies { get; private set; }
            public List<string> CookiesFlat { get { return Cookies.Select(c => c.Key + "=" + c.Value).ToList(); } }
            public string CookieHeader { get { return string.Join("; ", CookiesFlat); } }

            public CurlResponse(List<string[]> headers, byte[] content, HttpStatusCode s)
            {
                Headers = new Dictionary<string, string>();
                Cookies = new Dictionary<string, string>();
                HeaderList = headers;
                Content = content;
                Status = s;
                foreach (var h in headers)
                {
                    Headers[h[0]] = h[1];
                }
            }

            public void AddCookiesFromHeaderValue(string cookieHeaderValue)
            {
                var rawCookies = cookieHeaderValue.Split(';');
                foreach (var rawCookie in rawCookies)
                {
                    var parts = rawCookie.Split(new char[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 1)
                        Cookies[rawCookie.Trim()] = string.Empty;
                    else
                        Cookies[parts[0].Trim()] = parts[1].Trim();
                }
            }

            public void AddCookiesFromHeaders(List<string[]> headers)
            {
                foreach (var h in headers)
                {
                    if (h[0] == "set-cookie")
                    {
                        AddCookiesFromHeaderValue(h[1]);
                    }
                }
            }
        }

        public static async Task<CurlResponse> GetAsync(string url, string cookies = null, string referer = null)
        {
            var curlRequest = new CurlRequest(HttpMethod.Get, url, cookies, referer);
            var result = await PerformCurlAsync(curlRequest);
            return result;
        }    

        public static async Task<CurlResponse> PostAsync(string url, Dictionary<string, string> formData, string cookies = null, string referer = null)
        {
            var curlRequest = new CurlRequest(HttpMethod.Post, url, cookies, referer);
            curlRequest.PostData = formData;
            var result = await PerformCurlAsync(curlRequest);
            return result;
        }

        private static async Task<CurlResponse> FollowRedirect(string url, CurlResponse response)
        {
            var uri = new Uri(url);
            string redirect;
            if (response.Headers.TryGetValue("location", out redirect))
            {
                string cookie = response.CookieHeader;
                if (!redirect.StartsWith("http://") && !redirect.StartsWith("https://"))
                {
                    if (redirect.StartsWith("/"))
                        redirect = string.Format("{0}://{1}{2}", uri.Scheme, uri.Host, redirect);
                    else
                        redirect = string.Format("{0}://{1}/{2}", uri.Scheme, uri.Host, redirect);
                }
                var newRedirect = await GetAsync(redirect, cookie);
                foreach (var c in response.Cookies)
                    newRedirect.Cookies[c.Key] = c.Value;
                newRedirect.AddCookiesFromHeaders(response.HeaderList);
                return newRedirect;
            }
            else
                return response;
        }


        public static async Task<CurlResponse> PerformCurlAsync(CurlRequest curlRequest)
        {
            return await Task.Run(() => PerformCurl(curlRequest));
        }

        public static CurlResponse PerformCurl(CurlRequest curlRequest)
        {
            lock (instance)
            {
                var headerBuffers = new List<byte[]>();
                var contentBuffers = new List<byte[]>();

                using (var easy = new CurlEasy())
                {
                    easy.Url = curlRequest.Url;
                    easy.BufferSize = 64 * 1024;
                    easy.UserAgent = BrowserUtil.ChromeUserAgent;
                    easy.FollowLocation = false;
                    easy.ConnectTimeout  = 20;
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

                    if (!string.IsNullOrEmpty(curlRequest.Referer))
                        easy.Referer = curlRequest.Referer;

                    if (curlRequest.Method == HttpMethod.Post)
                    {
                        easy.Post = true;
                        var postString = new FormUrlEncodedContent(curlRequest.PostData).ReadAsStringAsync().Result;
                        easy.PostFields = postString;
                        easy.PostFieldSize = Encoding.UTF8.GetByteCount(postString);
                    }

                    easy.Perform();
                }

                var headerBytes = Combine(headerBuffers.ToArray());
                var headerString = Encoding.UTF8.GetString(headerBytes);
                var headerParts = headerString.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                var headers = new List<string[]>();
                var headerCount = 0;
                HttpStatusCode status = HttpStatusCode.InternalServerError;
                foreach (var headerPart in headerParts)
                {
                    if (headerCount == 0)
                    {
                        var responseCode = int.Parse(headerPart.Split(' ')[1]);
                        status = (HttpStatusCode)responseCode;
                    }
                    else
                    {
                        var keyVal = headerPart.Split(new char[] { ':' }, 2);
                        if (keyVal.Length > 1)
                        {
                            headers.Add(new[] { keyVal[0].ToLower().Trim(), keyVal[1].Trim() });
                        }
                    }

                    headerCount++;
                }

                var contentBytes = Combine(contentBuffers.ToArray());

                var curlResponse = new CurlResponse(headers, contentBytes, status);
                if (!string.IsNullOrEmpty(curlRequest.Cookies))
                    curlResponse.AddCookiesFromHeaderValue(curlRequest.Cookies);
                curlResponse.AddCookiesFromHeaders(headers);

                return curlResponse;
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
