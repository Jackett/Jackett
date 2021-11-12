using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers.Abstract
{
    [ExcludeFromCodeCoverage]
    public abstract class GazelleTracker : BaseWebIndexer
    {
        protected virtual string LoginUrl => SiteLink + "login.php";
        protected virtual string APIUrl => SiteLink + "ajax.php";
        protected virtual string DownloadUrl => SiteLink + "torrents.php?action=download&usetoken=" + (useTokens ? "1" : "0") + (usePassKey ? "&torrent_pass=" + configData.PassKey.Value : "") + "&id=";
        protected virtual string DetailsUrl => SiteLink + "torrents.php?torrentid=";
        protected virtual string PosterUrl => SiteLink;
        protected virtual string AuthorizationFormat => "{0}";
        protected virtual int ApiKeyLength => 41;
        protected virtual string FlipOptionalTokenString(string requestLink) => requestLink.Replace("usetoken=1", "usetoken=0");

        protected bool useTokens;
        protected string cookie = "";

        private readonly bool imdbInTags;
        private readonly bool useApiKey;
        private readonly bool usePassKey;

        private new ConfigurationDataGazelleTracker configData => (ConfigurationDataGazelleTracker)base.configData;

        protected GazelleTracker(string link, string id, string name, string description,
                                 IIndexerConfigurationService configService, WebClient client, Logger logger,
                                 IProtectionService p, ICacheService cs, TorznabCapabilities caps,
                                 bool supportsFreeleechTokens, bool imdbInTags = false, bool has2Fa = false,
                                 bool useApiKey = false, bool usePassKey = false, string instructionMessageOptional = null)
            : base(id: id,
                   name: name,
                   description: description,
                   link: link,
                   caps: caps,
                   configService: configService,
                   client: client,
                   logger: logger,
                   p: p,
                   cacheService: cs,
                   configData: new ConfigurationDataGazelleTracker(
                       has2Fa, supportsFreeleechTokens, useApiKey, usePassKey, instructionMessageOptional))
        {
            Encoding = Encoding.UTF8;

            this.imdbInTags = imdbInTags;
            this.useApiKey = useApiKey;
            this.usePassKey = usePassKey;
        }

        public override void LoadValuesFromJson(JToken jsonConfig, bool useProtectionService = false)
        {
            base.LoadValuesFromJson(jsonConfig, useProtectionService);

            var cookieItem = configData.CookieItem;
            if (cookieItem != null)
                cookie = cookieItem.Value;

            var useTokenItem = configData.UseTokenItem;
            if (useTokenItem != null)
                useTokens = useTokenItem.Value;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            if (useApiKey)
            {
                var apiKey = configData.ApiKey;
                if (apiKey?.Value == null)
                    throw new Exception("Invalid API Key configured");
                if (apiKey.Value.Length != ApiKeyLength)
                    throw new Exception($"Invalid API Key configured: expected length: {ApiKeyLength}, got {apiKey.Value.Length}");

                try
                {
                    var results = await PerformQuery(new TorznabQuery());
                    if (!results.Any())
                        throw new Exception("Found 0 results in the tracker");

                    IsConfigured = true;
                    SaveConfig();
                    return IndexerConfigurationStatus.Completed;
                }
                catch (Exception e)
                {
                    IsConfigured = false;
                    throw new Exception($"Your API Key did not work: {e.Message}");
                }
            }

            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "keeplogged", "1"}
            };

            if (!string.IsNullOrWhiteSpace(cookie))
            {
                // Cookie was manually supplied
                CookieHeader = cookie;
                try
                {
                    var results = await PerformQuery(new TorznabQuery());
                    if (!results.Any())
                        throw new Exception("Found 0 results in the tracker");

                    IsConfigured = true;
                    SaveConfig();
                    return IndexerConfigurationStatus.Completed;
                }
                catch (Exception e)
                {
                    IsConfigured = false;
                    throw new Exception($"Your cookie did not work: {e.Message}");
                }
            }

            var response = await RequestLoginAndFollowRedirect(LoginUrl, pairs, string.Empty, true, SiteLink);
            await ConfigureIfOK(response.Cookies, response.ContentString != null && response.ContentString.Contains("logout.php"), () =>
            {
                var loginResultParser = new HtmlParser();
                var loginResultDocument = loginResultParser.ParseDocument(response.ContentString);
                var loginform = loginResultDocument.QuerySelector("#loginform");
                if (loginform == null)
                    throw new ExceptionWithConfigData(response.ContentString, configData);

                loginform.QuerySelector("table").Remove();
                var errorMessage = loginform.TextContent.Replace("\n\t", " ").Trim();
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        // hook to adjust the search term
        protected virtual string GetSearchTerm(TorznabQuery query) => query.GetQueryString();

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = GetSearchTerm(query);

            var searchUrl = APIUrl;
            var queryCollection = new NameValueCollection
            {
                { "action", "browse" },
                //{"group_results", "0"}, # results won't include all information
                { "order_by", "time" },
                { "order_way", "desc" }
            };

            if (!string.IsNullOrWhiteSpace(query.ImdbID))
            {
                if (imdbInTags)
                    queryCollection.Add("taglist", query.ImdbID);
                else
                    queryCollection.Add("cataloguenumber", query.ImdbID);
            }
            else if (!string.IsNullOrWhiteSpace(searchString))
                queryCollection.Add("searchstr", searchString);

            if (query.Artist != null)
                queryCollection.Add("artistname", query.Artist);

            if (query.Label != null)
                queryCollection.Add("recordlabel", query.Label);

            if (query.Year != null)
                queryCollection.Add("year", query.Year.ToString());

            if (query.Album != null)
                queryCollection.Add("groupname", query.Album);

            foreach (var cat in MapTorznabCapsToTrackers(query))
                queryCollection.Add("filter_cat[" + cat + "]", "1");

            searchUrl += "?" + queryCollection.GetQueryString();

            var apiKey = configData.ApiKey;
            var headers = apiKey != null ? new Dictionary<string, string> { ["Authorization"] = String.Format(AuthorizationFormat, apiKey.Value) } : null;

            var response = await RequestWithCookiesAndRetryAsync(searchUrl, headers: headers);
            // we get a redirect in html pages and an error message in json response (api)
            if (response.IsRedirect && !useApiKey)
            {
                // re-login only if API key is not in use.
                await ApplyConfiguration(null);
                response = await RequestWithCookiesAndRetryAsync(searchUrl);
            }
            else if (response.ContentString != null && response.ContentString.Contains("failure") && useApiKey)
            {
                // reason for failure should be explained.
                var jsonError = JObject.Parse(response.ContentString);
                var errorReason = (string)jsonError["error"];
                throw new Exception(errorReason);
            }


            try
            {
                var json = JObject.Parse(response.ContentString);
                foreach (JObject r in json["response"]["results"])
                {
                    var groupTime = DateTimeUtil.UnixTimestampToDateTime(long.Parse((string)r["groupTime"]));
                    var groupName = WebUtility.HtmlDecode((string)r["groupName"]);
                    var artist = WebUtility.HtmlDecode((string)r["artist"]);
                    var cover = (string)r["cover"];
                    var tags = r["tags"].ToList();
                    var groupYear = (string)r["groupYear"];
                    var releaseType = (string)r["releaseType"];
                    var title = new StringBuilder();
                    if (!string.IsNullOrEmpty(artist))
                        title.Append(artist + " - ");
                    title.Append(groupName);
                    if (!string.IsNullOrEmpty(groupYear) && groupYear != "0")
                        title.Append(" [" + groupYear + "]");
                    if (!string.IsNullOrEmpty(releaseType) && releaseType != "Unknown")
                        title.Append(" [" + releaseType + "]");
                    var description = tags?.Any() == true && !string.IsNullOrEmpty(tags[0].ToString())
                        ? "Tags: " + string.Join(", ", tags) + "\n"
                        : null;
                    Uri poster = null;
                    if (!string.IsNullOrEmpty(cover))
                        poster = (cover.StartsWith("http")) ? new Uri(cover) : new Uri(PosterUrl + cover);
                    var release = new ReleaseInfo
                    {
                        PublishDate = groupTime,
                        Title = title.ToString(),
                        Description = description,
                        Poster = poster
                    };


                    if (imdbInTags)
                        release.Imdb = tags
                                       .Select(tag => ParseUtil.GetImdbID((string)tag))
                                       .Where(tag => tag != null).FirstIfSingleOrDefault();

                    if (r["torrents"] is JArray)
                        foreach (JObject torrent in r["torrents"])
                        {
                            var release2 = (ReleaseInfo)release.Clone();
                            FillReleaseInfoFromJson(release2, torrent);
                            if (ReleaseInfoPostParse(release2, torrent, r))
                                releases.Add(release2);
                        }
                    else
                    {
                        FillReleaseInfoFromJson(release, r);
                        if (ReleaseInfoPostParse(release, r, r))
                            releases.Add(release);
                    }
                }
            }
            catch (Exception ex)
            {
                OnParseError(response.ContentString, ex);
            }

            return releases;
        }

        // hook to add/modify the parsed information, return false to exclude the torrent from the results
        protected virtual bool ReleaseInfoPostParse(ReleaseInfo release, JObject torrent, JObject result) => true;

        private void FillReleaseInfoFromJson(ReleaseInfo release, JObject torrent)
        {
            var torrentId = torrent["torrentId"];

            var time = (string)torrent["time"];
            if (!string.IsNullOrEmpty(time))
                release.PublishDate = DateTime.ParseExact(time + " +0000", "yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);

            var flags = new List<string>();

            var format = (string)torrent["format"];
            if (!string.IsNullOrEmpty(format))
                flags.Add(WebUtility.HtmlDecode(format));

            var encoding = (string)torrent["encoding"];
            if (!string.IsNullOrEmpty(encoding))
                flags.Add(encoding);

            if (torrent["hasLog"] != null && (bool)torrent["hasLog"])
            {
                var logScore = (string)torrent["logScore"];
                flags.Add("Log (" + logScore + "%)");
            }

            if (torrent["hasCue"] != null && (bool)torrent["hasCue"])
                flags.Add("Cue");

            // tehconnection.me specific?
            var lang = (string)torrent["lang"];
            if (!string.IsNullOrEmpty(lang) && lang != "---")
                flags.Add(lang);

            var media = (string)torrent["media"];
            if (!string.IsNullOrEmpty(media))
                flags.Add(media);

            // tehconnection.me specific?
            var resolution = (string)torrent["resolution"];
            if (!string.IsNullOrEmpty(resolution))
                flags.Add(resolution);

            // tehconnection.me specific?
            var container = (string)torrent["container"];
            if (!string.IsNullOrEmpty(container))
                flags.Add(container);

            // tehconnection.me specific?
            var codec = (string)torrent["codec"];
            if (!string.IsNullOrEmpty(codec))
                flags.Add(codec);

            // tehconnection.me specific?
            var audio = (string)torrent["audio"];
            if (!string.IsNullOrEmpty(audio))
                flags.Add(audio);

            // tehconnection.me specific?
            var subbing = (string)torrent["subbing"];
            if (!string.IsNullOrEmpty(subbing) && subbing != "---")
                flags.Add(subbing);

            if (torrent["remastered"] != null && (bool)torrent["remastered"])
            {
                var remasterYear = (string)torrent["remasterYear"];
                var remasterTitle = WebUtility.HtmlDecode((string)torrent["remasterTitle"]);
                flags.Add(remasterYear + (!string.IsNullOrEmpty(remasterTitle) ? " " + remasterTitle : ""));
            }

            if (flags.Count > 0)
                release.Title += " " + string.Join(" / ", flags);

            release.Size = (long)torrent["size"];
            release.Seeders = (int)torrent["seeders"];
            release.Peers = (int)torrent["leechers"] + release.Seeders;
            release.Details = new Uri(DetailsUrl + torrentId);
            release.Guid = release.Details;
            release.Link = new Uri(DownloadUrl + torrentId);
            var category = (string)torrent["category"];
            if (category == null || category.Contains("Select Category"))
                release.Category = MapTrackerCatToNewznab("1");
            else
                release.Category = MapTrackerCatDescToNewznab(category);
            release.Files = (int)torrent["fileCount"];
            release.Grabs = (int)torrent["snatches"];
            release.DownloadVolumeFactor = 1;
            release.UploadVolumeFactor = 1;
            if ((bool)torrent["isFreeleech"])
                release.DownloadVolumeFactor = 0;
            var isPersonalFreeleech = (bool?)torrent["isPersonalFreeleech"];
            if (isPersonalFreeleech != null && isPersonalFreeleech == true)
                release.DownloadVolumeFactor = 0;
            if ((bool)torrent["isNeutralLeech"])
            {
                release.DownloadVolumeFactor = 0;
                release.UploadVolumeFactor = 0;
            }
        }

        public override async Task<byte[]> Download(Uri link)
        {
            var apiKey = configData.ApiKey;
            var headers = apiKey != null ? new Dictionary<string, string> { ["Authorization"] = String.Format(AuthorizationFormat, apiKey.Value) } : null;
            var response = await base.RequestWithCookiesAsync(link.ToString(), null, RequestType.GET, headers: headers);
            var content = response.ContentBytes;

            // Check if we're out of FL tokens/torrent is to large
            // most gazelle trackers will simply return the torrent anyway but e.g. redacted will return an error
            var requestLink = link.ToString();
            if (content.Length >= 1
                && content[0] != 'd' // simple test for torrent vs HTML content
                && requestLink.Contains("usetoken=1"))
            {
                var html = Encoding.GetString(content);
                if (html.Contains("You do not have any freeleech tokens left.")
                    || html.Contains("You do not have enough freeleech tokens")
                    || html.Contains("This torrent is too large.")
                    || html.Contains("You cannot use tokens here"))
                {
                    // download again with usetoken=0
                    var requestLinkNew = FlipOptionalTokenString(requestLink);
                    content = await base.Download(new Uri(requestLinkNew), RequestType.GET, headers: headers);
                }
            }

            return content;
        }
    }
}
