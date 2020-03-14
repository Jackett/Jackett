using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using AutoMapper;
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
            // not implemented by default
        }

        public WebClient(IProcessService p, Logger l, IConfigurationService c, ServerConfig sc)
        {
            processService = p;
            logger = l;
            configService = c;
            serverConfig = sc;
            ClientType = GetType().Name;
            ServerConfigUnsubscriber = serverConfig.Subscribe(this);
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

        public virtual async Task<WebClientByteResult> GetBytes(WebRequest request)
        {
            logger.Debug(string.Format("WebClient({0}).GetBytes(Url:{1})", ClientType, request.Url));
            PrepareRequest(request);
            await DelayRequest(request);
            var result = await Run(request);
            lastRequest = DateTime.Now;
            result.Request = request;
            logger.Debug(string.Format("WebClient({0}): Returning {1} => {2} bytes", ClientType, result.Status, (result.IsRedirect ? result.RedirectingTo + " " : "") + (result.Content == null ? "<NULL>" : result.Content.Length.ToString())));
            return result;
        }

        public virtual async Task<WebClientStringResult> GetString(WebRequest request)
        {
            logger.Debug(string.Format("WebClient({0}).GetString(Url:{1})", ClientType, request.Url));
            PrepareRequest(request);
            await DelayRequest(request);
            var result = await Run(request);
            lastRequest = DateTime.Now;
            result.Request = request;
            var stringResult = Mapper.Map<WebClientStringResult>(result);

            string decodedContent = null;
            if (result.Content != null)
                decodedContent = result.Encoding.GetString(result.Content);

            stringResult.Content = decodedContent;
            logger.Debug(string.Format("WebClient({0}): Returning {1} => {2}", ClientType, result.Status, (result.IsRedirect ? result.RedirectingTo + " " : "") + (decodedContent == null ? "<NULL>" : decodedContent)));

            if (stringResult.Headers.TryGetValue("server", out var server))
            {
                if (server[0] == "cloudflare-nginx")
                    stringResult.Content = BrowserUtil.DecodeCloudFlareProtectedEmailFromHTML(stringResult.Content);
            }
            return stringResult;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        protected virtual async Task<WebClientByteResult> Run(WebRequest webRequest) => throw new NotImplementedException();
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

        public abstract void Init();

        public virtual void OnCompleted() => throw new NotImplementedException();

        public virtual void OnError(Exception error) => throw new NotImplementedException();

        public virtual void OnNext(ServerConfig value)
        {
            // nothing by default
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
