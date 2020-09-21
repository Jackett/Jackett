using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using AutoMapper;
using com.LandonKey.SocksWebProxy;
using com.LandonKey.SocksWebProxy.Proxy;
using Jackett.Common.Models.Config;
using Jackett.Common.Services.Interfaces;
using NLog;

namespace Jackett.Common.Utils.Clients
{
    public abstract class WebClient : IObserver<ServerConfig>
    {
        protected IDisposable ServerConfigUnsubscriber;
        protected Logger logger;
        protected IConfigurationService configService;
        protected readonly ServerConfig serverConfig;
        protected IProcessService processService;
        protected DateTime lastRequest = DateTime.MinValue;
        protected TimeSpan requestDelayTimeSpan;
        protected string ClientType;
        public bool EmulateBrowser = true;

        protected static Dictionary<string, ICollection<string>> trustedCertificates = new Dictionary<string, ICollection<string>>();
        protected static string webProxyUrl;
        protected static IWebProxy webProxy;


        [DebuggerNonUserCode] // avoid "Exception User-Unhandled" Visual Studio messages
        public static bool ValidateCertificate(HttpRequestMessage request, X509Certificate2 certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            {
                var hash = certificate.GetCertHashString();


                trustedCertificates.TryGetValue(hash, out var hosts);
                if (hosts != null)
                {
                    if (hosts.Contains(request.RequestUri.Host))
                        return true;
                }

                if (sslPolicyErrors != SslPolicyErrors.None)
                {
                    // Throw exception with certificate details, this will cause a "Exception User-Unhandled" when running it in the Visual Studio debugger.
                    // The certificate is only available inside this function, so we can't catch it at the calling method.
                    throw new Exception("certificate validation failed: " + certificate.ToString());
                }

                return sslPolicyErrors == SslPolicyErrors.None;
            }
        }
        public static void InitProxy(ServerConfig serverConfig)
        {
            // dispose old SocksWebProxy
            if (webProxy is SocksWebProxy proxy)
                proxy.Dispose();
            webProxy = null;
            webProxyUrl = serverConfig.GetProxyUrl();
            if (!string.IsNullOrWhiteSpace(webProxyUrl))
            {
                if (serverConfig.ProxyType != ProxyType.Http)
                {
                    var addresses = Dns.GetHostAddressesAsync(serverConfig.ProxyUrl).Result;
                    var socksConfig = new ProxyConfig
                    {
                        SocksAddress = addresses.FirstOrDefault(),
                        Username = serverConfig.ProxyUsername,
                        Password = serverConfig.ProxyPassword,
                        Version = serverConfig.ProxyType == ProxyType.Socks4 ?
                            ProxyConfig.SocksVersion.Four :
                            ProxyConfig.SocksVersion.Five
                    };
                    if (serverConfig.ProxyPort.HasValue)
                    {
                        socksConfig.SocksPort = serverConfig.ProxyPort.Value;
                    }
                    webProxy = new SocksWebProxy(socksConfig, false);
                }
                else
                {
                    NetworkCredential creds = null;
                    if (!serverConfig.ProxyIsAnonymous)
                    {
                        var username = serverConfig.ProxyUsername;
                        var password = serverConfig.ProxyPassword;
                        creds = new NetworkCredential(username, password);
                    }
                    webProxy = new WebProxy(webProxyUrl)
                    {
                        BypassProxyOnLocal = false,
                        Credentials = creds
                    };
                }
            }
        }


        public double requestDelay
        {
            get => requestDelayTimeSpan.TotalSeconds;
            set => requestDelayTimeSpan = TimeSpan.FromSeconds(value);
        }

        protected virtual void OnConfigChange()
        {
        }

        public virtual void AddTrustedCertificate(string host, string hash)
        {
            hash = hash.ToUpper();
            trustedCertificates.TryGetValue(hash.ToUpper(), out var hosts);
            if (hosts == null)
            {
                hosts = new HashSet<string>();
                trustedCertificates[hash] = hosts;
            }
            hosts.Add(host);
        }

        public WebClient(IProcessService p, Logger l, IConfigurationService c, ServerConfig sc)
        {
            processService = p;
            logger = l;
            configService = c;
            serverConfig = sc;
            ClientType = GetType().Name;
            ServerConfigUnsubscriber = serverConfig.Subscribe(this);

            if (webProxyUrl == null)
                InitProxy(sc);
        }

