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
using System.Threading;
using Jackett.Models.IndexerConfig;

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
        protected IProtectionService protectionService;
        protected readonly string downloadUrlBase = "";

        protected string CookieHeader
        {
            get { return configData.CookieHeader.Value; }
            set { configData.CookieHeader.Value = value; }
        }



        protected ConfigurationData configData;

        private List<CategoryMapping> categoryMapping = new List<CategoryMapping>();

        public BaseIndexer(string name, string link, string description, IIndexerManagerService manager, IWebClient client, Logger logger, ConfigurationData configData, IProtectionService p, TorznabCapabilities caps = null, string downloadBase = null)
        {
            if (!link.EndsWith("/"))
                throw new Exception("Site link must end with a slash.");

            DisplayName = name;
            DisplayDescription = description;
            SiteLink = link;
            this.logger = logger;
            indexerService = manager;
            webclient = client;
            protectionService = p;
            this.downloadUrlBase = downloadBase;

            this.configData = configData;

            if (caps == null)
                caps = TorznabUtil.CreateDefaultTorznabTVCaps();
            TorznabCaps = caps;

        }

        public IEnumerable<ReleaseInfo> CleanLinks(IEnumerable<ReleaseInfo> releases)
        {
            if (string.IsNullOrEmpty(downloadUrlBase))
                return releases;
            foreach (var release in releases)
            {
                if (release.Link.ToString().StartsWith(downloadUrlBase))
                {
                    release.Link = new Uri(release.Link.ToString().Substring(downloadUrlBase.Length), UriKind.Relative);
                }
            }

            return releases;
        }

        public Uri UncleanLink(Uri link)
        {
            return new Uri(downloadUrlBase + link.ToString(), UriKind.RelativeOrAbsolute);
        }

        protected int MapTrackerCatToNewznab(string input)
        {
            if (null != input)
            {
                input = input.ToLowerInvariant();
                var mapping = categoryMapping.Where(m => m.TrackerCategory == input).FirstOrDefault();
                if (mapping != null)
                {
                    return mapping.NewzNabCategory;
                }
            }
            return 0;
        }

        public static string GetIndexerID(Type type)
        {
            return StringUtil.StripNonAlphaNumeric(type.Name.ToLowerInvariant());
        }

        public virtual Task<ConfigurationData> GetConfigurationForSetup()
        {
            return Task.FromResult<ConfigurationData>(configData);
        }

        public virtual void ResetBaseConfig()
        {
            CookieHeader = string.Empty;
            IsConfigured = false;
        }

        protected virtual void SaveConfig()
        {
            indexerService.SaveConfig(this as IIndexer, configData.ToJson(protectionService, forDisplay: false));
        }

        protected void OnParseError(string results, Exception ex)
        {
            var fileName = string.Format("Error on {0} for {1}.txt", DateTime.Now.ToString("yyyyMMddHHmmss"), DisplayName);
            var spacing = string.Join("", Enumerable.Repeat(Environment.NewLine, 5));
            var fileContents = string.Format("{0}{1}{2}", ex, spacing, results);
            logger.Error(fileName + fileContents);
        }

        protected void CleanCache()
        {
            foreach (var expired in cache.Where(i => i.Created - DateTime.Now > cacheTime).ToList())
            {
                cache.Remove(expired);
            }
        }

        protected async Task FollowIfRedirect(WebClientStringResult response, string referrer = null, string overrideRedirectUrl = null, string overrideCookies = null)
        {
            var byteResult = new WebClientByteResult();
            // Map to byte
            Mapper.Map(response, byteResult);
            await FollowIfRedirect(byteResult, referrer, overrideRedirectUrl, overrideCookies);
            // Map to string
            Mapper.Map(byteResult, response);
        }

        protected async Task FollowIfRedirect(WebClientByteResult response, string referrer = null, string overrideRedirectUrl = null, string overrideCookies = null)
        {
            // Follow up  to 5 redirects
            for (int i = 0; i < 5; i++)
            {
                if (!response.IsRedirect)
                    break;
                await DoFollowIfRedirect(response, referrer, overrideRedirectUrl, overrideCookies);
            }
        }

        private async Task DoFollowIfRedirect(WebClientByteResult incomingResponse, string referrer = null, string overrideRedirectUrl = null, string overrideCookies = null)
        {
            if (incomingResponse.IsRedirect)
            {
                // Do redirect
                var redirectedResponse = await webclient.GetBytes(new WebRequest()
                {
                    Url = overrideRedirectUrl ?? incomingResponse.RedirectingTo,
                    Referer = referrer,
                    Cookies = overrideCookies ?? CookieHeader
                });
                Mapper.Map(redirectedResponse, incomingResponse);
            }
        }


        protected void LoadLegacyCookieConfig(JToken jsonConfig)
        {
            string legacyCookieHeader = (string)jsonConfig["cookie_header"];
            if (!string.IsNullOrEmpty(legacyCookieHeader))
            {
                CookieHeader = legacyCookieHeader;
            }
            else
            {
                // Legacy cookie key
                var jcookies = jsonConfig["cookies"];
                if (jcookies is JArray)
                {
                    var array = (JArray)jcookies;
                    legacyCookieHeader = string.Empty;
                    for (int i = 0; i < array.Count; i++)
                    {
                        if (i != 0)
                            legacyCookieHeader += "; ";
                        legacyCookieHeader += array[i];
                    }
                    CookieHeader = legacyCookieHeader;
                }
                else if (jcookies != null)
                {
                    CookieHeader = (string)jcookies;
                }
            }
        }

        public virtual void LoadFromSavedConfiguration(JToken jsonConfig)
        {
            if (jsonConfig is JArray)
            {
                configData.LoadValuesFromJson(jsonConfig, protectionService);
                IsConfigured = true;
            }
            // read and upgrade old settings file format
            else if (jsonConfig is Object)
            {
                LoadLegacyCookieConfig(jsonConfig);
                SaveConfig();
                IsConfigured = true;
            }
        }

        public async virtual Task<byte[]> Download(Uri link)
        {
            var response = await RequestBytesWithCookiesAndRetry(link.ToString());
            return response.Content;
        }

        protected async Task<WebClientByteResult> RequestBytesWithCookiesAndRetry(string url, string cookieOverride = null)
        {
            Exception lastException = null;
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    return await RequestBytesWithCookies(url, cookieOverride);
                }
                catch (Exception e)
                {
                    logger.Error(string.Format("On attempt {0} downloading from {1}: {2}", (i + 1), DisplayName, e.Message));
                    lastException = e;
                }
                await Task.Delay(500);
            }

            throw lastException;
        }

        protected async Task<WebClientStringResult> RequestStringWithCookies(string url, string cookieOverride = null, string referer = null)
        {
            var request = new Utils.Clients.WebRequest()
            {
                Url = url,
                Type = RequestType.GET,
                Cookies = CookieHeader,
                Referer = referer
            };

            if (cookieOverride != null)
                request.Cookies = cookieOverride;
            return await webclient.GetString(request);
        }

        protected async Task<WebClientStringResult> RequestStringWithCookiesAndRetry(string url, string cookieOverride = null, string referer = null)
        {
            Exception lastException = null;
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    return await RequestStringWithCookies(url, cookieOverride, referer);
                }
                catch (Exception e)
                {
                    logger.Error(string.Format("On attempt {0} checking for results from {1}: {2}", (i + 1), DisplayName, e.Message));
                    lastException = e;
                }
                await Task.Delay(500);
            }

            throw lastException;
        }

        protected async Task<WebClientByteResult> RequestBytesWithCookies(string url, string cookieOverride = null)
        {
            var request = new Utils.Clients.WebRequest()
            {
                Url = url,
                Type = RequestType.GET,
                Cookies = cookieOverride ?? CookieHeader
            };

            if (cookieOverride != null)
                request.Cookies = cookieOverride;
            return await webclient.GetBytes(request);
        }

        protected async Task<WebClientStringResult> PostDataWithCookies(string url, IEnumerable<KeyValuePair<string, string>> data, string cookieOverride = null)
        {
            var request = new Utils.Clients.WebRequest()
            {
                Url = url,
                Type = RequestType.POST,
                Cookies = cookieOverride ?? CookieHeader,
                PostData = data
            };
            return await webclient.GetString(request);
        }

        protected async Task<WebClientStringResult> PostDataWithCookiesAndRetry(string url, IEnumerable<KeyValuePair<string, string>> data, string cookieOverride = null)
        {
            Exception lastException = null;
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    return await PostDataWithCookies(url, data, cookieOverride);
                }
                catch (Exception e)
                {
                    logger.Error(string.Format("On attempt {0} checking for results from {1}: {2}", (i + 1), DisplayName, e.Message));
                    lastException = e;
                }
                await Task.Delay(500);
            }

            throw lastException;
        }

        protected async Task<WebClientStringResult> RequestLoginAndFollowRedirect(string url, IEnumerable<KeyValuePair<string, string>> data, string cookies, bool returnCookiesFromFirstCall, string redirectUrlOverride = null, string referer = null)
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

            if (response.IsRedirect)
            {
                await FollowIfRedirect(response, request.Url, null, response.Cookies);
            }

            if (returnCookiesFromFirstCall)
            {
                response.Cookies = firstCallCookies;
            }

            return response;
        }

        protected async Task ConfigureIfOK(string cookies, bool isLoggedin, Func<Task> onError)
        {
            if (isLoggedin)
            {
                CookieHeader = cookies;
                SaveConfig();
                IsConfigured = true;
            }
            else
            {
                await onError();
            }
        }

        public virtual IEnumerable<ReleaseInfo> FilterResults(TorznabQuery query, IEnumerable<ReleaseInfo> results)
        {
            foreach (var result in results)
            {
                if (query.Categories.Length == 0 || query.Categories.Contains(result.Category) || result.Category == 0 || TorznabCatType.QueryContainsParentCategory(query.Categories, result.Category))
                {
                    yield return result;
                }
            }
        }

        protected void AddCategoryMapping(string trackerCategory, int newznabCategory)
        {
            categoryMapping.Add(new CategoryMapping(trackerCategory, newznabCategory));
        }

        protected void AddCategoryMapping(int trackerCategory, TorznabCategory newznabCategory)
        {
            categoryMapping.Add(new CategoryMapping(trackerCategory.ToString(), newznabCategory.ID));
            if (!TorznabCaps.Categories.Contains(newznabCategory))
                TorznabCaps.Categories.Add(newznabCategory);
        }

        protected void AddCategoryMapping(string trackerCategory, TorznabCategory newznabCategory)
        {
            categoryMapping.Add(new CategoryMapping(trackerCategory.ToString(), newznabCategory.ID));
            if (!TorznabCaps.Categories.Contains(newznabCategory))
                TorznabCaps.Categories.Add(newznabCategory);
        }

        protected void AddCategoryMapping(int trackerCategory, int newznabCategory)
        {
            categoryMapping.Add(new CategoryMapping(trackerCategory.ToString(), newznabCategory));
        }

        protected void AddMultiCategoryMapping(TorznabCategory newznabCategory, params int[] trackerCategories)
        {
            foreach (var trackerCat in trackerCategories)
            {
                categoryMapping.Add(new CategoryMapping(trackerCat.ToString(), newznabCategory.ID));
            }
        }

        protected void AddMultiCategoryMapping(int trackerCategory, params TorznabCategory[] newznabCategories)
        {
            foreach (var newznabCat in newznabCategories)
            {
                categoryMapping.Add(new CategoryMapping(trackerCategory.ToString(), newznabCat.ID));
            }
        }

        protected List<string> MapTorznabCapsToTrackers(TorznabQuery query, bool mapChildrenCatsToParent = false)
        {
            var result = new List<string>();
            foreach (var cat in query.Categories)
            {
                var queryCats = new List<int> { cat };
                var newznabCat = TorznabCatType.AllCats.FirstOrDefault(c => c.ID == cat);
                if (newznabCat != null)
                {
                    queryCats.AddRange(newznabCat.SubCategories.Select(c => c.ID));
                }

                if (mapChildrenCatsToParent)
                {
                    var parentNewznabCat = TorznabCatType.AllCats.FirstOrDefault(c => c.SubCategories.Contains(newznabCat));
                    if (parentNewznabCat != null)
                    {
                        queryCats.Add(parentNewznabCat.ID);
                    }
                }

                foreach (var mapping in categoryMapping.Where(c => queryCats.Contains(c.NewzNabCategory)))
                {
                    result.Add(mapping.TrackerCategory);
                }
            }

            return result.Distinct().ToList();
        }
    }
}
