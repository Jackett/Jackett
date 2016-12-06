using AutoMapper;
using CloudFlareUtilities;
using Jackett.Models;
using Jackett.Services;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Utils.Clients
{
    public class HttpWebClient : IWebClient
    {
        Logger logger;
        IConfigurationService configService;

        public HttpWebClient(Logger l, IConfigurationService c)
        {
            logger = l;
            configService = c;
        }


        public void Init()
        {
            if (Startup.IgnoreSslErrors == true)
            {
                logger.Info(string.Format("WindowsWebClient: Disabling certificate validation"));
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => { return true; };
            }
        }

        public async Task<WebClientByteResult> GetBytes(WebRequest request)
        {
            logger.Debug(string.Format("WindowsWebClient:GetBytes(Url:{0})", request.Url));
            var result = await Run(request);
            logger.Debug(string.Format("WindowsWebClient: Returning {0} => {1} bytes", result.Status, (result.Content == null ? "<NULL>" : result.Content.Length.ToString())));
            return result;
        }

        public async Task<WebClientStringResult> GetString(WebRequest request)
        {
            logger.Debug(string.Format("WindowsWebClient:GetString(Url:{0})", request.Url));
            var result = await Run(request);
            logger.Debug(string.Format("WindowsWebClient: Returning {0} => {1}", result.Status, (result.Content == null ? "<NULL>" : Encoding.UTF8.GetString(result.Content))));
            WebClientStringResult stringResult = Mapper.Map<WebClientStringResult>(result);
            string[] server;
            if (stringResult.Headers.TryGetValue("server", out server))
            {
                if (server[0] == "cloudflare-nginx")
                    stringResult.Content = BrowserUtil.DecodeCloudFlareProtectedEmailFromHTML(stringResult.Content);
            }
            return stringResult;
        }

        private async Task<WebClientByteResult> Run(WebRequest webRequest)
        {
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)192 | (SecurityProtocolType)768 | (SecurityProtocolType)3072;

            var cookies = new CookieContainer();
            if (!string.IsNullOrEmpty(webRequest.Cookies))
            {
                var uri = new Uri(webRequest.Url);
                foreach (var c in webRequest.Cookies.Split(';'))
                {
                    try
                    {
                        cookies.SetCookies(uri, c);
                    }
                    catch (CookieException ex)
                    {
                        logger.Info("(Non-critical) Problem loading cookie {0}, {1}, {2}", uri, c, ex.Message);
                    }
                }
            }
            var useProxy = false;
            WebProxy proxyServer = null;
            if (Startup.ProxyConnection != null)
            {
                proxyServer = new WebProxy(Startup.ProxyConnection, false);
                useProxy = true;
            }

            ClearanceHandler clearanceHandlr = new ClearanceHandler();
            HttpClientHandler clientHandlr = new HttpClientHandler
            {
                CookieContainer = cookies,
                AllowAutoRedirect = false, // Do not use this - Bugs ahoy! Lost cookies and more.
                UseCookies = true,
                Proxy = proxyServer,
                UseProxy = useProxy
            };

            clearanceHandlr.InnerHandler = clientHandlr;
            var client = new HttpClient(clearanceHandlr);

            if (webRequest.EmulateBrowser)
                client.DefaultRequestHeaders.Add("User-Agent",  BrowserUtil.ChromeUserAgent);
            else
                client.DefaultRequestHeaders.Add("User-Agent", "Jackett/" + configService.GetVersion());
            
            HttpResponseMessage response = null;
            var request = new HttpRequestMessage();
            request.Headers.ExpectContinue = false;
            request.RequestUri = new Uri(webRequest.Url);

            if (webRequest.Headers != null)
            {
                foreach (var header in webRequest.Headers)
                {
                    if (header.Key != "Content-Type")
                    {
                        request.Headers.Add(header.Key, header.Value);
                    }
                }
            }

            if (!string.IsNullOrEmpty(webRequest.RawBody))
            {
                var type = webRequest.Headers.Where(h => h.Key == "Content-Type").Cast<KeyValuePair<string,string>?>().FirstOrDefault();
                if (type.HasValue)
                {
                    var str = new StringContent(webRequest.RawBody);
                    str.Headers.Remove("Content-Type");
                    str.Headers.Add("Content-Type", type.Value.Value);
                    request.Content = str;
                }
                else
                    request.Content = new StringContent(webRequest.RawBody);
                request.Method = HttpMethod.Post;
            }
            else if (webRequest.Type == RequestType.POST)
            {
                request.Content = new FormUrlEncodedContent(webRequest.PostData);
                request.Method = HttpMethod.Post;
            }
            else
            {
                request.Method = HttpMethod.Get;
            }

            response = await client.SendAsync(request);

            var result = new WebClientByteResult();
            result.Content = await response.Content.ReadAsByteArrayAsync();

            foreach (var header in response.Headers)
            {
                IEnumerable<string> value = header.Value;
                result.Headers[header.Key.ToLowerInvariant()] = value.ToArray();
            }

            // some cloudflare clients are using a refresh header
            // Pull it out manually 
            if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable && response.Headers.Contains("Refresh"))
            {
                var refreshHeaders = response.Headers.GetValues("Refresh");
                var redirval = "";
                var redirtime = 0;
                if (refreshHeaders != null)
                {
                    foreach (var value in refreshHeaders)
                    {
                        var start = value.IndexOf("=");
                        var end = value.IndexOf(";");
                        var len = value.Length;
                        if (start > -1)
                        {
                            redirval = value.Substring(start + 1);
                            result.RedirectingTo = redirval;
                            // normally we don't want a serviceunavailable (503) to be a redirect, but that's the nature
                            // of this cloudflare approach..don't want to alter BaseWebResult.IsRedirect because normally
                            // it shoudln't include service unavailable..only if we have this redirect header.
                            response.StatusCode = System.Net.HttpStatusCode.Redirect;
                            redirtime = Int32.Parse(value.Substring(0, end));
                            System.Threading.Thread.Sleep(redirtime * 1000);
                        }
                    }
                }
            }
            if (response.Headers.Location != null)
            {
                result.RedirectingTo = response.Headers.Location.ToString();
            }
            result.Status = response.StatusCode;

            // Compatiblity issue between the cookie format and httpclient
            // Pull it out manually ignoring the expiry date then set it manually
            // http://stackoverflow.com/questions/14681144/httpclient-not-storing-cookies-in-cookiecontainer
            IEnumerable<string> cookieHeaders;
            var responseCookies = new List<Tuple<string, string>>();

            if (response.Headers.TryGetValues("set-cookie", out cookieHeaders))
            {
                foreach (var value in cookieHeaders)
                {
                    var nameSplit = value.IndexOf('=');
                    if (nameSplit > -1)
                    {
                        responseCookies.Add(new Tuple<string, string>(value.Substring(0, nameSplit), value.Substring(0, value.IndexOf(';') == -1 ? value.Length : (value.IndexOf(';') + 1))));
                    }
                }

                var cookieBuilder = new StringBuilder();
                foreach (var cookieGroup in responseCookies.GroupBy(c => c.Item1))
                {
                    cookieBuilder.AppendFormat("{0} ", cookieGroup.Last().Item2);
                }
                result.Cookies = cookieBuilder.ToString().Trim();
            }
            ServerUtil.ResureRedirectIsFullyQualified(webRequest, result);
            return result;
        }
    }
}
