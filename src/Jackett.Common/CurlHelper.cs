using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CurlSharp;
using CurlSharp.Enums;
using Jackett.Common.Models.Config;
using Jackett.Common.Utils;

namespace Jackett.Common
{
    public class CurlHelper
    {
        private static readonly object instance = new object();

        public class CurlRequest
        {
            public string Url { get; private set; }
            public string Cookies { get; private set; }
            public string Referer { get; private set; }
            public HttpMethod Method { get; private set; }
            public IEnumerable<KeyValuePair<string, string>> PostData { get; set; }
            public Dictionary<string, string> Headers { get; set; }
            public string RawPOSTDdata { get; set; }

            public CurlRequest(HttpMethod method, string url, string cookies = null, string referer = null, Dictionary<string, string> headers = null, string rawPOSTData = null)
            {
                Method = method;
                Url = url.Replace(" ", "+"); // avoids bad request to cloudflare for urls containing a space followed by H (" H")
                Cookies = cookies;
                Referer = referer;
                Headers = headers;
                RawPOSTDdata = rawPOSTData;
            }
        }

        public class CurlResponse
        {
            public List<string[]> HeaderList { get; private set; }
            public byte[] Content { get; private set; }
            public HttpStatusCode Status { get; private set; }
            public string Cookies { set; get; }

            public CurlResponse(List<string[]> headers, byte[] content, HttpStatusCode s, string cookies)
            {
                HeaderList = headers;
                Content = content;
                Status = s;
                Cookies = cookies;
            }
        }

        public static async Task<CurlResponse> GetAsync(string url, ServerConfig config, string cookies = null, string referer = null, Dictionary<string, string> headers = null)
        {
            var curlRequest = new CurlRequest(HttpMethod.Get, url, cookies, referer, headers);
            var result = await PerformCurlAsync(curlRequest, config);
            return result;
        }

        public static async Task<CurlResponse> PostAsync(string url, ServerConfig config, IEnumerable<KeyValuePair<string, string>> formData, string cookies = null, string referer = null, Dictionary<string, string> headers = null, string rawPostData = null)
        {
            var curlRequest = new CurlRequest(HttpMethod.Post, url, cookies, referer, headers);
            curlRequest.PostData = formData;
            curlRequest.RawPOSTDdata = rawPostData;
            var result = await PerformCurlAsync(curlRequest, config);
            return result;
        }

        public static async Task<CurlResponse> PerformCurlAsync(CurlRequest curlRequest, ServerConfig config)
        {
            return await Task.Run(() => PerformCurl(curlRequest, config));
        }

        public delegate void ErrorMessage(string s);
        public static ErrorMessage OnErrorMessage;

        public static CurlResponse PerformCurl(CurlRequest curlRequest, ServerConfig config)
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
                    easy.ConnectTimeout = 20;
                    if (curlRequest.Headers != null)
                    {
                        CurlSlist curlHeaders = new CurlSlist();
                        foreach (var header in curlRequest.Headers)
                        {
                            curlHeaders.Append(header.Key + ": " + header.Value);
                        }
                        easy.SetOpt(CurlOption.HttpHeader, curlHeaders);
                    }

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
                        if (!string.IsNullOrEmpty(curlRequest.RawPOSTDdata))
                        {
                            easy.Post = true;
                            easy.PostFields = curlRequest.RawPOSTDdata;
                            easy.PostFieldSize = Encoding.UTF8.GetByteCount(curlRequest.RawPOSTDdata);
                        }
                        else
                        {
                            easy.Post = true;
                            var postString = StringUtil.PostDataFromDict(curlRequest.PostData);
                            easy.PostFields = postString;
                            easy.PostFieldSize = Encoding.UTF8.GetByteCount(postString);
                        }
                    }

                    if (config.RuntimeSettings.DoSSLFix == true)
                    {
                        // http://stackoverflow.com/questions/31107851/how-to-fix-curl-35-cannot-communicate-securely-with-peer-no-common-encryptio
                        // https://git.fedorahosted.org/cgit/mod_nss.git/plain/docs/mod_nss.html
                        easy.SslCipherList = SSLFix.CipherList;
                        easy.FreshConnect = true;
                        easy.ForbidReuse = true;
                    }

