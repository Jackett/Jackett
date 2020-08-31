using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class Libble : BaseWebIndexer
    {
        private string LandingUrl => SiteLink + "login.php";
        private string LoginUrl => SiteLink + "login.php";
        private string SearchUrl => SiteLink + "torrents.php";

       private new ConfigurationDataBasicLogin configData
        {
            get => (ConfigurationDataBasicLogin)base.configData;
            set => base.configData = value;
        }

        public Libble(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(id: "libble",
                   name: "Libble",
                   description: "Libble is a Private Torrent Tracker for MUSIC",
                   link: "https://libble.me/",
                   caps: new TorznabCapabilities
                   {
                       TVSearchAvailable = false
                   },
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   configData: new ConfigurationDataBasicLogin())
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";

            AddCategoryMapping(13, TorznabCatType.Audio, "Music");
            AddCategoryMapping(15, TorznabCatType.AudioVideo, "Music Videos");

            // RSS Textual categories
            AddCategoryMapping("cats_music", TorznabCatType.Audio);
            AddCategoryMapping("cats_libblemixtapes", TorznabCatType.Audio);
            AddCategoryMapping("cats_musicvideos", TorznabCatType.AudioVideo);
        }

        public override async Task<ConfigurationData> GetConfigurationForSetup()
        {
            await RequestStringWithCookies(LandingUrl);
            return configData;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string>
                        {
                            {"username", configData.Username.Value},
                            {"password", configData.Password.Value},
                            {"login", "Login"}
                        };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, SearchUrl, LandingUrl, true);
            await ConfigureIfOK(result.Cookies, result.Content?.Contains("logout.php") == true,
                () =>
                {
                    // TODO: <span class="warning">Your username, password or code was incorrect.<br><br></span>
                    var parser = new HtmlParser();
                    var dom = parser.ParseDocument(result.Content);
                    var errorMessage = dom.QuerySelector("class:")?.TextContent.Trim().Replace("\n\t", " ");
                    if (string.IsNullOrWhiteSpace(errorMessage))
                        errorMessage = dom.QuerySelector("div.notification-body").TextContent.Trim().Replace("\n\t", " ");
                    throw new ExceptionWithConfigData(errorMessage, configData);
                });


            SaveConfig();

            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            // https://libble.me/torrents.php?searchstr=the+used&taglist=&tags_type=1&order_by=time&order_way=desc
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();
            var prevCook = CookieHeader + "";
            var searchUrl = SearchUrl;

            var searchParams = new Dictionary<string, string>{};

            // Skip search params if its a test, ask for all torrents
            if (!query.IsTest)
            {
                string[] searchParamParts = searchString.Split(' ');
                string searchStringCombined = "";
                foreach (var part in searchParamParts) {
                    searchStringCombined += part + "+";
                }

               searchUrl += "?searchstr=" + searchStringCombined;
            }


            var searchPage = await PostDataWithCookiesAndRetry(searchUrl, searchParams, CookieHeader);
            // Occasionally the cookies become invalid, login again if that happens
            if (searchPage.IsRedirect)
            {
                await ApplyConfiguration(null);
                searchPage = await PostDataWithCookiesAndRetry(searchUrl, searchParams, CookieHeader);
            }

            try
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(searchPage.Content);
                var albumRows = dom.QuerySelectorAll("table#torrent_table > tbody > tr:has(strong > a[href*=\"torrents.php?id=\"])");
                foreach (var row in albumRows)
                {
                    Regex releaseGroupRegex = new Regex(@"torrents\.php\?id=([0-9]+)");

                    var albumNameNode = row.QuerySelector("strong > a[href*=\"torrents.php?id=\"]");
                    var artistsNameNodes = row.QuerySelectorAll("strong > a[href*=\"artist.php?id=\"]");
                    var albumYearNode = albumNameNode.NextSibling;
                    var categoryNode = row.QuerySelector(".cats_col > div");
                    var thumbnailNode = row.QuerySelector(".thumbnail");

                    var releaseArtist = "Various Artists";
                    if (artistsNameNodes.Count() > 0) 
                    {
                        List<string> aristNames = new List<string>();
                        foreach (var aristNode in artistsNameNodes) 
                        {
                            aristNames.Add(aristNode.TextContent.Trim());
                        }
                        releaseArtist = string.Join(", ", aristNames);
                    }

                    var releaseAlbumName = albumNameNode.TextContent.Trim();
                    var releaseGroupId = ParseUtil.CoerceInt(releaseGroupRegex.Match(albumNameNode.GetAttribute("href")).Groups[1].ToString());
                    var releaseAlbumYear = ParseUtil.CoerceInt(albumYearNode.TextContent.Replace("[", "").Replace("]", "").Trim());

                    Uri releaseThumbnailUri = null;
                    if (thumbnailNode != null) 
                    {
                        releaseThumbnailUri = new Uri(thumbnailNode.GetAttribute("title"));
                    }

                    ICollection<int> releaseNewznabCategory = null;
                    var categoriesSplit = categoryNode.ClassName.Split(' ');
                    foreach (var rawCategory in categoriesSplit) 
                    {
                        var newznabCat = MapTrackerCatToNewznab(rawCategory);
                        if (newznabCat.Count != 0) {
                            releaseNewznabCategory = newznabCat;
                        }
                    }
                    
                    var releaseRows = dom.QuerySelectorAll(String.Format(".group_torrent.groupid_{0}", releaseGroupId));

                    string lastEdition = null;
                    foreach (var releaseDetails in releaseRows) {
                        var editionInfoDetails = releaseDetails.QuerySelector(".edition_info");

                        // Process as release details
                        if (editionInfoDetails != null) {
                            lastEdition = editionInfoDetails.QuerySelector("strong").TextContent;
                        }
                        // Process as torrent
                        else if (lastEdition != null) {
                            // https://libble.me/torrents.php?id=51694&torrentid=89758
                            var release = new ReleaseInfo();

                            var releaseMediaDetails = releaseDetails.Children[0].Children[1];
                            var releaseFileCountDetails = releaseDetails.Children[1];
                            var releaseDateDetails = releaseDetails.Children[2].Children[0];
                            var releaseSizeDetails = releaseDetails.Children[3];
                            var releaseGrabsDetails = releaseDetails.Children[4];
                            var releaseSeedsCountDetails = releaseDetails.Children[5];
                            var releasePeersCountDetails = releaseDetails.Children[6];
                            var releaseDownloadDetails = releaseDetails.QuerySelector("a[href*=\"action=download\"]");
                            var releaseMediaType = releaseMediaDetails.TextContent;

                            release.Title = String.Format("{0} - {1} [{2}] {3}", releaseArtist, releaseAlbumName, releaseAlbumYear, releaseMediaType);
                            release.Link = new Uri(SiteLink + releaseDownloadDetails.GetAttribute("href"));
                            release.Guid = release.Link;
                            release.Comments = new Uri(SiteLink + albumNameNode.GetAttribute("href"));
                            
                            // Aug 31 2020, 15:50
                            try {
                                release.PublishDate = DateTime.ParseExact(
                                    releaseDateDetails.GetAttribute("title").Trim(),
                                    "MMM dd yyyy, HH:mm",
                                    CultureInfo.InvariantCulture,
                                    DateTimeStyles.AssumeUniversal
                                );
                            }
                            catch(Exception ex) {
                            }

                            release.Files = ParseUtil.CoerceInt(releaseFileCountDetails.TextContent.Trim());
                            release.Grabs = ParseUtil.CoerceInt(releaseGrabsDetails.TextContent.Trim());
                            release.Seeders = ParseUtil.CoerceInt(releaseSeedsCountDetails.TextContent.Trim());
                            release.Peers = release.Seeders + ParseUtil.CoerceInt(releasePeersCountDetails.TextContent.Trim());
                            release.Size = ReleaseInfo.GetBytes(releaseSizeDetails.TextContent.Trim());
                            release.BannerUrl = releaseThumbnailUri;
                            release.Category = releaseNewznabCategory;

                            // TODO: Neutral / Freeleech

                            releases.Add(release);
                        }
                        else {
                            // TODO: Error, no release edition
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnParseError(searchPage.Content, ex);
            }

            if (!CookieHeader.Trim().Equals(prevCook.Trim()))
                SaveConfig();

            return releases;
        }
    }
}
