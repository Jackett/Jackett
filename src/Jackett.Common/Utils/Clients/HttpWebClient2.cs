using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using FlareSolverrSharp;
using Jackett.Common.Helpers;
using Jackett.Common.Models.Config;
using Jackett.Common.Services.Interfaces;
using NLog;

namespace Jackett.Common.Utils.Clients
{
    // Compared to HttpWebClient this implementation will reuse the HttpClient instance (one per indexer).
    // This should improve performance and avoid problems with too man open file handles.
    public class HttpWebClient2 : WebClient
    {
        private readonly CookieContainer cookies;
        private ClearanceHandler clearanceHandlr;
        private HttpClientHandler clientHandlr;
        private HttpClient client;

        public HttpWebClient2(IProcessService p, Logger l, IConfigurationService c, ServerConfig sc)
            : base(p: p,
                   l: l,
                   c: c,
                   sc: sc)
        {
            cookies = new CookieContainer();
            CreateClient();
        }

        [DebuggerNonUserCode] // avoid "Exception User-Unhandled" Visual Studio messages
        public static bool ValidateCertificate(HttpRequestMessage request, X509Certificate2 certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            var hash = certificate.GetCertHashString();

            trustedCertificates.TryGetValue(hash, out var hosts);
            if (hosts != null && hosts.Contains(request.RequestUri.Host))
                return true;

            // Throw exception with certificate details, this will cause a "Exception User-Unhandled" when running it in the Visual Studio debugger.
            // The certificate is only available inside this function, so we can't catch it at the calling method.
            if (sslPolicyErrors != SslPolicyErrors.None)
                throw new Exception("certificate validation failed: " + certificate);

            return sslPolicyErrors == SslPolicyErrors.None;
        }

