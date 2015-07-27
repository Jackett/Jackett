using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jackett.Models;
using Newtonsoft.Json.Linq;
using NLog;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
using AutoMapper;

namespace Jackett.Indexers
{
    public abstract class BaseIndexer
    {
        public string SiteLink { get; private set; }
        public string DisplayDescription { get; private set; }
        public string DisplayName { get; private set; }
        public string ID { get { return GetIndexerID(GetType()); } }

        public bool IsConfigured { get; protected set; }
        public TorznabCapabilities TorznabCaps { get; private set; }
        protected Logger logger;
        protected IIndexerManagerService indexerService;
        protected static List<CachedQueryResult> cache = new List<CachedQueryResult>();
        protected static readonly TimeSpan cacheTime = new TimeSpan(0, 9, 0);
        protected IWebClient webclient;
        protected string cookieHeader = "";

        public BaseIndexer(string name, string link, string description, IIndexerManagerService manager, IWebClient client, Logger logger, TorznabCapabilities caps = null)
        {
            if (!link.EndsWith("/"))
                throw new Exception("Site link must end with a slash.");

            DisplayName = name;
            DisplayDescription = description;
            SiteLink = link;
            this.logger = logger;
            indexerService = manager;
            webclient = client;

            if (caps == null)
                caps = TorznabCapsUtil.CreateDefaultTorznabTVCaps();
            TorznabCaps = caps;
        }

        public static string GetIndexerID(Type type)
        {
            return StringUtil.StripNonAlphaNumeric(type.Name.ToLowerInvariant());
        }

        public void ResetBaseConfig()
        {
            cookieHeader = string.Empty;
            IsConfigured = false;
        }

        protected void SaveConfig(JToken config)
        {
            indexerService.SaveConfig(this as IIndexer, config);
        }

        protected void OnParseError(string results, Exception ex)
        {
            var fileName = string.Format("Error on {0} for {1}.txt", DateTime.Now.ToString("yyyyMMddHHmmss"), DisplayName);
            var spacing = string.Join("", Enumerable.Repeat(Environment.NewLine, 5));
            var fileContents = string.Format("{0}{1}{2}", ex, spacing, results);
            logger.Error(fileName + fileContents);
            throw ex;
        }

        protected void CleanCache()
        {
            foreach (var expired in cache.Where(i => i.Created - DateTime.Now > cacheTime).ToList())
            {
                cache.Remove(expired);
            }
        }

        protected async Task FollowIfRedirect(WebRequest request, WebClientStringResult incomingResponse, string overrideRedirectUrl = null, string overrideCookies = null)
        {
            if (incomingResponse.Status == System.Net.HttpStatusCode.Redirect ||
               incomingResponse.Status == System.Net.HttpStatusCode.RedirectKeepVerb ||
               incomingResponse.Status == System.Net.HttpStatusCode.RedirectMethod ||
               incomingResponse.Status == System.Net.HttpStatusCode.Found)
            {
                // Do redirect
                var redirectedResponse = await webclient.GetString(new WebRequest()
                {
                    Url = overrideRedirectUrl??incomingResponse.RedirectingTo,
                    Referer = request.Url,
                    Cookies = overrideCookies??cookieHeader
                });
                Mapper.Map(redirectedResponse, incomingResponse);
            }
        }

        protected async void FollowIfRedirect(WebRequest request, WebClientByteResult incomingResponse, string overrideRedirectUrl)
        {
            if (incomingResponse.Status == System.Net.HttpStatusCode.Redirect ||
               incomingResponse.Status == System.Net.HttpStatusCode.RedirectKeepVerb ||
               incomingResponse.Status == System.Net.HttpStatusCode.RedirectMethod ||
               incomingResponse.Status == System.Net.HttpStatusCode.Found)
            {
                // Do redirect
                var redirectedResponse = await webclient.GetBytes(new WebRequest()
                {
                    Url = overrideRedirectUrl??incomingResponse.RedirectingTo,
                    Referer = request.Url,
                    Cookies = cookieHeader
                });
                Mapper.Map(redirectedResponse, incomingResponse);
            }
        }

        protected void LoadCookieHeaderAndConfigure(JToken jsonConfig)
        {
            cookieHeader = (string)jsonConfig["cookie_header"];
            if (!string.IsNullOrEmpty(cookieHeader))
            {
                IsConfigured = true;
            }
            else
            {
                // Legacy cookie key
                cookieHeader = (string)jsonConfig["cookies"];
                if (!string.IsNullOrEmpty(cookieHeader))
                {
                    IsConfigured = true;
                }
            }
        }

        protected void SaveCookieHeaderAndConfigure()
        {
            var configSaveData = new JObject();
            configSaveData["cookie_header"] = cookieHeader;
            SaveConfig(configSaveData);
            IsConfigured = !string.IsNullOrEmpty(cookieHeader);
        }

        public virtual void LoadFromSavedConfiguration(JToken jsonConfig)
        {
            LoadCookieHeaderAndConfigure(jsonConfig);
        }

        public async virtual Task<byte[]> Download(Uri link)
        {
            var response = await webclient.GetBytes(new Utils.Clients.WebRequest()
            {
                Url = link.ToString(),
                Cookies = cookieHeader
            });

            return response.Content;
        }

        protected async Task<WebClientStringResult> RequestStringWithCookies(string url, string cookieOverride = null, string referer = null)
        {
            var request = new Utils.Clients.WebRequest()
            {
                Url = url,
                Type = RequestType.GET,
                Cookies = cookieHeader,
                Referer = referer
            };

            if (cookieOverride != null)
                request.Cookies = cookieOverride;
            return await webclient.GetString(request);
        }

        protected async Task<WebClientByteResult> RequestBytesWithCookies(string url, string cookieOverride = null)
        {
            var request = new Utils.Clients.WebRequest()
            {
                Url = url,
                Type = RequestType.GET,
                Cookies = cookieOverride ?? cookieHeader
            };

            if (cookieOverride != null)
                request.Cookies = cookieOverride;
            return await webclient.GetBytes(request);
        }

        protected async Task<WebClientStringResult> PostDataWithCookies(string url, Dictionary<string, string> data, string cookieOverride = null)
        {
            var request = new Utils.Clients.WebRequest()
            {
                Url = url,
                Type = RequestType.POST,
                Cookies = cookieOverride ?? cookieHeader,
                PostData = data
            };
            return await webclient.GetString(request);
        }

        protected async Task<WebClientStringResult> RequestLoginAndFollowRedirect(string url, Dictionary<string, string> data, string cookies, bool returnCookiesFromFirstCall, string redirectUrlOverride = null, string referer =null)
        {
            var request = new Utils.Clients.WebRequest()
            {
                Url = url,
                Type = RequestType.POST,
                Cookies = cookies,
                Referer = referer,
                PostData = data
            };
            var response = await webclient.GetString(request);
            var firstCallCookies = response.Cookies;
            await FollowIfRedirect(request, response, SiteLink, response.Cookies);

            if (returnCookiesFromFirstCall)
            {
                response.Cookies = firstCallCookies;
            }

            return response;
        }

        protected void ConfigureIfOK(string cookies, bool isLoggedin, Action onError)
        {
            if (isLoggedin)
            {
                cookieHeader = cookies;
                SaveCookieHeaderAndConfigure();
            } else
            {
                onError();
            }
        }
    }
}