        protected async Task DelayRequest(WebRequest request)
        {
            if (request.EmulateBrowser == null)
                request.EmulateBrowser = EmulateBrowser;

            if (requestDelay != 0)
            {
                var timeElapsed = DateTime.Now - lastRequest;
                if (timeElapsed < requestDelayTimeSpan)
                {
                    var delay = requestDelayTimeSpan - timeElapsed;
                    logger.Debug(string.Format("WebClient({0}): delaying request for {1} by {2} seconds", ClientType, request.Url, delay.TotalSeconds.ToString()));
                    await Task.Delay(delay);
                }
            }
        }

        protected virtual void PrepareRequest(WebRequest request)
        {
            // add Accept/Accept-Language header if not set
            // some webservers won't accept requests without accept
            // e.g. elittracker requieres the Accept-Language header
            if (request.Headers == null)
                request.Headers = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            var hasAccept = false;
            var hasAcceptLanguage = false;
            foreach (var header in request.Headers)
            {
                var key = header.Key.ToLower();
                if (key == "accept")
                {
                    hasAccept = true;
                }
                else if (key == "accept-language")
                {
                    hasAcceptLanguage = true;
                }
            }
            if (!hasAccept)
                request.Headers.Add("Accept", "*/*");
            if (!hasAcceptLanguage)
                request.Headers.Add("Accept-Language", "*");
            return;
        }

        public virtual async Task<BaseWebResult> GetBytes(WebRequest request)
        {
            logger.Debug(string.Format("WebClient({0}).GetBytes(Url:{1})", ClientType, request.Url));
            PrepareRequest(request);
            await DelayRequest(request);
            var result = await Run(request);
            lastRequest = DateTime.Now;
            result.Request = request;
            logger.Debug(string.Format("WebClient({0}): Returning {1} => {2} bytes", ClientType, result.Status, (result.IsRedirect ? result.RedirectingTo + " " : "") + (result.ContentBytes == null ? "<NULL>" : result.ContentBytes.Length.ToString())));
            return result;
        }

        public virtual async Task<BaseWebResult> GetString(WebRequest request)
        {
            logger.Debug(string.Format("WebClient({0}).GetString(Url:{1})", ClientType, request.Url));
            PrepareRequest(request);
            await DelayRequest(request);
            var result = await Run(request);
            lastRequest = DateTime.Now;
            result.Request = request;
            BaseWebResult stringResult = result;

            logger.Debug(string.Format("WebClient({0}): Returning {1} => {2}", ClientType, result.Status, (result.IsRedirect ? result.RedirectingTo + " " : "") + (stringResult.ContentString ?? "<NULL>")));

            if (stringResult.Headers.TryGetValue("server", out var server))
            {
                if (server[0] == "cloudflare-nginx")
                    stringResult.ContentString = BrowserUtil.DecodeCloudFlareProtectedEmailFromHTML(stringResult.ContentString);
            }
            return stringResult;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        protected virtual async Task<WebClientByteResult> Run(WebRequest webRequest) => throw new NotImplementedException();
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

        public virtual void Init()
        {
            if (serverConfig.RuntimeSettings.IgnoreSslErrors == true)
            {
                logger.Info($"WebClient({ClientType}): Disabling certificate validation");
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => { return true; };
            }
        }


        public virtual void OnCompleted() => throw new NotImplementedException();

        public virtual void OnError(Exception error) => throw new NotImplementedException();

        public virtual void OnNext(ServerConfig value)
        {
            var newProxyUrl = serverConfig.GetProxyUrl();
            if (webProxyUrl != newProxyUrl) // if proxy URL changed
                InitProxy(serverConfig);
        }

        /**
         * This method does the same as FormUrlEncodedContent but with custom encoding instead of utf-8
         * https://stackoverflow.com/a/13832544
         */
        protected static ByteArrayContent FormUrlEncodedContentWithEncoding(
            IEnumerable<KeyValuePair<string, string>> nameValueCollection, Encoding encoding)
        {
            // utf-8 / default
            if (Encoding.UTF8.Equals(encoding) || encoding == null)
                return new FormUrlEncodedContent(nameValueCollection);

            // other encodings
            var builder = new StringBuilder();
            foreach (var pair in nameValueCollection)
            {
                if (builder.Length > 0)
                    builder.Append('&');
                builder.Append(HttpUtility.UrlEncode(pair.Key, encoding));
                builder.Append('=');
                builder.Append(HttpUtility.UrlEncode(pair.Value, encoding));
            }
            // HttpRuleParser.DefaultHttpEncoding == "latin1"
            var data = Encoding.GetEncoding("latin1").GetBytes(builder.ToString());
            var content = new ByteArrayContent(data);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            return content;
        }
    }
}
