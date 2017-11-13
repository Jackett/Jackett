using AutoMapper;
using Jackett.Models;
using Jackett.Services;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jackett.Services.Interfaces;
using Jackett.Models.Config;

namespace Jackett.Utils.Clients
{
    public abstract class WebClient
    {
        protected Logger logger;
        protected IConfigurationService configService;
        protected readonly ServerConfig serverConfig;
        protected IProcessService processService;
        protected DateTime lastRequest = DateTime.MinValue;
        protected TimeSpan requestDelayTimeSpan;
        public bool EmulateBrowser = true;
        public double requestDelay
        {
            get { return requestDelayTimeSpan.TotalSeconds; }
            set
            {
                requestDelayTimeSpan = TimeSpan.FromSeconds(value);
            }
        }

        virtual public void AddTrustedCertificate(string host, string hash)
        {
            // not implemented by default
        }

        public WebClient(IProcessService p, Logger l, IConfigurationService c, ServerConfig sc)
        {
            processService = p;
            logger = l;
            configService = c;
            serverConfig = sc;
        }

        async protected Task DelayRequest(WebRequest request)
        {
            if (request.EmulateBrowser == null)
                request.EmulateBrowser = EmulateBrowser;

            if (requestDelay != 0)
            {
                var timeElapsed = DateTime.Now - lastRequest;
                if (timeElapsed < requestDelayTimeSpan)
                {
                    var delay = requestDelayTimeSpan - timeElapsed;
                    logger.Debug(string.Format("IWebClient: delaying request for {0} by {1} seconds", request.Url, delay.TotalSeconds.ToString()));
                    await Task.Delay(delay);
                }
                lastRequest = DateTime.Now;
            }
        }

        virtual protected void PrepareRequest(WebRequest request)
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

        virtual public async Task<WebClientByteResult> GetBytes(WebRequest request)
        {
            logger.Debug(string.Format("IWebClient.GetBytes(Url:{0})", request.Url));
            PrepareRequest(request);
            await DelayRequest(request);
            var result = await Run(request);
            result.Request = request;
            logger.Debug(string.Format("IWebClient: Returning {0} => {1} bytes", result.Status, (result.IsRedirect ? result.RedirectingTo + " " : "") + (result.Content == null ? "<NULL>" : result.Content.Length.ToString())));
            return result;
        }

        virtual public async Task<WebClientStringResult> GetString(WebRequest request)
        {
            logger.Debug(string.Format("IWebClient.GetString(Url:{0})", request.Url));
            PrepareRequest(request);
            await DelayRequest(request);
            var result = await Run(request);
            result.Request = request;
            WebClientStringResult stringResult = Mapper.Map<WebClientStringResult>(result);
            Encoding encoding = null;
            if (request.Encoding != null)
            {
                encoding = request.Encoding;
            }
            else if (result.Headers.ContainsKey("content-type"))
            {
                Regex CharsetRegex = new Regex(@"charset=([\w-]+)", RegexOptions.Compiled);
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
                        logger.Error(string.Format("IWebClient.GetString(Url:{0}): Error loading encoding {0} based on header {1}: {2}", request.Url, charset, result.Headers["content-type"][0], ex));
                    }
                }
                else
                {
                    logger.Error(string.Format("IWebClient.GetString(Url:{0}): Got header without charset: {0}", request.Url, result.Headers["content-type"][0]));
                }
            }

            if (encoding == null)
            {
                logger.Error(string.Format("IWebClient.GetString(Url:{0}): No encoding detected, defaulting to UTF-8", request.Url));
                encoding = Encoding.UTF8;
            }

            string decodedContent = null;
            if (result.Content != null)
                decodedContent = encoding.GetString(result.Content);

            stringResult.Content = decodedContent;
            logger.Debug(string.Format("IWebClient: Returning {0} => {1}", result.Status, (result.IsRedirect ? result.RedirectingTo + " " : "") + (decodedContent == null ? "<NULL>" : decodedContent)));

            string[] server;
            if (stringResult.Headers.TryGetValue("server", out server))
            {
                if (server[0] == "cloudflare-nginx")
                    stringResult.Content = BrowserUtil.DecodeCloudFlareProtectedEmailFromHTML(stringResult.Content);
            }
            return stringResult;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        virtual protected async Task<WebClientByteResult> Run(WebRequest webRequest) { throw new NotImplementedException(); }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

        abstract public void Init();
    }
}
