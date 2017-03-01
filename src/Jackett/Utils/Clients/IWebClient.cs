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

namespace Jackett.Utils.Clients
{
    public abstract class IWebClient
    {
        protected Logger logger;
        protected IConfigurationService configService;
        protected IProcessService processService;

        virtual public void AddTrustedCertificate(string host, string hash)
        {
            // not implemented by default
        }

        public IWebClient(IProcessService p, Logger l, IConfigurationService c)
        {
            processService = p;
            logger = l;
            configService = c;
        }

        virtual protected void PrepareRequest(WebRequest request)
        {
            // add accept header if not set
            if (request.Headers == null)
                request.Headers = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            var hasAccept = false;
            foreach (var header in request.Headers)
            {
                var key = header.Key.ToLower();
                if (key == "accept")
                {
                    hasAccept = true;
                }
            }
            if (!hasAccept)
                request.Headers.Add("Accept", "*/*");
            return;
        }

        virtual public async Task<WebClientByteResult> GetBytes(WebRequest request)
        {
            logger.Debug(string.Format("IWebClient.GetBytes(Url:{0})", request.Url));
            PrepareRequest(request);
            var result = await Run(request);
            logger.Debug(string.Format("IWebClient: Returning {0} => {1} bytes", result.Status, (result.IsRedirect ? result.RedirectingTo + " " : "") + (result.Content == null ? "<NULL>" : result.Content.Length.ToString())));
            return result;
        }

        virtual public async Task<WebClientStringResult> GetString(WebRequest request)
        {
            logger.Debug(string.Format("IWebClient.GetString(Url:{0})", request.Url));
            PrepareRequest(request);
            var result = await Run(request);
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
