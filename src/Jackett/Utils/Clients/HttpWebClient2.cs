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
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Jackett.Utils.Clients
{
    // Compared to HttpWebClient this implementation will reuse the HttpClient instance (one per indexer).
    // This should improve performance and avoid problems with too man open file handles.
    public class HttpWebClient2 : IWebClient
    {
        CookieContainer cookies;
        ClearanceHandler clearanceHandlr;
        HttpClientHandler clientHandlr;
        HttpClient client;

        static protected Dictionary<string, ICollection<string>> trustedCertificates = new Dictionary<string, ICollection<string>>();

        public HttpWebClient2(IProcessService p, Logger l, IConfigurationService c)
            : base(p: p,
                   l: l,
                   c: c)
        {
            cookies = new CookieContainer();
            var useProxy = false;
            WebProxy proxyServer = null;
            if (Startup.ProxyConnection != null)
            {
                proxyServer = new WebProxy(Startup.ProxyConnection, false);
                useProxy = true;
            }

            clearanceHandlr = new ClearanceHandler();
            clientHandlr = new HttpClientHandler
            {
                CookieContainer = cookies,
                AllowAutoRedirect = false, // Do not use this - Bugs ahoy! Lost cookies and more.
                UseCookies = true,
                Proxy = proxyServer,
                UseProxy = useProxy,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            clearanceHandlr.InnerHandler = clientHandlr;
            client = new HttpClient(clearanceHandlr);
        }

        override public void Init()
        {
            if (Startup.IgnoreSslErrors == true)
            {
                logger.Info(string.Format("HttpWebClient2: Disabling certificate validation"));
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => { return true; };
            }

            ServicePointManager.SecurityProtocol = (SecurityProtocolType)192 | (SecurityProtocolType)768 | (SecurityProtocolType)3072;

            // custom handler for our own internal certificates
            ServicePointManager.ServerCertificateValidationCallback += delegate (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            {
                if (sender.GetType() != typeof(HttpWebRequest))
                    return sslPolicyErrors == SslPolicyErrors.None;

                var request = (HttpWebRequest)sender;
                var hash = certificate.GetCertHashString();

                ICollection<string> hosts;

                trustedCertificates.TryGetValue(hash, out hosts);
                if (hosts != null)
                {
                    if (hosts.Contains(request.Host))
                        return true;
                }
                return sslPolicyErrors == SslPolicyErrors.None;
            };
        }

        override protected async Task<WebClientByteResult> Run(WebRequest webRequest)
        {
            HttpResponseMessage response = null;
            var request = new HttpRequestMessage();
            request.Headers.ExpectContinue = false;
            request.RequestUri = new Uri(webRequest.Url);

            if (webRequest.EmulateBrowser == true)
                request.Headers.UserAgent.ParseAdd(BrowserUtil.ChromeUserAgent);
            else
                request.Headers.UserAgent.ParseAdd("Jackett/" + configService.GetVersion());

            // clear cookies from cookiecontainer
            var oldCookies = cookies.GetCookies(request.RequestUri);
            foreach (Cookie oldCookie in oldCookies)
            {
                oldCookie.Expired = true;
            }

            if (!string.IsNullOrEmpty(webRequest.Cookies))
            {
                // add cookies to cookiecontainer
                var cookieUrl = new Uri(request.RequestUri.Scheme + "://" + request.RequestUri.Host); // don't include the path, Scheme is needed for mono compatibility
                foreach (var ccookiestr in webRequest.Cookies.Split(';'))
                {
                    var cookiestrparts = ccookiestr.Split('=');
                    var name = cookiestrparts[0].Trim();
                    if (string.IsNullOrWhiteSpace(name))
                        continue;
                    var value = "";
                    if (cookiestrparts.Length >= 2)
                        value = cookiestrparts[1].Trim();
                    var cookie = new Cookie(name, value);
                    cookies.Add(cookieUrl, cookie);
                }
            }

            if (webRequest.Headers != null)
            {
                foreach (var header in webRequest.Headers)
                {
                    if (header.Key != "Content-Type")
                    {
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }
            }

            if (!string.IsNullOrEmpty(webRequest.Referer))
                request.Headers.Referrer = new Uri(webRequest.Referer);

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
                if (webRequest.PostData != null)
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
                    logger.Debug(value);
                    var nameSplit = value.IndexOf('=');
                    if (nameSplit > -1)
                    {
                        responseCookies.Add(new Tuple<string, string>(value.Substring(0, nameSplit), value.Substring(0, value.IndexOf(';') == -1 ? value.Length : (value.IndexOf(';')))+";"));
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
