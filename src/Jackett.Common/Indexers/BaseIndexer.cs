using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jackett.Common.Exceptions;
using Jackett.Common.Extensions;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using Polly;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;

namespace Jackett.Common.Indexers
{
    public abstract class BaseIndexer : IIndexer
    {
        public virtual string Id { get; protected set; }
        public virtual string Name { get; protected set; }
        public virtual string Description { get; protected set; }

        public virtual string SiteLink { get; protected set; }
        public string DefaultSiteLink { get; protected set; }
        public virtual string[] AlternativeSiteLinks { get; protected set; } = Array.Empty<string>();
        public virtual string[] LegacySiteLinks { get; protected set; } = Array.Empty<string>();

        [JsonConverter(typeof(EncodingJsonConverter))]
        public virtual Encoding Encoding { get; protected set; }
        public virtual string Language { get; protected set; } = "en-US";
        public virtual string Type { get; protected set; }

        public virtual bool SupportsPagination => false;

        public virtual bool IsConfigured { get; protected set; }
        public virtual string[] Tags { get; protected set; }

        // https://github.com/Jackett/Jackett/issues/3292#issuecomment-838586679
        private TimeSpan HealthyStatusValidity => cacheService.CacheTTL + cacheService.CacheTTL;
        private static readonly TimeSpan ErrorStatusValidity = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan MaxStatusValidity = TimeSpan.FromDays(1);

        private int errorCount;
        private DateTime expireAt;

        protected Logger logger;
        protected IIndexerConfigurationService configurationService;
        protected IProtectionService protectionService;
        protected ICacheService cacheService;

        protected ConfigurationData configData;

        protected string CookieHeader
        {
            get => configData.CookieHeader.Value;
            set => configData.CookieHeader.Value = value;
        }

        public string LastError
        {
            get => configData.LastError.Value;
            set
            {
                var SaveNeeded = configData.LastError.Value != value && IsConfigured;
                configData.LastError.Value = value;
                if (SaveNeeded)
                    SaveConfig();
            }
        }

        public virtual bool IsHealthy => errorCount == 0 && expireAt > DateTime.Now;
        public virtual bool IsFailing => errorCount > 0 && expireAt > DateTime.Now;


        public abstract TorznabCapabilities TorznabCaps { get; protected set; }

        // standard constructor used by most indexers
        public BaseIndexer(IIndexerConfigurationService configService, Logger logger, ConfigurationData configData, IProtectionService p, ICacheService cs)
        {
            this.logger = logger;
            configurationService = configService;
            protectionService = p;
            cacheService = cs;

            if (SiteLink.IsNotNullOrWhiteSpace() && !SiteLink.EndsWith("/", StringComparison.Ordinal))
                throw new Exception("Site link must end with a slash.");

            DefaultSiteLink = SiteLink;

            this.configData = configData;
            if (configData != null)
                LoadValuesFromJson(null);
        }

        public virtual Task<ConfigurationData> GetConfigurationForSetup() => Task.FromResult<ConfigurationData>(configData);

        public virtual void ResetBaseConfig()
        {
            CookieHeader = string.Empty;
            IsConfigured = false;
            errorCount = 0;
            expireAt = DateTime.MinValue;
        }

        public virtual void SaveConfig() => configurationService.Save(this as IIndexer, configData.ToJson(protectionService, forDisplay: false));

        public virtual void LoadValuesFromJson(JToken jsonConfig, bool useProtectionService = false)
        {
            IProtectionService ps = null;
            if (useProtectionService)
                ps = protectionService;
            configData.LoadConfigDataValuesFromJson(jsonConfig, ps);
            if (string.IsNullOrWhiteSpace(configData.SiteLink.Value))
            {
                configData.SiteLink.Value = DefaultSiteLink;
            }

            if (!configData.SiteLink.Value.EndsWith("/", StringComparison.Ordinal))
                configData.SiteLink.Value += "/";

            // reset site link to default if it's a legacy (defunc link)
            if (LegacySiteLinks != null && LegacySiteLinks.Contains(configData.SiteLink.Value))
            {
                logger.Debug(string.Format("changing legacy site link from {0} to {1}", configData.SiteLink.Value, DefaultSiteLink));
                configData.SiteLink.Value = DefaultSiteLink;
            }

            // check whether the site link is well-formatted
            var siteUri = new Uri(configData.SiteLink.Value);
            SiteLink = configData.SiteLink.Value;

            Tags = configData.Tags.Values.Select(t => t.ToLowerInvariant()).ToArray();
        }

