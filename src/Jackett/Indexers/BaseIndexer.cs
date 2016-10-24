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
        public string SiteLink { get; protected set; }
        public string DisplayDescription { get; protected set; }
        public string DisplayName { get; protected set; }
        public string ID { get { return GetIndexerID(GetType()); } }

        public bool IsConfigured { get; protected set; }
        public TorznabCapabilities TorznabCaps { get; protected set; }
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

        // standard constructor used by most indexers
        public BaseIndexer(string name, string link, string description, IIndexerManagerService manager, IWebClient client, Logger logger, ConfigurationData configData, IProtectionService p, TorznabCapabilities caps = null, string downloadBase = null)
            : this(manager, client, logger, p)
        {
            if (!link.EndsWith("/"))
                throw new Exception("Site link must end with a slash.");

            DisplayName = name;
            DisplayDescription = description;
            SiteLink = link;
            this.downloadUrlBase = downloadBase;
            this.configData = configData;

            if (caps == null)
                caps = TorznabUtil.CreateDefaultTorznabTVCaps();
            TorznabCaps = caps;

        }

        // minimal constructor used by e.g. cardigann generic indexer
        public BaseIndexer(IIndexerManagerService manager, IWebClient client, Logger logger, IProtectionService p)
        {
            this.logger = logger;
            indexerService = manager;
            webclient = client;
            protectionService = p;
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
            if (string.IsNullOrWhiteSpace(downloadUrlBase))
            {
                return link;
            }

            if (link.ToString().StartsWith(downloadUrlBase))
            {
                return link;
            }

            return new Uri(downloadUrlBase + link.ToString(), UriKind.RelativeOrAbsolute);
        }

        protected int MapTrackerCatToNewznab(string input)
        {
            if (null != input)
            {
                input = input.ToLowerInvariant();
                var mapping = categoryMapping.Where(m => m.TrackerCategory.ToLowerInvariant() == input.ToLowerInvariant()).FirstOrDefault();
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
            foreach (var expired in cache.Where(i => DateTime.Now - i.Created > cacheTime).ToList())
            {
                cache.Remove(expired);
            }
        }

        protected async Task FollowIfRedirect(WebClientStringResult response, string referrer = null, string overrideRedirectUrl = null, string overrideCookies = null, bool accumulateCookies = false)
        {
            var byteResult = new WebClientByteResult();
            // Map to byte
            Mapper.Map(response, byteResult);
            await FollowIfRedirect(byteResult, referrer, overrideRedirectUrl, overrideCookies, accumulateCookies);
            // Map to string
            Mapper.Map(byteResult, response);
        }

        protected async Task FollowIfRedirect(WebClientByteResult response, string referrer = null, string overrideRedirectUrl = null, string overrideCookies = null, bool accumulateCookies = false)
        {
            // Follow up  to 5 redirects
            for (int i = 0; i < 5; i++)
            {
                if (!response.IsRedirect)
                    break;
                await DoFollowIfRedirect(response, referrer, overrideRedirectUrl, overrideCookies, accumulateCookies);
                if (accumulateCookies)
                {
                    CookieHeader = ResolveCookies((CookieHeader != null && CookieHeader != ""? CookieHeader + " " : "") + (overrideCookies != null && overrideCookies != "" ? overrideCookies + " " : "") + response.Cookies);
                    overrideCookies = response.Cookies = CookieHeader;
                }
                if (overrideCookies != null && response.Cookies == null)
                {
                    response.Cookies = overrideCookies;
                }
            }
        }

        private String ResolveCookies(String incomingCookies = "")
        {
            var redirRequestCookies = (CookieHeader != null && CookieHeader != "" ? CookieHeader + " " : "") + incomingCookies;
            System.Text.RegularExpressions.Regex expression = new System.Text.RegularExpressions.Regex(@"([^\s]+)=([^=]+)(?:\s|$)");
            Dictionary<string, string> cookieDIctionary = new Dictionary<string, string>();
            var matches = expression.Match(redirRequestCookies);
            while (matches.Success)
            {
                if (matches.Groups.Count > 2) cookieDIctionary[matches.Groups[1].Value] = matches.Groups[2].Value;
                matches = matches.NextMatch();
            }
            return string.Join(" ", cookieDIctionary.Select(kv => kv.Key.ToString() + "=" + kv.Value.ToString()).ToArray());
            
        }

        // Update CookieHeader with new cookies and save the config if something changed (e.g. a new CloudFlare clearance cookie was issued)
        protected void UpdateCookieHeader(string newCookies, string cookieOverride = null)
        {
            string newCookieHeader = ResolveCookies((cookieOverride != null && cookieOverride != "" ? cookieOverride + " " : "") + newCookies);
            if (CookieHeader != newCookieHeader)
            {
                logger.Debug(string.Format("updating Cookies {0} => {1}", CookieHeader, newCookieHeader));
                CookieHeader = newCookieHeader;
                if (IsConfigured)
                    SaveConfig();
            }
        }

        private async Task DoFollowIfRedirect(WebClientByteResult incomingResponse, string referrer = null, string overrideRedirectUrl = null, string overrideCookies = null, bool accumulateCookies = false)
        {
            if (incomingResponse.IsRedirect)
            {
                var redirRequestCookies = "";
                if (accumulateCookies)
                {
                    redirRequestCookies = ResolveCookies((CookieHeader != "" ? CookieHeader + " " : "") + (overrideCookies != null ? overrideCookies : ""));
                } else
                {
                    redirRequestCookies = (overrideCookies != null ? overrideCookies : "");
                }
                // Do redirect
                var redirectedResponse = await webclient.GetBytes(new WebRequest()
                {
                    Url = overrideRedirectUrl ?? incomingResponse.RedirectingTo,
                    Referer = referrer,
                    Cookies = redirRequestCookies
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
            if (response.Status != System.Net.HttpStatusCode.OK && response.Status != System.Net.HttpStatusCode.Continue && response.Status != System.Net.HttpStatusCode.PartialContent)
            {
                throw new Exception($"Remote server returned {response.Status.ToString()}");
            }

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

        protected async Task<WebClientStringResult> RequestStringWithCookies(string url, string cookieOverride = null, string referer = null, Dictionary<string, string> headers = null)
        {
            var request = new Utils.Clients.WebRequest()
            {
                Url = url,
                Type = RequestType.GET,
                Cookies = CookieHeader,
                Referer = referer,
                Headers = headers
            };

            if (cookieOverride != null)
                request.Cookies = cookieOverride;
            WebClientStringResult result = await webclient.GetString(request);
            UpdateCookieHeader(result.Cookies, cookieOverride);
            return result;
        }

        protected async Task<WebClientStringResult> RequestStringWithCookiesAndRetry(string url, string cookieOverride = null, string referer = null, Dictionary<string, string> headers = null)
        {
            Exception lastException = null;
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    return await RequestStringWithCookies(url, cookieOverride, referer, headers);
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

        protected async Task<WebClientStringResult> PostDataWithCookies(string url, IEnumerable<KeyValuePair<string, string>> data, string cookieOverride = null, string referer = null, Dictionary<string, string> headers = null, string rawbody = null, bool? emulateBrowser = null)
        {
            var request = new Utils.Clients.WebRequest()
            {
                Url = url,
                Type = RequestType.POST,
                Cookies = cookieOverride ?? CookieHeader,
                PostData = data,
                Referer = referer,
                Headers = headers,
                RawBody = rawbody
            };

            if (emulateBrowser.HasValue)
                request.EmulateBrowser = emulateBrowser.Value;
            WebClientStringResult result = await webclient.GetString(request);
            UpdateCookieHeader(result.Cookies, cookieOverride);
            return result;
        }

        protected async Task<WebClientStringResult> PostDataWithCookiesAndRetry(string url, IEnumerable<KeyValuePair<string, string>> data, string cookieOverride = null, string referer = null, Dictionary<string, string> headers = null, string rawbody = null, bool? emulateBrowser = null)
        {
            Exception lastException = null;
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    return await PostDataWithCookies(url, data, cookieOverride, referer, headers, rawbody, emulateBrowser);
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

        protected async Task<WebClientStringResult> RequestLoginAndFollowRedirect(string url, IEnumerable<KeyValuePair<string, string>> data, string cookies, bool returnCookiesFromFirstCall, string redirectUrlOverride = null, string referer = null, bool accumulateCookies = false)
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
            if (accumulateCookies)
            {
                response.Cookies = ResolveCookies((request.Cookies == null ? "" : request.Cookies + " ") + response.Cookies);
            }
            var firstCallCookies = response.Cookies;

            if (response.IsRedirect)
            {
                await FollowIfRedirect(response, request.Url, redirectUrlOverride, response.Cookies, accumulateCookies);
            }

            if (returnCookiesFromFirstCall)
            {
                response.Cookies = ResolveCookies(firstCallCookies + (accumulateCookies ? " " + response.Cookies : ""));
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

        protected void AddCategoryMapping(string trackerCategory, TorznabCategory newznabCategory)
        {
            categoryMapping.Add(new CategoryMapping(trackerCategory, newznabCategory.ID));
            if (!TorznabCaps.Categories.Contains(newznabCategory))
                TorznabCaps.Categories.Add(newznabCategory);
        }

        protected void AddCategoryMapping(int trackerCategory, TorznabCategory newznabCategory)
        {
            AddCategoryMapping(trackerCategory.ToString(), newznabCategory);
        }

        protected void AddMultiCategoryMapping(TorznabCategory newznabCategory, params int[] trackerCategories)
        {
            foreach (var trackerCat in trackerCategories)
            {
                AddCategoryMapping(trackerCat, newznabCategory);
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