                    if (config.RuntimeSettings.IgnoreSslErrors == true)
                    {
                        easy.SetOpt(CurlOption.SslVerifyhost, false);
                        easy.SetOpt(CurlOption.SslVerifyPeer, false);
                    }

                    var proxy = config.GetProxyUrl();
                    if (proxy != null)
                    {
                        easy.SetOpt(CurlOption.HttpProxyTunnel, 1);
                        easy.SetOpt(CurlOption.Proxy, proxy);

                        var authString = config.GetProxyAuthString();
                        if (authString != null)
                        {
                            easy.SetOpt(CurlOption.ProxyUserPwd, authString);
                        }
                    }

                    easy.Perform();

                    if (easy.LastErrorCode != CurlCode.Ok)
                    {
                        var message = "Error " + easy.LastErrorCode.ToString() + " " + easy.LastErrorDescription + " " + easy.ErrorBuffer;
                        if (null != OnErrorMessage)
                            OnErrorMessage(message);
                        else
                            Console.WriteLine(message);
                    }
                }

                var headerBytes = Combine(headerBuffers.ToArray());
                var headerString = Encoding.UTF8.GetString(headerBytes);
                if (config.GetProxyUrl() != null)
                {
                    var firstcrlf = headerString.IndexOf("\r\n\r\n");
                    var secondcrlf = headerString.IndexOf("\r\n\r\n", firstcrlf + 1);
                    if (secondcrlf > 0)
                    {
                        headerString = headerString.Substring(firstcrlf + 4, secondcrlf - (firstcrlf));
                    }
                }
                var headerParts = headerString.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                var headers = new List<string[]>();
                var headerCount = 0;
                HttpStatusCode status = HttpStatusCode.NotImplemented;
                var cookieBuilder = new StringBuilder();
                var cookies = new List<Tuple<string, string>>();
                foreach (var headerPart in headerParts)
                {
                    if (headerCount == 0)
                    {
                        var split = headerPart.Split(' ');
                        if (split.Length < 2)
                            throw new Exception("HTTP Header missing");
                        var responseCode = int.Parse(headerPart.Split(' ')[1]);
                        status = (HttpStatusCode)responseCode;
                    }
                    else
                    {
                        var keyVal = headerPart.Split(new char[] { ':' }, 2);
                        if (keyVal.Length > 1)
                        {
                            var key = keyVal[0].ToLower().Trim();
                            var value = keyVal[1].Trim();

                            if (key == "set-cookie")
                            {
                                var nameSplit = value.IndexOf('=');
                                if (nameSplit > -1)
                                {
                                    var cKey = value.Substring(0, nameSplit);
                                    var cVal = value.Split(';')[0] + ";";
                                    cookies.Add(new Tuple<string, string>(cKey, cVal));
                                }
                            }
                            else
                            {
                                headers.Add(new[] { key, value });
                            }
                        }
                    }

                    headerCount++;
                }

                foreach (var cookieGroup in cookies.GroupBy(c => c.Item1))
                {
                    cookieBuilder.AppendFormat("{0} ", cookieGroup.Last().Item2);
                }

                // add some debug output to track down the problem causing people getting InternalServerError results
                if (status == HttpStatusCode.NotImplemented || status == HttpStatusCode.InternalServerError)
                {
                    try
                    {
                        OnErrorMessage("got NotImplemented/InternalServerError");
                        OnErrorMessage("request.Method: " + curlRequest.Method);
                        OnErrorMessage("request.Url: " + curlRequest.Url);
                        OnErrorMessage("request.Cookies: " + curlRequest.Cookies);
                        OnErrorMessage("request.Referer: " + curlRequest.Referer);
                        OnErrorMessage("request.RawPOSTDdata: " + curlRequest.RawPOSTDdata);
                        OnErrorMessage("cookies: " + cookieBuilder.ToString().Trim());
                        OnErrorMessage("headerString:\n" + headerString);

                        foreach (var headerPart in headerParts)
                        {
                            OnErrorMessage("headerParts: " + headerPart);
                        }
                    }
                    catch (Exception ex)
                    {
                        OnErrorMessage(string.Format("CurlHelper: error while handling NotImplemented/InternalServerError:\n{0}", ex));
                    }
                }

                var contentBytes = Combine(contentBuffers.ToArray());
                var curlResponse = new CurlResponse(headers, contentBytes, status, cookieBuilder.ToString().Trim());
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
