using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Parser.Html;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers.Abstract
{
    public abstract class GazelleTracker : BaseWebIndexer
    {
        protected string LoginUrl { get { return SiteLink + "login.php"; } }
        protected string APIUrl { get { return SiteLink + "ajax.php"; } }
        protected string DownloadUrl { get { return SiteLink + "torrents.php?action=download&usetoken=" + (useTokens ? "1" : "0") + "&id="; } }
        protected string DetailsUrl { get { return SiteLink + "torrents.php?torrentid="; } }
        protected bool supportsFreeleechTokens;
        protected bool useTokens = false;

        new ConfigurationDataBasicLogin configData
        {
            get { return (ConfigurationDataBasicLogin)base.configData; }
            set { base.configData = value; }
        }

        public GazelleTracker(IIndexerConfigurationService configService, Utils.Clients.WebClient webClient, Logger logger, IProtectionService protectionService, string name, string desc, string link, bool supportsFreeleechTokens)
            : base(name: name,
                description: desc,
                link: link,
                caps: new TorznabCapabilities(),
                configService: configService,
                client: webClient,
                logger: logger,
                p: protectionService,
                configData: new ConfigurationDataBasicLogin())
        {
            Encoding = Encoding.UTF8;
            this.supportsFreeleechTokens = supportsFreeleechTokens;

            if (supportsFreeleechTokens)
            {
                var useTokenItem = new ConfigurationData.BoolItem { Value = false };
                useTokenItem.Name = "Use Freeleech Tokens when available";
                configData.AddDynamic("usetoken", useTokenItem);
            }
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var useTokenItem = (ConfigurationData.BoolItem)configData.GetDynamic("usetoken");
            if (useTokenItem != null)
            {
                useTokens = useTokenItem.Value;
            }

            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
            };

            var response = await RequestLoginAndFollowRedirect(LoginUrl, pairs, string.Empty, true, SiteLink);
            await ConfigureIfOK(response.Cookies, response.Content != null && response.Content.Contains("logout.php"), () =>
            {
                var loginResultParser = new HtmlParser();
                var loginResultDocument = loginResultParser.Parse(response.Content);
                var loginform = loginResultDocument.QuerySelector("#loginform");
                if (loginform == null)
                    throw new ExceptionWithConfigData(response.Content, configData);

                loginform.QuerySelector("table").Remove();
                var errorMessage = loginform.TextContent.Replace("\n\t", " ").Trim();
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();

            var searchUrl = APIUrl;
            var queryCollection = new NameValueCollection();

            queryCollection.Add("action", "browse");
            //queryCollection.Add("group_results", "0"); # results won't include all information
            queryCollection.Add("order_by", "time");
            queryCollection.Add("order_way", "desc");

            
            if (!string.IsNullOrWhiteSpace(query.ImdbID))
            {
                queryCollection.Add("cataloguenumber", query.ImdbID);
            }
            else if (!string.IsNullOrWhiteSpace(searchString))
            {
                queryCollection.Add("searchstr", searchString);
            }

            if (query.Artist != null)
                queryCollection.Add("artistname", query.Artist);

            if (query.Label != null)
                queryCollection.Add("recordlabel", query.Label);

            if (query.Year != null)
                queryCollection.Add("year", query.Year.ToString());

            if (query.Album != null)
                queryCollection.Add("groupname", query.Album);

            foreach (var cat in MapTorznabCapsToTrackers(query))
            {
                queryCollection.Add("filter_cat[" + cat + "]", "1");
            }

            searchUrl += "?" + queryCollection.GetQueryString();

            var response = await RequestStringWithCookiesAndRetry(searchUrl);

            if (response.IsRedirect || query.IsTest)
            {
                // re-login
                await ApplyConfiguration(null);
                response = await RequestStringWithCookiesAndRetry(searchUrl);
            }

            try
            {
                var json = JObject.Parse(response.Content);
                foreach (JObject r in json["response"]["results"])
                {
                    var groupTime = DateTimeUtil.UnixTimestampToDateTime(long.Parse((string)r["groupTime"]));
                    var groupName = WebUtility.HtmlDecode((string)r["groupName"]);
                    var artist = WebUtility.HtmlDecode((string)r["artist"]);
                    var cover = (string)r["cover"];
                    var tags = r["tags"].ToList();
                    var groupYear = (string)r["groupYear"];
                    var releaseType = (string)r["releaseType"];

                    var release = new ReleaseInfo();

                    release.PublishDate = groupTime;

                    if (!string.IsNullOrEmpty(cover))
                        release.BannerUrl = new Uri(cover);

                    release.Title = "";
                    if (!string.IsNullOrEmpty(artist))
                        release.Title += artist + " - ";
                    release.Title += groupName;
                    if (!string.IsNullOrEmpty(groupYear) && groupYear != "0")
                        release.Title += " [" + groupYear + "]";
                    if (!string.IsNullOrEmpty(releaseType) && releaseType != "Unknown")
                        release.Title += " [" + releaseType + "]";

                    release.Description = "";
                    if (tags != null && tags.Count > 0 && (string)tags[0] != "")
                        release.Description += "Tags: " + string.Join(", ", tags) + "\n";

                    if (r["torrents"] is JArray)
                    {
                        foreach (JObject torrent in r["torrents"])
                        {
                            ReleaseInfo release2 = (ReleaseInfo)release.Clone();
                            FillReleaseInfoFromJson(release2, torrent);
                            if (ReleaseInfoPostParse(release2, torrent, r))
                                releases.Add(release2);
                        }
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
                OnParseError(response.Content, ex);
            }

            return releases;
        }

        // hook to add/modify the parsed information, return false to exclude the torrent from the results
        protected virtual bool ReleaseInfoPostParse(ReleaseInfo release, JObject torrent, JObject result)
        {
            return true;
        }

        void FillReleaseInfoFromJson(ReleaseInfo release, JObject torrent)
        {
            var torrentId = torrent["torrentId"];

            var time = (string)torrent["time"];
            if (!string.IsNullOrEmpty(time)) {
                release.PublishDate = DateTime.ParseExact(time+" +0000", "yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);
            }

            var flags = new List<string>();

            var format = (string)torrent["format"];
            if (!string.IsNullOrEmpty(format))
                flags.Add(WebUtility.HtmlDecode(format));

            var encoding = (string)torrent["encoding"];
            if (!string.IsNullOrEmpty(encoding))
                flags.Add(encoding);

            if(torrent["hasLog"] != null && (bool)torrent["hasLog"])
            {
                var logScore = (string)torrent["logScore"];
                flags.Add("Log (" + logScore + "%)");
            }

            if (torrent["hasCue"] != null && (bool)torrent["hasCue"])
                flags.Add("Cue");

            var media = (string)torrent["media"];
            if (!string.IsNullOrEmpty(media))
                flags.Add(media);

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
            release.Comments = new Uri(DetailsUrl + torrentId);
            release.Guid = release.Comments;
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
            {
                release.DownloadVolumeFactor = 0;
            }
            var isPersonalFreeleech = (bool?)torrent["isPersonalFreeleech"];
            if (isPersonalFreeleech != null && isPersonalFreeleech == true)
            {
                release.DownloadVolumeFactor = 0;
            }
            if ((bool)torrent["isNeutralLeech"])
            {
                release.DownloadVolumeFactor = 0;
                release.UploadVolumeFactor = 0;
            }
        }
    }
}
