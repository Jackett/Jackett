using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Newtonsoft.Json.Linq;
using NLog;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class Redacted : BaseWebIndexer
    {
        protected string APIUrl => SiteLink + "ajax.php";

        protected bool supportsCategories = true;
        protected string DownloadUrl => SiteLink + "ajax.php?action=download&usetoken=" + (useTokens ? "1" : "0") + "&id=";
        protected string DetailsUrl => SiteLink + "torrents.php?torrentid=";
        protected bool useTokens = false;
        private new ConfigurationDataAPIKey configData
        {
            get => (ConfigurationDataAPIKey)base.configData;
            set => base.configData = value;
        }

        public Redacted(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(id: "redacted",
                   name: "Redacted",
                   description: "A music tracker",
                   link: "https://redacted.ch/",
                   caps: new TorznabCapabilities
                   {
                       SupportedMusicSearchParamsList = new List<string> { "q", "album", "artist", "label", "year" }
                   },
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   configData: new ConfigurationDataAPIKey()
                  )
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";

            AddCategoryMapping(1, TorznabCatType.Audio, "Music");
            AddCategoryMapping(2, TorznabCatType.PC, "Applications");
            AddCategoryMapping(3, TorznabCatType.Books, "E-Books");
            AddCategoryMapping(4, TorznabCatType.AudioAudiobook, "Audiobooks");
            AddCategoryMapping(5, TorznabCatType.Movies, "E-Learning Videos");
            AddCategoryMapping(6, TorznabCatType.TV, "Comedy");
            AddCategoryMapping(7, TorznabCatType.Books, "Comics");

            var cookieHint = new ConfigurationData.DisplayItem(
                "<ol><li>Go to Redacted's site and open your account settings.</li><li>Go to <b>Access Settings</b> tab and copy the API Key.<li>Ensure that you've checked <b>Confirm API Key</b>.</li><li>Finally, click <b>Save Profile</b>.</li></ol>")
            {
                Name = ""
            };
            configData.AddDynamic("cookieHint", cookieHint);

            var useTokenItem = new ConfigurationData.BoolItem { Value = false };
            useTokenItem.Name = "Use Freeleech Tokens when available";
            configData.AddDynamic("usetoken", useTokenItem);
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            if (configData.Key.Value.Length != 41)
                throw new Exception("invalid API Key configured: expected length: 41, got " + configData.Key.Value.Length.ToString());

            try
            {
                var results = await PerformQuery(new TorznabQuery());
                if (!results.Any())
                {
                    throw new Exception("Found 0 results in the tracker");
                }

                IsConfigured = true;
                SaveConfig();
                return IndexerConfigurationStatus.Completed;
            }
            catch (Exception e)
            {
                IsConfigured = false;
                throw new Exception("Your API Key did not work: " + e.Message);
            }
            
        }

        protected string GetSearchTerm(TorznabQuery query) => query.GetQueryString();

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = GetSearchTerm(query);

            var searchUrl = APIUrl;
            var queryCollection = new NameValueCollection
            {
                { "action", "browse" },
                { "order_by", "time" },
                { "order_way", "desc" }
            };


            if (!string.IsNullOrWhiteSpace(searchString))
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

            if (supportsCategories)
            {
                foreach (var cat in MapTorznabCapsToTrackers(query))
                {
                    queryCollection.Add("filter_cat[" + cat + "]", "1");
                }
            }

            searchUrl += "?" + queryCollection.GetQueryString();
            var headers = new Dictionary<string, string>()
            {
                { "Authorization", configData.Key.Value }
            };

            var response = await RequestStringWithCookiesAndRetry(searchUrl, headers: headers);
            
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
                    Uri banner = null;
                    if (!string.IsNullOrEmpty(cover))
                        banner = new Uri(cover);
                    var release = new ReleaseInfo
                    {
                        PublishDate = groupTime,
                        Title = title.ToString(),
                        Description = description,
                        BannerUrl = banner
                    };


                    if (r["torrents"] is JArray)
                    {
                        foreach (JObject torrent in r["torrents"])
                        {
                            var release2 = (ReleaseInfo)release.Clone();
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

        protected bool ReleaseInfoPostParse(ReleaseInfo release, JObject torrent, JObject result) => true;

        private void FillReleaseInfoFromJson(ReleaseInfo release, JObject torrent)
        {
            var torrentId = torrent["torrentId"];

            var time = (string)torrent["time"];
            if (!string.IsNullOrEmpty(time))
            {
                release.PublishDate = DateTime.ParseExact(time + " +0000", "yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);
            }

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

        public override async Task<byte[]> Download(Uri link)
        {
            var headers = new Dictionary<string, string>()
            {
                { "Authorization", configData.Key.Value }
            };

            var response = await base.RequestBytesWithCookies(link.ToString(), headers: headers);
            var content = response.Content;

            // Check if we're out of FL tokens/torrent is to large
            // most gazelle trackers will simply return the torrent anyway but e.g. redacted will return an error
            var requestLink = link.ToString();
            if (content.Length >= 1
                && content[0] != 'd' // simple test for torrent vs HTML content
                && requestLink.Contains("usetoken=1"))
            {
                var html = Encoding.GetString(content);
                if (html.Contains("You do not have any freeleech tokens left.")
                    || html.Contains("You do not have enough freeleech tokens left.")
                    || html.Contains("This torrent is too large."))
                {
                    // download again with usetoken=0
                    var requestLinkNew = requestLink.Replace("usetoken=1", "usetoken=0");
                    response = await base.RequestBytesWithCookies(requestLinkNew, headers: headers);
                    content = response.Content;
                }
            }

            return content;
        }


    }
}