        public void LoadFromSavedConfiguration(JToken jsonConfig)
        {
            if (jsonConfig is JArray)
            {
                LoadValuesFromJson(jsonConfig, true);
                IsConfigured = true;
            }
            else
                logger.Warn("Some of the configuration files (.json) are in the old format. Please, update Jackett.");
        }

        protected async Task ConfigureIfOK(string cookies, bool isLoggedin, Func<Task> onError)
        {
            if (isLoggedin)
            {
                CookieHeader = cookies;
                IsConfigured = true;
                SaveConfig();
            }
            else
            {
                await onError();
            }
        }

        protected virtual IEnumerable<ReleaseInfo> FilterResults(TorznabQuery query, IEnumerable<ReleaseInfo> results)
        {
            var filteredResults = results;

            // filter results with wrong categories
            if (query.Categories.Length > 0)
            {
                // expand parent categories from the query
                var expandedQueryCats = TorznabCaps.Categories.ExpandTorznabQueryCategories(query);

                filteredResults = filteredResults.Where(result =>
                    result.Category?.Any() != true ||
                    expandedQueryCats.Intersect(result.Category).Any()
                );
            }

            // eliminate excess results
            if (query.Limit > 0)
                filteredResults = filteredResults.Take(query.Limit);

            return filteredResults;
        }

        protected virtual IEnumerable<ReleaseInfo> FixResults(TorznabQuery query, IEnumerable<ReleaseInfo> results)
        {
            var fixedResults = results.Select(r =>
            {
                // add origin
                r.Origin = this;

                // fix publish date
                // some trackers do not keep their clocks up to date and can be ~20 minutes out!
                if (!EnvironmentUtil.IsDebug && r.PublishDate > DateTime.Now)
                    r.PublishDate = DateTime.Now;

                // generate magnet link from info hash (not allowed for private sites)
                if (r.MagnetUri == null && !string.IsNullOrWhiteSpace(r.InfoHash) && Type != "private")
                    r.MagnetUri = MagnetUtil.InfoHashToPublicMagnet(r.InfoHash, r.Title);

                // generate info hash from magnet link
                if (r.MagnetUri != null && string.IsNullOrWhiteSpace(r.InfoHash))
                    r.InfoHash = MagnetUtil.MagnetToInfoHash(r.MagnetUri);

                // set guid
                if (r.Guid == null)
                {
                    if (r.Link != null)
                        r.Guid = r.Link;
                    else if (r.MagnetUri != null)
                        r.Guid = r.MagnetUri;
                    else if (r.Details != null)
                        r.Guid = r.Details;
                }

                return r;
            });
            return fixedResults;
        }

