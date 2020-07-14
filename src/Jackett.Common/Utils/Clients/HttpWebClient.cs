using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CloudflareSolverRe;
using Jackett.Common.Helpers;
using Jackett.Common.Models.Config;
using Jackett.Common.Services.Interfaces;
using NLog;

namespace Jackett.Common.Utils.Clients
{
    public class HttpWebClient : WebClient
    {
        public HttpWebClient(IProcessService p, Logger l, IConfigurationService c, ServerConfig sc)
            : base(p, l, c, sc)
        {
        }

        protected override async Task<WebResult> Run(WebRequest webRequest)
        {
            var cookies = new CookieContainer();
            UpdateCookies(new Uri(webRequest.Url), webRequest, cookies);
            var userAgent = webRequest.EmulateBrowser == true
                ? BrowserUtil.ChromeUserAgent
                : "Jackett/" + configService.GetVersion();
            using var clearanceHandler = new ClearanceHandler(userAgent)
            {
                MaxTries = 10,
                InnerHandler = new HttpClientHandler
                {
                    CookieContainer = cookies,
                    AllowAutoRedirect = false, // Do not use this - Bugs ahoy! Lost cookies and more.
                    UseCookies = true,
                    Proxy = webProxy,
                    UseProxy = webProxy != null,
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                    // custom certificate validation handler
                    ServerCertificateCustomValidationCallback = ValidateCertificate
                }
            };
            using var client = new HttpClient(clearanceHandler);
            client.DefaultRequestHeaders.Add(
                "User-Agent",
                webRequest.EmulateBrowser == true
                    ? BrowserUtil.ChromeUserAgent
                    : "Jackett/" + configService.GetVersion());
            HttpResponseMessage response;
            using var request = new HttpRequestMessage
            {
                RequestUri = new Uri(webRequest.Url)
            };
            request.Headers.ExpectContinue = false;
            if (webRequest.Headers != null)
                foreach (var header in webRequest.Headers.Where(header => header.Key != "Content-Type"))
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            if (!string.IsNullOrEmpty(webRequest.Referer))
                request.Headers.Referrer = new Uri(webRequest.Referer);
            if (!string.IsNullOrEmpty(webRequest.RawBody))
            {
                var type = webRequest.Headers?
                                     .Where(h => h.Key == "Content-Type")
                                     .Cast<KeyValuePair<string, string>?>()
                                     .FirstOrDefault();
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
                request.Method = HttpMethod.Get;

            using (response = await client.SendAsync(request))
            {
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
                if (response.StatusCode == HttpStatusCode.ServiceUnavailable && response.Headers.Contains("Refresh"))
                {
                    var refreshHeaders = response.Headers.GetValues("Refresh");
                    if (refreshHeaders != null)
                        foreach (var value in refreshHeaders)
                        {
                            var start = value.IndexOf("=", StringComparison.Ordinal);
                            var end = value.IndexOf(";", StringComparison.Ordinal);
                            if (start > -1)
                            {
                                var redirectVal = value.Substring(start + 1);
                                result.RedirectingTo = redirectVal;
                                // normally we don't want a service unavailable (503) to be a redirect, but that's the nature
                                // of this cloudflare approach..don't want to alter WebResult.IsRedirect because normally
                                // it shouldn't include service unavailable..only if we have this redirect header.
                                response.StatusCode = HttpStatusCode.Redirect;
                                var redirectTime = int.Parse(value.Substring(0, end));
                                Thread.Sleep(redirectTime * 1000);
                            }
                        }
                }

                if (response.Headers.Location != null)
                    result.RedirectingTo = response.Headers.Location.ToString();
                // Mono won't add the base url to relative redirects.
                // e.g. a "Location: /index.php" header will result in the Uri "file:///index.php"
                // See issue #1200
                if (result.RedirectingTo?.StartsWith("file://") == true)
                {
                    // URL decoding apparently is needed to, without it e.g. Demonoid download is broken
                    // TODO: is it always needed (not just for relative redirects)?
                    var newRedirectingTo = WebUtilityHelpers.UrlDecode(result.RedirectingTo, webRequest.Encoding);
                    newRedirectingTo = newRedirectingTo.StartsWith("file:////")
                        ? newRedirectingTo.Replace("file://", request.RequestUri.Scheme + ":")
                        : newRedirectingTo.Replace("file://", request.RequestUri.Scheme + "://" + request.RequestUri.Host);
                    logger.Debug(
                        "[MONO relative redirect bug] Rewriting relative redirect URL from " + result.RedirectingTo +
                        " to " + newRedirectingTo);
                    result.RedirectingTo = newRedirectingTo;
                }

                result.Status = response.StatusCode;

                // Compatibility issue between the cookie format and http-client
                // Pull it out manually ignoring the expiry date then set it manually
                // http://stackoverflow.com/questions/14681144/httpclient-not-storing-cookies-in-cookiecontainer
                var responseCookies = new List<Tuple<string, string>>();
                if (response.Headers.TryGetValues("set-cookie", out var cookieHeaders))
                {
                    responseCookies.AddRange(
                        from value in cookieHeaders
                        let nameSplit = value.IndexOf('=')
                        where nameSplit > -1
                        select new Tuple<string, string>(
                            value.Substring(0, nameSplit),
                            value.Substring(0, value.IndexOf(';') == -1 ? value.Length : value.IndexOf(';')) + ";"));
                    var cookieBuilder = new StringBuilder();
                    foreach (var cookieGroup in responseCookies.GroupBy(c => c.Item1))
                        cookieBuilder.AppendFormat("{0} ", cookieGroup.Last().Item2);
                    result.Cookies = cookieBuilder.ToString().Trim();
                }

                ServerUtil.ResureRedirectIsFullyQualified(webRequest, result);
                return result;
            }
        }
    }
}
