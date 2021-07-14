using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class Libble : BaseWebIndexer
    {
        private string LandingUrl => SiteLink + "login.php";
        private string LoginUrl => SiteLink + "login.php";
        private string SearchUrl => SiteLink + "torrents.php";
        private Dictionary<string, string> CategoryMappings = new Dictionary<string, string>{
            { "cats_music", "Music" },
            { "cats_libblemixtapes", "Libble Mixtapes" },
            { "cats_musicvideos", "Music Videos" }
        };
        class VolumeFactorTag
        {
            public double DownloadVolumeFactor { get; set; } = 1.0;
            public double UploadVolumeFactor { get; set; } = 1.0;
        }
        private Dictionary<string, VolumeFactorTag> VolumeTagMappings = new Dictionary<string, VolumeFactorTag>{
            { "Neutral!", new VolumeFactorTag
                {
                    DownloadVolumeFactor = 0,
                    UploadVolumeFactor = 0
                }
            },
            { "Freeleech!", new VolumeFactorTag
                {
                    DownloadVolumeFactor = 0,
                    UploadVolumeFactor = 1
                }
            }
        };

        private new ConfigurationDataBasicLogin configData
        {
            get => (ConfigurationDataBasicLogin)base.configData;
            set => base.configData = value;
        }

        public Libble(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "libble",
                   name: "Libble",
                   description: "Libble is a Private Torrent Tracker for MUSIC",
                   link: "https://libble.me/",
                   caps: new TorznabCapabilities
                   {
                       MusicSearchParams = new List<MusicSearchParam>
                       {
                           MusicSearchParam.Q, MusicSearchParam.Album, MusicSearchParam.Artist, MusicSearchParam.Label, MusicSearchParam.Year
                       }
                   },
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataBasicLogin())
        {
            Encoding = Encoding.UTF8;
            Language = "en-US";
            Type = "private";

            AddCategoryMapping(1, TorznabCatType.Audio, "Music");
            AddCategoryMapping(2, TorznabCatType.Audio, "Libble Mixtapes");
            AddCategoryMapping(7, TorznabCatType.AudioVideo, "Music Videos");
        }

        public override async Task<ConfigurationData> GetConfigurationForSetup()
        {
            await RequestWithCookiesAsync(LandingUrl);
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
            await ConfigureIfOK(result.Cookies, result.ContentString?.Contains("logout.php") == true,
                () =>
                {
                    var parser = new HtmlParser();
                    var dom = parser.ParseDocument(result.ContentString);
                    var warningNode = dom.QuerySelector("#loginform > .warning");
                    var errorMessage = warningNode?.TextContent.Trim().Replace("\n\t", " ");
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
            var searchUrl = SearchUrl;

            var searchParams = new Dictionary<string, string> { };
            var queryCollection = new NameValueCollection { };

            // Search String
            if (!string.IsNullOrWhiteSpace(query.ImdbID))
                queryCollection.Add("cataloguenumber", query.ImdbID);
            else if (!string.IsNullOrWhiteSpace(searchString))
                queryCollection.Add("searchstr", searchString);


            // Filter Categories
            if (query.HasSpecifiedCategories)
            {
                foreach (var cat in MapTorznabCapsToTrackers(query))
                {
                    queryCollection.Add("filter_cat[" + cat.ToString() + "]", "1");
                }
            }

            if (query.Artist != null)
                queryCollection.Add("artistname", query.Artist);

            if (query.Label != null)
                queryCollection.Add("recordlabel", query.Label);

            if (query.Year != null)
                queryCollection.Add("year", query.Year.ToString());

            if (query.Album != null)
                queryCollection.Add("groupname", query.Album);

            searchUrl += "?" + queryCollection.GetQueryString();

            var searchPage = await RequestWithCookiesAndRetryAsync(searchUrl, method: RequestType.POST, data: searchParams);
            // Occasionally the cookies become invalid, login again if that happens
            if (searchPage.IsRedirect)
            {
                await ApplyConfiguration(null);
                searchPage = await RequestWithCookiesAndRetryAsync(searchUrl, method: RequestType.POST, data: searchParams);
            }

            try
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(searchPage.ContentString);
                var albumRows = dom.QuerySelectorAll("table#torrent_table > tbody > tr:has(strong > a[href*=\"torrents.php?id=\"])");
                foreach (var row in albumRows)
                {
                    var releaseGroupRegex = new Regex(@"torrents\.php\?id=([0-9]+)");

                    var albumNameNode = row.QuerySelector("strong > a[href*=\"torrents.php?id=\"]");
                    var artistsNameNodes = row.QuerySelectorAll("strong > a[href*=\"artist.php?id=\"]");
                    var albumYearNode = albumNameNode.NextSibling;
                    var categoryNode = row.QuerySelector(".cats_col > div");
                    var thumbnailNode = row.QuerySelector(".thumbnail");

                    var releaseArtist = "Various Artists";
                    if (artistsNameNodes.Count() > 0)
                    {
                        var aristNames = new List<string>();
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
                        releaseThumbnailUri = new Uri(thumbnailNode.GetAttribute("title"));

                    ICollection<int> releaseNewznabCategory = null;
                    var categoriesSplit = categoryNode.ClassName.Split(' ');
                    foreach (var rawCategory in categoriesSplit)
                    {
                        if (CategoryMappings.ContainsKey(rawCategory))
                        {
                            var newznabCat = MapTrackerCatDescToNewznab(CategoryMappings[rawCategory]);
                            if (newznabCat.Count != 0)
                                releaseNewznabCategory = newznabCat;
                        }
                    }

                    var releaseRows = dom.QuerySelectorAll(String.Format(".group_torrent.groupid_{0}", releaseGroupId));

                    string lastEdition = null;
                    foreach (var releaseDetails in releaseRows)
                    {
                        var editionInfoDetails = releaseDetails.QuerySelector(".edition_info");

                        // Process as release details
                        if (editionInfoDetails != null)
                        {
                            lastEdition = editionInfoDetails.QuerySelector("strong").TextContent;
                        }
                        // Process as torrent
                        else
                        {
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

                            release.Link = new Uri(SiteLink + releaseDownloadDetails.GetAttribute("href"));
                            release.Guid = release.Link;
                            release.Details = new Uri(SiteLink + albumNameNode.GetAttribute("href"));

                            // Aug 31 2020, 15:50
                            try
                            {
                                release.PublishDate = DateTime.ParseExact(
                                    releaseDateDetails.GetAttribute("title").Trim(),
                                    "MMM dd yyyy, HH:mm",
                                    CultureInfo.InvariantCulture,
                                    DateTimeStyles.AssumeUniversal
                                );
                            }
                            catch (Exception)
                            {
                            }

                            release.Files = ParseUtil.CoerceInt(releaseFileCountDetails.TextContent.Trim());
                            release.Grabs = ParseUtil.CoerceInt(releaseGrabsDetails.TextContent.Trim());
                            release.Seeders = ParseUtil.CoerceInt(releaseSeedsCountDetails.TextContent.Trim());
                            release.Peers = release.Seeders + ParseUtil.CoerceInt(releasePeersCountDetails.TextContent.Trim());
                            release.Size = ReleaseInfo.GetBytes(releaseSizeDetails.TextContent.Trim());
                            release.Poster = releaseThumbnailUri;
                            release.Category = releaseNewznabCategory;
                            release.MinimumSeedTime = 259200; // 72 hours

                            // Attempt to find volume factor tag
                            release.DownloadVolumeFactor = 1;
                            release.UploadVolumeFactor = 1;
                            var releaseTags = releaseMediaType.Split('/').Select(tag => tag.Trim()).ToList();
                            for (var i = releaseTags.Count - 1; i >= 0; i--)
                            {
                                var releaseTag = releaseTags[i];
                                if (VolumeTagMappings.ContainsKey(releaseTag))
                                {
                                    var volumeFactor = VolumeTagMappings[releaseTag];
                                    release.DownloadVolumeFactor = volumeFactor.DownloadVolumeFactor;
                                    release.UploadVolumeFactor = volumeFactor.UploadVolumeFactor;
                                    releaseTags.RemoveAt(i);
                                }
                            }

                            // Set title (with volume factor tags stripped)
                            var releaseTagsString = string.Join(" / ", releaseTags);
                            release.Title = String.Format("{0} - {1} [{2}] {3}", releaseArtist, releaseAlbumName, releaseAlbumYear, releaseTagsString);

                            releases.Add(release);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnParseError(searchPage.ContentString, ex);
            }

            return releases;
        }
    }
}