        public virtual bool CanHandleQuery(TorznabQuery query)
        {
            if (query == null)
                return false;
            if (query.QueryType == "caps")
                return true;

            var caps = TorznabCaps;
            if (caps.TvSearchImdbAvailable && query.IsImdbQuery && query.IsTVSearch)
                return true;
            if (caps.MovieSearchImdbAvailable && query.IsImdbQuery && query.IsMovieSearch)
                return true;
            if (!caps.MovieSearchImdbAvailable && query.IsImdbQuery && query.QueryType != "TorrentPotato") // potato query should always contain imdb+search term
                return false;
            if (caps.SearchAvailable && query.IsSearch)
                return true;
            if (caps.TvSearchAvailable && query.IsTVSearch)
                return true;
            if (caps.MovieSearchAvailable && query.IsMovieSearch)
                return true;
            if (caps.MusicSearchAvailable && query.IsMusicSearch)
                return true;
            if (caps.BookSearchAvailable && query.IsBookSearch)
                return true;
            if (caps.TvSearchTvRageAvailable && query.IsTVRageSearch)
                return true;
            if (caps.TvSearchTvdbAvailable && query.IsTvdbSearch)
                return true;
            if (caps.MovieSearchImdbAvailable && query.IsImdbQuery)
                return true;
            if (caps.MovieSearchTmdbAvailable && query.IsTmdbQuery && query.IsMovieSearch)
                return true;
            if (caps.TvSearchTmdbAvailable && query.IsTmdbQuery && query.IsTVSearch)
                return true;

            return false;
        }

        protected bool CanHandleCategories(TorznabQuery query, bool isMetaIndexer = false)
        {
            // https://torznab.github.io/spec-1.3-draft/torznab/Specification-v1.3.html#cat-parameter
            if (query.HasSpecifiedCategories)
            {
                var supportedCats = TorznabCaps.Categories.SupportedCategories(query.Categories);
                if (supportedCats.Length == 0)
                {
                    if (!isMetaIndexer)
                        logger.Error($"All categories provided are unsupported in {Name}: {string.Join(",", query.Categories)}");
                    return false;
                }
                if (supportedCats.Length != query.Categories.Length && !isMetaIndexer)
                {
                    var unsupportedCats = query.Categories.Except(supportedCats);
                    logger.Warn($"Some of the categories provided are unsupported in {Name}: {string.Join(",", unsupportedCats)}");
                }
            }
            return true;
        }

        public void Unconfigure()
        {
            IsConfigured = false;
            SiteLink = DefaultSiteLink;
            CookieHeader = ""; // clear cookies
        }

        public abstract Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson);

        public virtual async Task<IndexerResult> ResultsForQuery(TorznabQuery query, bool isMetaIndexer)
        {
            // we make a copy just in case some C# indexer modifies the object.
            // without the copy, if you make a request with several indexers, all indexers share the query object.
            var queryCopy = query.Clone();

            if (!CanHandleQuery(queryCopy) || !CanHandleCategories(queryCopy, isMetaIndexer))
                return new IndexerResult(this, Array.Empty<ReleaseInfo>(), false);

            if (!SupportsPagination && queryCopy.Offset > 0)
                return new IndexerResult(this, Array.Empty<ReleaseInfo>(), false);

            if (queryCopy.Cache)
            {
                var cachedReleases = cacheService.Search(this, queryCopy);
                if (cachedReleases != null)
                    return new IndexerResult(this, cachedReleases, true);
            }

            try
            {
                var results = await PerformQuery(queryCopy);
                results = FilterResults(queryCopy, results);
                results = FixResults(queryCopy, results);
                cacheService.CacheResults(this, queryCopy, results.ToList());
                errorCount = 0;
                expireAt = DateTime.Now.Add(HealthyStatusValidity);
                return new IndexerResult(this, results, false);
            }
            catch (TooManyRequestsException ex)
            {
                var delay = ex.RetryAfter.TotalSeconds;
                expireAt = DateTime.Now.AddSeconds(delay);
                throw new IndexerException(this, ex);
            }
            catch (Exception ex)
            {
                var delay = Math.Min(MaxStatusValidity.TotalSeconds, ErrorStatusValidity.TotalSeconds * Math.Pow(2, errorCount++));
                expireAt = DateTime.Now.AddSeconds(delay);
                throw new IndexerException(this, ex);
            }
        }

