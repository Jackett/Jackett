using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Jackett.Common.Extensions;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class Libble : IndexerBase
    {
        public override string Id => "libble";
        public override string Name => "Libble";
        public override string Description => "Libble is a Private Torrent Tracker for MUSIC";
        public override string SiteLink { get; protected set; } = "https://libble.me/";
        public override string Language => "en-US";
        public override string Type => "private";

        public override bool SupportsPagination => true;

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

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

        private new ConfigurationDataBasicLoginWith2FA configData
        {
            get => (ConfigurationDataBasicLoginWith2FA)base.configData;
            set => base.configData = value;
        }

        public Libble(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataBasicLoginWith2FA())
        {
            configData.AddDynamic("freeleech", new BoolConfigurationItem("Search freeleech only") { Value = false });
            configData.AddDynamic("Account Inactivity", new DisplayInfoConfigurationItem("Account Inactivity", "Accounts are disabled for inactivity after twelve months. To prevent inactivity pruning, browse the site while being logged in. Seeding does not count as 'activity' (actively using site)."));
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                MusicSearchParams = new List<MusicSearchParam>
                {
                    MusicSearchParam.Q, MusicSearchParam.Album, MusicSearchParam.Artist, MusicSearchParam.Label, MusicSearchParam.Year, MusicSearchParam.Genre
                }
            };

            caps.Categories.AddCategoryMapping(1, TorznabCatType.Audio, "Music");
            caps.Categories.AddCategoryMapping(2, TorznabCatType.Audio, "Libble Mixtapes");
            caps.Categories.AddCategoryMapping(7, TorznabCatType.AudioVideo, "Music Videos");

            return caps;
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
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "code", configData.TwoFactorAuth.Value },
                { "keeplogged", "1" },
                { "login", "Login" }
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, SearchUrl, LandingUrl, true);
            await ConfigureIfOK(result.Cookies, result.ContentString?.Contains("logout.php") == true, () =>
            {
                var parser = new HtmlParser();
                using var dom = parser.ParseDocument(result.ContentString);
                var errorMessage = dom.QuerySelector("#loginform > .warning")?.TextContent.Trim();

                throw new Exception(errorMessage ?? "Login failed.");
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

            var queryCollection = new NameValueCollection
            {
                { "order_by", "time" },
                { "order_way", "desc" }
            };

            if (((BoolConfigurationItem)configData.GetDynamic("freeleech")).Value)
                queryCollection.Set("freetorrent", "1");

            // Search String
            if (!string.IsNullOrWhiteSpace(query.ImdbID))
                queryCollection.Set("cataloguenumber", query.ImdbID);
            else if (!string.IsNullOrWhiteSpace(searchString))
                queryCollection.Set("searchstr", searchString);

            // Filter Categories
            if (query.HasSpecifiedCategories)
                foreach (var cat in MapTorznabCapsToTrackers(query))
                    queryCollection.Set($"filter_cat[{cat}]", "1");

            if (query.Artist.IsNotNullOrWhiteSpace() && query.Artist != "VA")
                queryCollection.Set("artistname", query.Artist);

            if (query.Label.IsNotNullOrWhiteSpace())
                queryCollection.Set("recordlabel", query.Label);

            if (query.Year.HasValue)
                queryCollection.Set("year", query.Year.ToString());

            if (query.Album.IsNotNullOrWhiteSpace())
                queryCollection.Set("groupname", query.Album);

            if (query.IsGenreQuery)
            {
                queryCollection.Set("taglist", query.Genre);
                queryCollection.Set("tags_type", "0");
            }

            if (query.Limit > 0 && query.Offset > 0)
            {
                var page = query.Offset / query.Limit + 1;
                queryCollection.Add("page", page.ToString());
            }

            searchUrl += "?" + queryCollection.GetQueryString();

            var searchPage = await RequestWithCookiesAndRetryAsync(searchUrl);

            // Occasionally the cookies become invalid, login again if that happens
            if (searchPage.IsRedirect)
            {
                await ApplyConfiguration(null);
                searchPage = await RequestWithCookiesAndRetryAsync(searchUrl);
            }

            try
            {
                var parser = new HtmlParser();
                using var dom = parser.ParseDocument(searchPage.ContentString);

                var albumRows = dom.QuerySelectorAll("table#torrent_table > tbody > tr.group:has(strong > a[href*=\"torrents.php?id=\"])");
                foreach (var row in albumRows)
                {
                    var releaseGroupRegex = new Regex(@"torrents\.php\?id=([0-9]+)");
                    var releaseYearRegex = new Regex(@"\[(\d{4})\]$");

                    var albumNameNode = row.QuerySelector("strong > a[href^=\"torrents.php?id=\"]");
                    var artistsNameNodes = row.QuerySelectorAll("strong > a[href*=\"artist.php?id=\"]");
                    var albumYearNode = row.QuerySelector("strong:has(a[href*=\"torrents.php?id=\"])");
                    var categoryNode = row.QuerySelector(".cats_col > div");
                    var thumbnailNode = row.QuerySelector(".thumbnail");

                    var releaseGenres = new List<string>();
                    var releaseDescription = "";
                    var genres = row.QuerySelector("div.tags")?.TextContent;
                    if (!string.IsNullOrEmpty(genres))
                    {
                        releaseDescription = genres.Trim().Replace(", ", ",");
                        releaseGenres = releaseGenres.Union(releaseDescription.Split(',')).ToList();
                    }

                    var releaseArtist = "Various Artists";
                    if (artistsNameNodes.Any())
                        releaseArtist = string.Join(", ", artistsNameNodes.Select(artist => artist.TextContent.Trim()).ToList());

                    var releaseAlbumName = albumNameNode.TextContent.Trim();
                    var releaseGroupId = ParseUtil.CoerceInt(releaseGroupRegex.Match(albumNameNode.GetAttribute("href")).Groups[1].ToString());
                    var releaseAlbumYear = releaseYearRegex.Match(albumYearNode.TextContent);

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

                    var releaseRows = dom.QuerySelectorAll($"table#torrent_table > tbody > tr.group_torrent.groupid_{releaseGroupId}:has(a[href*=\"torrents.php?id=\"])");
                    foreach (var releaseDetails in releaseRows)
                    {
                        // https://libble.me/torrents.php?id=51694&torrentid=89758
                        var release = new ReleaseInfo();

                        var releaseMediaDetails = releaseDetails.Children[0].Children[1];
                        var releaseMediaType = releaseMediaDetails.TextContent;

                        release.Link = new Uri(SiteLink + releaseDetails.QuerySelector("a[href^=\"torrents.php?action=download&id=\"]").GetAttribute("href"));
                        release.Guid = release.Link;
                        release.Details = new Uri(SiteLink + albumNameNode.GetAttribute("href"));

                        // Aug 31 2020, 15:50
                        try
                        {
                            var dateAdded = releaseDetails.QuerySelector("td:nth-child(3) > span[title]").GetAttribute("title").Trim();
                            release.PublishDate = DateTime.ParseExact(dateAdded, "MMM dd yyyy, HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
                        }
                        catch (Exception)
                        {
                            release.PublishDate = DateTimeUtil.FromTimeAgo(releaseDetails.QuerySelector("td:nth-child(3)")?.TextContent.Trim());
                        }

                        release.Files = ParseUtil.CoerceInt(releaseDetails.QuerySelector("td:nth-child(2)").TextContent);
                        release.Grabs = ParseUtil.CoerceInt(releaseDetails.QuerySelector("td:nth-child(5)").TextContent);
                        release.Seeders = ParseUtil.CoerceInt(releaseDetails.QuerySelector("td:nth-child(6)").TextContent);
                        release.Peers = release.Seeders + ParseUtil.CoerceInt(releaseDetails.QuerySelector("td:nth-child(7)").TextContent);
                        release.Size = ParseUtil.GetBytes(releaseDetails.QuerySelector("td:nth-child(4)").TextContent.Trim());
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
                        release.Title = $"{releaseArtist} - {releaseAlbumName} {releaseAlbumYear} {releaseTagsString}".Trim(' ', '-');

                        release.Description = releaseDescription;
                        release.Genres = releaseGenres;

                        releases.Add(release);
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
