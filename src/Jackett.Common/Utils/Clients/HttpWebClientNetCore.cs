using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CloudflareSolverRe;
using Jackett.Common.Helpers;
using Jackett.Common.Models.Config;
using Jackett.Common.Services.Interfaces;
using NLog;

namespace Jackett.Common.Utils.Clients
{
    // custom HttpWebClient based WebClient for netcore (due to changed custom certificate validation API)
    public class HttpWebClientNetCore : WebClient
    {
        public HttpWebClientNetCore(IProcessService p, Logger l, IConfigurationService c, ServerConfig sc)
            : base(p: p,
                   l: l,
                   c: c,
                   sc: sc)
        {
        }
        public override void Init()
        {
            ServicePointManager.DefaultConnectionLimit = 1000;

            base.Init();
        }

        protected override async Task<WebClientByteResult> Run(WebRequest webRequest)
        {
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)192 | (SecurityProtocolType)768 | (SecurityProtocolType)3072;

            var cookies = new CookieContainer();
            if (!string.IsNullOrEmpty(webRequest.Cookies))
            {
                var uri = new Uri(webRequest.Url);
                var cookieUrl = new Uri(uri.Scheme + "://" + uri.Host); // don't include the path, Scheme is needed for mono compatibility
                foreach (var c in webRequest.Cookies.Split(';'))
                {
                    try
                    {
                        cookies.SetCookies(cookieUrl, c.Trim());
                    }
                    catch (CookieException ex)
                    {
                        logger.Info("(Non-critical) Problem loading cookie {0}, {1}, {2}", uri, c, ex.Message);
                    }
                }
            }

            var userAgent = webRequest.EmulateBrowser.Value ? BrowserUtil.ChromeUserAgent : "Jackett/" + configService.GetVersion();

            using (var clearanceHandlr = new ClearanceHandler(userAgent))
            {
                clearanceHandlr.MaxTries = 30;
                using (var clientHandlr = new HttpClientHandler
                {
                    CookieContainer = cookies,
                    AllowAutoRedirect = false, // Do not use this - Bugs ahoy! Lost cookies and more.
                    UseCookies = true,
                    Proxy = webProxy,
                    UseProxy = (webProxy != null),
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                })
                {
                    // custom certificate validation handler (netcore version)
                    clientHandlr.ServerCertificateCustomValidationCallback = ValidateCertificate;

                    clearanceHandlr.InnerHandler = clientHandlr;
                    using (var client = new HttpClient(clearanceHandlr))
                    {
                        //if (webRequest.EmulateBrowser == true)
                        //    client.DefaultRequestHeaders.Add("User-Agent", BrowserUtil.ChromeUserAgent);
                        //else
                        //    client.DefaultRequestHeaders.Add("User-Agent", "Jackett/" + configService.GetVersion());

                        HttpResponseMessage response = null;
                        using (var request = new HttpRequestMessage())
                        {
                            request.Headers.ExpectContinue = false;
                            request.RequestUri = new Uri(webRequest.Url);

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

                            using (response = await client.SendAsync(request))
                            {
                                var result = new WebClientByteResult
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
                                Encoding encoding = null;
                                if (webRequest.Encoding != null)
                                {
                                    encoding = webRequest.Encoding;
                                }
                                else if (result.Headers.ContainsKey("content-type"))
                                {
                                    var CharsetRegex = new Regex(@"charset=([\w-]+)", RegexOptions.Compiled);
                                    var CharsetRegexMatch = CharsetRegex.Match(result.Headers["content-type"][0]);
                                    if (CharsetRegexMatch.Success)
                                    {
                                        var charset = CharsetRegexMatch.Groups[1].Value;
                                        try
                                        {
                                            encoding = Encoding.GetEncoding(charset);
                                        }
                                        catch (Exception ex)
                                        {
                                            logger.Error(string.Format("WebClient({0}).GetString(Url:{1}): Error loading encoding {2} based on header {3}: {4}", ClientType, webRequest.Url, charset, result.Headers["content-type"][0], ex));
                                        }
                                    }
                                    else
                                    {
                                        logger.Error(string.Format("WebClient({0}).GetString(Url:{1}): Got header without charset: {2}", ClientType, webRequest.Url, result.Headers["content-type"][0]));
                                    }
                                }

                                if (encoding == null)
                                {
                                    logger.Error(string.Format("WebClient({0}).GetString(Url:{1}): No encoding detected, defaulting to UTF-8", ClientType, webRequest.Url));
                                    encoding = Encoding.UTF8;
                                }

                                result.Encoding = encoding;
                                return result;
                            }
                        }
                    }
                }
            }
        }
    }
}