        protected abstract Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query);
    }

    public abstract class BaseWebIndexer : BaseIndexer, IWebIndexer
    {
        protected BaseWebIndexer(IIndexerConfigurationService configService, WebClient client, Logger logger,
                                 ConfigurationData configData, IProtectionService p, ICacheService cacheService,
                                 string downloadBase = null)
            : base(configService: configService, logger: logger, configData: configData, p: p, cs: cacheService)
        {
            webclient = client;
            downloadUrlBase = downloadBase;
        }

        // minimal constructor used by e.g. cardigann generic indexer
        protected BaseWebIndexer(IIndexerConfigurationService configService, WebClient client, Logger logger,
            IProtectionService p, ICacheService cacheService)
            : base(configService: configService, logger: logger, configData: null, p: p, cs: cacheService)
        {
            webclient = client;
        }

        protected virtual int DefaultNumberOfRetryAttempts => 2;

        /// <summary>
        /// Number of retry attempts to make if a web request fails.
        /// </summary>
        /// <remarks>
        /// Number of retries can be overridden for unstable indexers by overriding this property. Note that retry attempts include an
        /// exponentially increasing delay.
        ///
        /// Alternatively, <see cref="EnableConfigurableRetryAttempts()" /> can be called in the constructor to add user configurable options.
        /// </remarks>
        protected virtual int NumberOfRetryAttempts
        {
            get
            {
                var configItem = configData.GetDynamic("retryAttempts");
                if (configItem == null)
                {
                    // No config specified so use the default.
                    return DefaultNumberOfRetryAttempts;
                }

                var configValue = ((SingleSelectConfigurationItem)configItem).Value;

                if (int.TryParse(configValue, out var parsedConfigValue) && parsedConfigValue > 0)
                {
                    return parsedConfigValue;
                }
                else
                {
                    // No config specified so use the default.
                    return DefaultNumberOfRetryAttempts;
                }
            }
        }

        private AsyncPolicy<WebResult> RetryPolicy
        {
            get
            {
                // Configure the retry policy
                int attemptNumber = 1;
                var retryPolicy = Policy
                    .HandleResult<WebResult>(r => (int)r.Status >= 500)
                    .Or<Exception>()
                    .WaitAndRetryAsync(
                        NumberOfRetryAttempts,
                        retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt) / 4),
                        onRetry: (exception, timeSpan, context) =>
                        {
                            if (exception.Result == null)
                            {
                                logger.Warn($"Request to {Name} failed with exception '{exception.Exception.Message}'. Retrying in {timeSpan.TotalSeconds}s... (Attempt {attemptNumber} of {NumberOfRetryAttempts}).");
                            }
                            else
                            {
                                logger.Warn($"Request to {Name} failed with status {exception.Result.Status}. Retrying in {timeSpan.TotalSeconds}s... (Attempt {attemptNumber} of {NumberOfRetryAttempts}).");
                            }
                            attemptNumber++;
                        });
                return retryPolicy;
            }
        }

        /// <summary>
        /// Adds configuration options to allow the user to manually configure request retries.
        /// </summary>
        /// <remarks>
        /// This should only be enabled for indexers known to be unstable. To control the default value, override <see cref="DefaultNumberOfRetryAttempts" />.
        /// </remarks>
        protected void EnableConfigurableRetryAttempts()
        {
            var attemptSelect = new SingleSelectConfigurationItem(
                "Number of retries",
                new Dictionary<string, string>
                {
                    {"0", "No retries (fail fast)"},
                    {"1", "1 retry (0.5s delay)"},
                    {"2", "2 retries (1s delay)"},
                    {"3", "3 retries (2s delay)"},
                    {"4", "4 retries (4s delay)"},
                    {"5", "5 retries (8s delay)"}
                })
            {
                Value = DefaultNumberOfRetryAttempts.ToString()
            };
            configData.AddDynamic("retryAttempts", attemptSelect);
        }

        public virtual async Task<byte[]> Download(Uri link)
        {
            var uncleanLink = UncleanLink(link);
            return await Download(uncleanLink, RequestType.GET);
        }

        protected async Task<byte[]> Download(Uri link, RequestType method, string referer = null, Dictionary<string, string> headers = null)
        {
            // return magnet link
            if (link.Scheme == "magnet")
                return Encoding.UTF8.GetBytes(link.OriginalString);

            // do some extra escaping, needed for HD-Torrents
            var requestLink = link.ToString()
                .Replace("(", "%28")
                .Replace(")", "%29")
                .Replace("'", "%27");
            var response = await RequestWithCookiesAndRetryAsync(requestLink, null, method, referer, null, headers);

            if (response.IsRedirect)
            {
                await FollowIfRedirect(response);
            }

            if (response.IsRedirect)
            {
                var redirectingTo = new Uri(response.RedirectingTo);
                if (redirectingTo.Scheme == "magnet")
                    return Encoding.UTF8.GetBytes(redirectingTo.OriginalString);

                await FollowIfRedirect(response);
            }

            if (response.Status != System.Net.HttpStatusCode.OK && response.Status != System.Net.HttpStatusCode.Continue && response.Status != System.Net.HttpStatusCode.PartialContent)
            {
                logger.Error("Failed download cookies: " + CookieHeader);
                if (response.ContentBytes != null)
                    logger.Error("Failed download response:\n" + Encoding.UTF8.GetString(response.ContentBytes));
                throw new Exception($"Remote server returned {response.Status.ToString()}" + (response.IsRedirect ? " => " + response.RedirectingTo : ""));
            }

            return response.ContentBytes;
        }

        public virtual async Task<WebResult> DownloadImage(Uri link)
        {
            var uncleanLink = UncleanLink(link);
            var requestLink = uncleanLink.ToString();
            var referer = SiteLink;

            var response = await RequestWithCookiesAsync(requestLink, null, RequestType.GET, referer);
            if (response.IsRedirect)
                await FollowIfRedirect(response);

            return response;
        }

        protected async Task<WebResult> RequestWithCookiesAndRetryAsync(
            string url, string cookieOverride = null, RequestType method = RequestType.GET,
            string referer = null, IEnumerable<KeyValuePair<string, string>> data = null,
            Dictionary<string, string> headers = null, string rawbody = null, bool? emulateBrowser = null)
        {
            return await RetryPolicy.ExecuteAsync(async () =>
                await RequestWithCookiesAsync(url, cookieOverride, method, referer, data, headers, rawbody, emulateBrowser)
            );
        }

        protected virtual async Task<WebResult> RequestWithCookiesAsync(
            string url, string cookieOverride = null, RequestType method = RequestType.GET,
            string referer = null, IEnumerable<KeyValuePair<string, string>> data = null,
            Dictionary<string, string> headers = null, string rawbody = null, bool? emulateBrowser = null)
        {
            var request = new WebRequest
            {
                Url = url,
                Type = method,
                Cookies = cookieOverride ?? CookieHeader,
                PostData = data,
                Referer = referer,
                Headers = headers,
                RawBody = rawbody,
                Encoding = Encoding
            };

            if (emulateBrowser.HasValue)
                request.EmulateBrowser = emulateBrowser.Value;
            var result = await webclient.GetResultAsync(request);
            CheckSiteDown(result);
            UpdateCookieHeader(result.Cookies, cookieOverride);
            return result;
        }

        protected async Task<WebResult> RequestLoginAndFollowRedirect(string url, IEnumerable<KeyValuePair<string, string>> data, string cookies, bool returnCookiesFromFirstCall, string redirectUrlOverride = null, string referer = null, bool accumulateCookies = false, Dictionary<string, string> headers = null)
        {
            var request = new WebRequest
            {
                Url = url,
                Type = RequestType.POST,
                Cookies = cookies ?? CookieHeader,
                Referer = referer,
                PostData = data,
                Encoding = Encoding,
                Headers = headers,
            };
            var response = await webclient.GetResultAsync(request);
            CheckSiteDown(response);
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

        protected static void CheckSiteDown(WebResult response)
        {
            if (response.Status == System.Net.HttpStatusCode.BadGateway
                || response.Status == System.Net.HttpStatusCode.GatewayTimeout
                || (int)response.Status == 521 // used by cloudflare to signal the original webserver is refusing the connection
                || (int)response.Status == 522 // used by cloudflare to signal the original webserver is not reachable at all (timeout)
                || (int)response.Status == 523 // used by cloudflare to signal the original webserver is not reachable at all (Origin is unreachable)
                )
            {
                throw new Exception("Request to " + response.Request.Url + " failed (Error " + response.Status + ") - The tracker seems to be down.");
            }
        }

        protected async Task FollowIfRedirect(WebResult response, string referrer = null, string overrideRedirectUrl = null, string overrideCookies = null, bool accumulateCookies = false)
        {
            // Follow up  to 5 redirects
            for (var i = 0; i < 5; i++)
            {
                if (!response.IsRedirect)
                    break;

                var redirectingTo = new Uri(response.RedirectingTo);
                if (redirectingTo.Scheme == "magnet")
                    break;

                await DoFollowIfRedirect(response, referrer, overrideRedirectUrl, overrideCookies, accumulateCookies);
                if (accumulateCookies)
                {
                    CookieHeader = ResolveCookies((CookieHeader != null && CookieHeader != "" ? CookieHeader + " " : "") + (overrideCookies != null && overrideCookies != "" ? overrideCookies + " " : "") + response.Cookies);
                    overrideCookies = response.Cookies = CookieHeader;
                }
                if (overrideCookies != null && response.Cookies == null)
                {
                    response.Cookies = overrideCookies;
                }
            }
        }

        private string ResolveCookies(string incomingCookies = "")
        {
            var redirRequestCookies = string.IsNullOrWhiteSpace(CookieHeader) ? incomingCookies : CookieHeader + " " + incomingCookies;
            var cookieDictionary = CookieUtil.CookieHeaderToDictionary(redirRequestCookies);

            // These cookies are causing BadGateway errors, so we drop them, see issue #2306
            cookieDictionary.Remove("cf_use_ob");
            cookieDictionary.Remove("cf_ob_info");

            return CookieUtil.CookieDictionaryToHeader(cookieDictionary);
        }

        // Update CookieHeader with new cookies and save the config if something changed (e.g. a new CloudFlare clearance cookie was issued)
        protected virtual void UpdateCookieHeader(string newCookies, string cookieOverride = null)
        {
            var newCookieHeader = ResolveCookies((cookieOverride != null && cookieOverride != "" ? cookieOverride + " " : "") + newCookies);
            if (CookieHeader != newCookieHeader)
            {
                logger.Debug(string.Format("updating Cookies {0} => {1}", CookieHeader, newCookieHeader));
                CookieHeader = newCookieHeader;
                if (IsConfigured)
                    SaveConfig();
            }
        }

        private async Task DoFollowIfRedirect(WebResult incomingResponse, string referrer = null, string overrideRedirectUrl = null, string overrideCookies = null, bool accumulateCookies = false)
        {
            if (incomingResponse.IsRedirect)
            {
                var redirRequestCookies = "";
                if (accumulateCookies)
                {
                    redirRequestCookies = ResolveCookies((CookieHeader != "" ? CookieHeader + " " : "") + (overrideCookies != null ? overrideCookies : ""));
                }
                else
                {
                    redirRequestCookies = (overrideCookies != null ? overrideCookies : "");
                }
                // Do redirect
                var redirectedResponse = await webclient.GetResultAsync(new WebRequest
                {
                    Url = overrideRedirectUrl ?? incomingResponse.RedirectingTo,
                    Referer = referrer,
                    Cookies = redirRequestCookies,
                    Encoding = Encoding
                });
                MapperUtil.Mapper.Map(redirectedResponse, incomingResponse);
            }
        }

        protected List<string> GetAllTrackerCategories() =>
            TorznabCaps.Categories.GetTrackerCategories();

        protected void AddCategoryMapping(string trackerCategory, TorznabCategory newznabCategory, string trackerCategoryDesc = null) =>
            TorznabCaps.Categories.AddCategoryMapping(trackerCategory, newznabCategory, trackerCategoryDesc);

        // TODO: remove this method ?
        protected void AddCategoryMapping(int trackerCategory, TorznabCategory newznabCategory, string trackerCategoryDesc = null) =>
            AddCategoryMapping(trackerCategory.ToString(), newznabCategory, trackerCategoryDesc);

        // TODO: remove this method and use AddCategoryMapping instead. this method doesn't allow to create custom cats
        protected void AddMultiCategoryMapping(TorznabCategory newznabCategory, params int[] trackerCategories)
        {
            foreach (var trackerCat in trackerCategories)
                AddCategoryMapping(trackerCat, newznabCategory);
        }

        protected List<string> MapTorznabCapsToTrackers(TorznabQuery query, bool mapChildrenCatsToParent = false) =>
            TorznabCaps.Categories.MapTorznabCapsToTrackers(query, mapChildrenCatsToParent);

        protected ICollection<int> MapTrackerCatToNewznab(string input) =>
            TorznabCaps.Categories.MapTrackerCatToNewznab(input);

        protected ICollection<int> MapTrackerCatDescToNewznab(string input) =>
            TorznabCaps.Categories.MapTrackerCatDescToNewznab(input);

        private IEnumerable<ReleaseInfo> CleanLinks(IEnumerable<ReleaseInfo> releases)
        {
            if (string.IsNullOrEmpty(downloadUrlBase))
                return releases;
            foreach (var release in releases)
            {
                if (release.Link.ToString().StartsWith(downloadUrlBase, StringComparison.Ordinal))
                {
                    release.Link = new Uri(release.Link.ToString().Substring(downloadUrlBase.Length), UriKind.Relative);
                }
            }

            return releases;
        }

        public override async Task<IndexerResult> ResultsForQuery(TorznabQuery query, bool isMetaIndexer)
        {
            var result = await base.ResultsForQuery(query, isMetaIndexer);
            result.Releases = CleanLinks(result.Releases);
            return result;
        }

        protected virtual Uri UncleanLink(Uri link)
        {
            if (string.IsNullOrWhiteSpace(downloadUrlBase))
            {
                return link;
            }

            if (link.ToString().StartsWith(downloadUrlBase, StringComparison.Ordinal))
            {
                return link;
            }

            return new Uri(downloadUrlBase + link.ToString(), UriKind.RelativeOrAbsolute);
        }

        protected void OnParseError(string results, Exception ex)
        {
            var fileName = string.Format("Error on {0} for {1}.txt", DateTime.Now.ToString("yyyyMMddHHmmss"), Name);
            var spacing = string.Join("", Enumerable.Repeat(Environment.NewLine, 5));
            var fileContents = string.Format("{0}{1}{2}", ex, spacing, results);
            logger.Error(fileName + fileContents);
            throw new Exception("Parse error", ex);
        }

        public override TorznabCapabilities TorznabCaps { get; protected set; }

        protected WebClient webclient;
        protected readonly string downloadUrlBase = "";
    }

    public abstract class BaseCachingWebIndexer : BaseWebIndexer
    {
        protected BaseCachingWebIndexer(IIndexerConfigurationService configService, WebClient client, Logger logger,
                                        ConfigurationData configData, IProtectionService p, ICacheService cacheService,
                                        string downloadBase = null)
            : base(configService: configService, client: client, logger: logger, configData: configData, p: p, cacheService: cacheService, downloadBase: downloadBase)
        {
        }

        protected void CleanCache()
        {
            foreach (var expired in cache.Where(i => DateTime.Now - i.Created > cacheTime).ToList())
            {
                cache.Remove(expired);
            }
        }

        // TODO: remove this implementation and use gloal cache
        protected static List<CachedQueryResult> cache = new List<CachedQueryResult>();
        protected static readonly TimeSpan cacheTime = TimeSpan.FromMinutes(9);
    }
}
