using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
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
        protected int ClientTimeout = 100; // default timeout is 100 s
        public bool EmulateBrowser = true;

        protected static Dictionary<string, ICollection<string>> trustedCertificates = new Dictionary<string, ICollection<string>>();
        protected static string webProxyUrl;
        protected static IWebProxy webProxy;

        public void InitProxy(ServerConfig serverConfig)
        {
            // dispose old SocksWebProxy
            if (webProxy is SocksWebProxy proxy)
                proxy.Dispose();
            webProxy = null;

            webProxyUrl = serverConfig.GetProxyUrl();
            if (serverConfig.ProxyType == ProxyType.Disabled || string.IsNullOrWhiteSpace(webProxyUrl))
                return;
            if (serverConfig.ProxyType == ProxyType.Http)
            {
                NetworkCredential creds = null;
                if (!serverConfig.ProxyIsAnonymous)
                {
                    var username = serverConfig.ProxyUsername;
                    var password = serverConfig.ProxyPassword;
                    creds = new NetworkCredential(username, password);
                }
                webProxy = new WebProxy(serverConfig.GetProxyUrl(false)) // proxy URL without credentials
                {
                    BypassProxyOnLocal = false,
                    Credentials = creds
                };
            }
            else if (serverConfig.ProxyType == ProxyType.Socks4 || serverConfig.ProxyType == ProxyType.Socks5)
            {
                // in case of error in DNS resolution, we use a fake proxy to avoid leaking the user IP (disabling proxy)
                // https://github.com/Jackett/Jackett/issues/8826
                var addresses = new[] { new IPAddress(2130706433) }; // 127.0.0.1
                try
                {
                    addresses = Dns.GetHostAddressesAsync(serverConfig.ProxyUrl).Result;
                }
                catch (Exception e)
                {
                    logger.Error($"Unable to resolve proxy URL: {serverConfig.ProxyUrl}. The proxy will not work properly.\n{e}");
                }
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
                    socksConfig.SocksPort = serverConfig.ProxyPort.Value;
                webProxy = new SocksWebProxy(socksConfig, false);
            }
            else
                throw new Exception($"Proxy type '{serverConfig.ProxyType}' is not implemented!");
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

        public virtual async Task<WebResult> GetResultAsync(WebRequest request)
        {
            if (logger.IsDebugEnabled) // performance optimization
            {
                var postData = "";
                if (request.Type == RequestType.POST)
                {
                    var lines = request.PostData?.Select(kvp => kvp.Key + "=" + kvp.Value);
                    lines ??= new List<string>();
                    postData = $" PostData: {{{string.Join(", ", lines)}}} RawBody: {request.RawBody}";
                }
                logger.Debug($"WebClient({ClientType}).GetResultAsync(Method: {request.Type} Url: {request.Url}{postData})");
            }

            PrepareRequest(request);
            await DelayRequest(request);
            var result = await Run(request);
            lastRequest = DateTime.Now;
            result.Request = request;

            if (logger.IsDebugEnabled) // performance optimization to compute result.ContentString in debug mode only
            {
                var body = "";
                var bodySize = 0;
                if (result.ContentBytes != null && result.ContentBytes.Length > 0)
                {
                    bodySize = result.ContentBytes.Length;
                    var contentString = result.ContentString.Trim();
                    if (contentString.StartsWith("<") || contentString.StartsWith("{") || contentString.StartsWith("["))
                        body = "\n" + contentString;
                    else
                        body = " <BINARY>";
                }
                logger.Debug($@"WebClient({ClientType}): Returning {result.Status} => {
                                     (result.IsRedirect ? result.RedirectingTo + " " : "")
                                 }{bodySize} bytes{body}");
            }

            return result;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        protected virtual async Task<WebResult> Run(WebRequest webRequest) => throw new NotImplementedException();
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

        public virtual void Init() => ServicePointManager.DefaultConnectionLimit = 1000;

        public virtual void OnCompleted() => throw new NotImplementedException();

        public virtual void OnError(Exception error) => throw new NotImplementedException();

        public virtual void OnNext(ServerConfig value)
        {
            var newProxyUrl = serverConfig.GetProxyUrl();
            if (webProxyUrl != newProxyUrl) // if proxy URL changed
                InitProxy(serverConfig);
        }

        public virtual void SetTimeout(int seconds) => throw new NotImplementedException();

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