        public void CreateClient()
        {
            clearanceHandlr = new ClearanceHandler(serverConfig.FlareSolverrUrl)
            {
                MaxTimeout = 55000,
                ProxyUrl = serverConfig.GetProxyUrl(false)
            };
            clientHandlr = new HttpClientHandler
            {
                CookieContainer = cookies,
                AllowAutoRedirect = false, // Do not use this - Bugs ahoy! Lost cookies and more.
                UseCookies = true,
                Proxy = webProxy,
                UseProxy = (webProxy != null),
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            // custom certificate validation handler (netcore version)
            if (serverConfig.RuntimeSettings.IgnoreSslErrors == true)
                clientHandlr.ServerCertificateCustomValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            else
                clientHandlr.ServerCertificateCustomValidationCallback = ValidateCertificate;

            clearanceHandlr.InnerHandler = clientHandlr;
            client = new HttpClient(clearanceHandlr);
        }

        // Called everytime the ServerConfig changes
        public override void OnNext(ServerConfig value)
        {
            base.OnNext(value);
            // recreate client if needed (can't just change the proxy attribute)
            if (!ReferenceEquals(clientHandlr.Proxy, webProxy))
            {
                CreateClient();
            }
        }

        public override void Init()
        {
            base.Init();

            ServicePointManager.SecurityProtocol = (SecurityProtocolType)192 | (SecurityProtocolType)768 | (SecurityProtocolType)3072;
        }

        protected override async Task<WebResult> Run(WebRequest webRequest)
        {
            HttpResponseMessage response = null;
            var request = new HttpRequestMessage();
            request.Headers.ExpectContinue = false;
            request.RequestUri = new Uri(webRequest.Url);

            // clear cookies from cookiecontainer
            var oldCookies = cookies.GetCookies(request.RequestUri);
            foreach (Cookie oldCookie in oldCookies)
                oldCookie.Expired = true;

            // add cookies to cookiecontainer
            if (!string.IsNullOrWhiteSpace(webRequest.Cookies))
            {
                // don't include the path, Scheme is needed for mono compatibility
                var cookieUrl = new Uri(request.RequestUri.Scheme + "://" + request.RequestUri.Host);
                var cookieDictionary = CookieUtil.CookieHeaderToDictionary(webRequest.Cookies);
                foreach (var kv in cookieDictionary)
                    cookies.Add(cookieUrl, new Cookie(kv.Key, kv.Value));
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

            // The User-Agent can be set by the indexer (in the headers)
            if (string.IsNullOrWhiteSpace(request.Headers.UserAgent.ToString()))
            {
                if (webRequest.EmulateBrowser == true)
                    request.Headers.UserAgent.ParseAdd(BrowserUtil.ChromeUserAgent);
                else
                    request.Headers.UserAgent.ParseAdd("Jackett/" + configService.GetVersion());
            }

            if (!string.IsNullOrEmpty(webRequest.Referer))
                request.Headers.Referrer = new Uri(webRequest.Referer);

            if (!string.IsNullOrEmpty(webRequest.RawBody))
            {
                var type = webRequest.Headers.Where(h => h.Key == "Content-Type").Cast<KeyValuePair<string, string>?>().FirstOrDefault();
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
                    request.Content = FormUrlEncodedContentWithEncoding(webRequest.PostData, webRequest.Encoding);
                request.Method = HttpMethod.Post;
            }
            else
            {
                request.Method = HttpMethod.Get;
            }

            response = await client.SendAsync(request);

            var result = new WebResult
            {
                ContentBytes = await response.Content.ReadAsByteArrayAsync()
            };

            foreach (var header in response.Headers)
            {
                var value = header.Value;
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
                            // of this cloudflare approach..don't want to alter WebResult.IsRedirect because normally
                            // it shoudln't include service unavailable..only if we have this redirect header.
                            response.StatusCode = System.Net.HttpStatusCode.Redirect;
                            redirtime = int.Parse(value.Substring(0, end));
                            System.Threading.Thread.Sleep(redirtime * 1000);
                        }
                    }
                }
            }
            if (response.Headers.Location != null)
            {
                result.RedirectingTo = response.Headers.Location.ToString();
            }
            // Mono won't add the baseurl to relative redirects.
            // e.g. a "Location: /index.php" header will result in the Uri "file:///index.php"
            // See issue #1200
            if (result.RedirectingTo != null && result.RedirectingTo.StartsWith("file://"))
            {
                // URL decoding apparently is needed to, without it e.g. Demonoid download is broken
                // TODO: is it always needed (not just for relative redirects)?
                var newRedirectingTo = WebUtilityHelpers.UrlDecode(result.RedirectingTo, webRequest.Encoding);
                if (newRedirectingTo.StartsWith("file:////")) // Location without protocol but with host (only add scheme)
                    newRedirectingTo = newRedirectingTo.Replace("file://", request.RequestUri.Scheme + ":");
                else
                    newRedirectingTo = newRedirectingTo.Replace("file://", request.RequestUri.Scheme + "://" + request.RequestUri.Host);
                logger.Debug("[MONO relative redirect bug] Rewriting relative redirect URL from " + result.RedirectingTo + " to " + newRedirectingTo);
                result.RedirectingTo = newRedirectingTo;
            }
            result.Status = response.StatusCode;

            // Compatiblity issue between the cookie format and httpclient
            // Pull it out manually ignoring the expiry date then set it manually
            // http://stackoverflow.com/questions/14681144/httpclient-not-storing-cookies-in-cookiecontainer
            var responseCookies = new List<Tuple<string, string>>();

            if (response.Headers.TryGetValues("set-cookie", out var cookieHeaders))
            {
                foreach (var value in cookieHeaders)
                {
                    logger.Debug(value);
                    var nameSplit = value.IndexOf('=');
                    if (nameSplit > -1)
                    {
                        responseCookies.Add(new Tuple<string, string>(value.Substring(0, nameSplit), value.Substring(0, value.IndexOf(';') == -1 ? value.Length : (value.IndexOf(';'))) + ";"));
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
