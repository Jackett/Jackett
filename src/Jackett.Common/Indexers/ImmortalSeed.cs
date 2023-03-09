using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Newtonsoft.Json.Linq;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class ImmortalSeed : IndexerBase
    {
        public override string Id => "immortalseed";
        public override string Name => "ImmortalSeed";
        public override string Description => "ImmortalSeed (iS) is a Private Torrent Tracker for MOVIES / TV / GENERAL";
        public override string SiteLink { get; protected set; } = "https://immortalseed.me/";
        public override string[] LegacySiteLinks => new[]
        {
            "http://immortalseed.me/"
        };
        public override string Language => "en-US";
        public override string Type => "private";

        private string SearchUrl => SiteLink + "browse.php";
        private string LoginUrl => SiteLink + "takelogin.php";
        private readonly Regex _dateMatchRegex = new Regex(@"\d{4}-\d{2}-\d{2} \d{2}:\d{2} [AaPp][Mm]", RegexOptions.Compiled);

        private new ConfigurationDataBasicLogin configData
        {
            get => (ConfigurationDataBasicLogin)base.configData;
            set => base.configData = value;
        }

        public ImmortalSeed(IIndexerConfigurationService configService, Utils.Clients.WebClient wc, Logger l,
            IProtectionService ps, ICacheService cs)
            : base(
                   caps: new TorznabCapabilities
                   {
                       TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                       },
                       MovieSearchParams = new List<MovieSearchParam>
                       {
                           MovieSearchParam.Q
                       },
                       MusicSearchParams = new List<MusicSearchParam>
                       {
                           MusicSearchParam.Q
                       },
                       BookSearchParams = new List<BookSearchParam>
                       {
                           BookSearchParam.Q
                       }
                   },
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataBasicLogin())
        {
            AddCategoryMapping(3, TorznabCatType.Other, "Nuked");
            AddCategoryMapping(32, TorznabCatType.TVAnime, "Anime");
            AddCategoryMapping(23, TorznabCatType.PC, "Apps");
            AddCategoryMapping(35, TorznabCatType.AudioAudiobook, "Audiobooks");
            AddCategoryMapping(31, TorznabCatType.TV, "Childrens/Cartoons");
            AddCategoryMapping(54, TorznabCatType.TVDocumentary, "Documentary - HD");
            AddCategoryMapping(53, TorznabCatType.TVDocumentary, "Documentary - SD");
            AddCategoryMapping(22, TorznabCatType.BooksEBook, "Ebooks");
            AddCategoryMapping(41, TorznabCatType.BooksComics, "Comics");
            AddCategoryMapping(46, TorznabCatType.BooksMags, "Magazines");
            AddCategoryMapping(25, TorznabCatType.PCGames, "Games");
            AddCategoryMapping(61, TorznabCatType.ConsoleNDS, "Games Nintendo");
            AddCategoryMapping(26, TorznabCatType.PCGames, "Games-PC ISO");
            AddCategoryMapping(28, TorznabCatType.ConsolePS4, "Games-PSx");
            AddCategoryMapping(29, TorznabCatType.ConsoleXBox, "Games Xbox");
            AddCategoryMapping(49, TorznabCatType.PCMobileOther, "Mobile");
            AddCategoryMapping(51, TorznabCatType.PCMobileAndroid, "Android");
            AddCategoryMapping(50, TorznabCatType.PCMobileiOS, "IOS");
            AddCategoryMapping(52, TorznabCatType.PC0day, "Windows");
            AddCategoryMapping(59, TorznabCatType.MoviesUHD, "Movies-4k");
            AddCategoryMapping(60, TorznabCatType.MoviesForeign, "Non-English 4k Movies");
            AddCategoryMapping(16, TorznabCatType.MoviesHD, "Movies HD");
            AddCategoryMapping(18, TorznabCatType.MoviesForeign, "Movies HD Non-English");
            AddCategoryMapping(17, TorznabCatType.MoviesSD, "TS/CAM/PPV");
            AddCategoryMapping(34, TorznabCatType.MoviesForeign, "Movies Low Def Non-English");
            AddCategoryMapping(62, TorznabCatType.Movies, "Movies-Packs");
            AddCategoryMapping(14, TorznabCatType.MoviesSD, "Movies-SD");
            AddCategoryMapping(33, TorznabCatType.MoviesForeign, "Movies SD Non-English");
            AddCategoryMapping(30, TorznabCatType.AudioOther, "Music");
            AddCategoryMapping(37, TorznabCatType.AudioLossless, "FLAC");
            AddCategoryMapping(36, TorznabCatType.AudioMP3, "MP3");
            AddCategoryMapping(39, TorznabCatType.AudioOther, "Music Other");
            AddCategoryMapping(38, TorznabCatType.AudioVideo, "Music Video");
            AddCategoryMapping(45, TorznabCatType.Other, "Other");
            AddCategoryMapping(7, TorznabCatType.TVSport, "Sports Tv");
            AddCategoryMapping(44, TorznabCatType.TVSport, "Sports Fitness-Instructional");
            AddCategoryMapping(58, TorznabCatType.TVSport, "Olympics");
            AddCategoryMapping(47, TorznabCatType.TVSD, "TV - 480p");
            AddCategoryMapping(64, TorznabCatType.TVUHD, "TV - 4K");
            AddCategoryMapping(8, TorznabCatType.TVHD, "TV - High Definition");
            AddCategoryMapping(48, TorznabCatType.TVSD, "TV - Standard Definition - x264");
            AddCategoryMapping(9, TorznabCatType.TVSD, "TV - Standard Definition - XviD");
            AddCategoryMapping(63, TorznabCatType.TVUHD, "TV Season Packs - 4K");
            AddCategoryMapping(4, TorznabCatType.TVHD, "TV Season Packs - HD");
            AddCategoryMapping(6, TorznabCatType.TVSD, "TV Season Packs - SD");

            // Configure the sort selects
            var sortBySelect = new SingleSelectConfigurationItem(
                "Sort by",
                new Dictionary<string, string>
                {
                    { "added", "created" },
                    { "seeders", "seeders" },
                    { "size", "size" },
                    { "name", "title" }
                })
            { Value = "added" };
            configData.AddDynamic("sortrequestedfromsite", sortBySelect);

            var orderSelect = new SingleSelectConfigurationItem(
                "Order",
                new Dictionary<string, string>
                {
                    { "desc", "descending" },
                    { "asc", "ascending" }
                })
            { Value = "desc" };
            configData.AddDynamic("orderrequestedfromsite", orderSelect);
        }

        private string GetSortBy => ((SingleSelectConfigurationItem)configData.GetDynamic("sortrequestedfromsite")).Value;

        private string GetOrder => ((SingleSelectConfigurationItem)configData.GetDynamic("orderrequestedfromsite")).Value;

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value }
            };

            var response = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, LoginUrl);

            await ConfigureIfOK(response.Cookies, response.ContentString.Contains("logout.php"), () =>
            {
                var parser = new HtmlParser();
                var document = parser.ParseDocument(response.ContentString);
                var errorMessage = document.QuerySelector("#main table td:contains(\"ERROR\")")?.TextContent.Trim();

                throw new ExceptionWithConfigData(errorMessage ?? "Login failed.", configData);
            });

            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var searchParams = new Dictionary<string, string>
            {
                { "category", "0" },
                { "include_dead_torrents", "yes" },
                { "sort", GetSortBy },
                { "order", GetOrder }
            };

            var searchString = Regex.Replace(query.GetQueryString(), @"[ -._]+", " ").Trim();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchParams.Add("do", "search");
                searchParams.Add("keywords", searchString);
                searchParams.Add("search_type", "t_name");
            }

            var categoryMapping = MapTorznabCapsToTrackers(query);
            if (categoryMapping.Any())
                searchParams.Add("selectedcats2", string.Join(",", categoryMapping));

            var searchUrl = $"{SearchUrl}?{searchParams.GetQueryString()}";

            var results = await RequestWithCookiesAndRetryAsync(searchUrl);

            // Occasionally the cookies become invalid, login again if that happens
            if (results.ContentString.Contains("You do not have permission to access this page."))
            {
                await ApplyConfiguration(null);
                results = await RequestWithCookiesAndRetryAsync(searchUrl);
            }

            var releases = new List<ReleaseInfo>();

            try
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(results.ContentString);

                var rows = dom.QuerySelectorAll("#sortabletable tr:has(a[href*=\"details.php?id=\"])");
                foreach (var row in rows)
                {
                    var release = new ReleaseInfo();

                    var qDetails = row.QuerySelector("div > a[href*=\"details.php?id=\"]"); // details link, release name get's shortened if it's to long
                    // use Title from tooltip or fallback to Details link if there's no tooltip
                    var qTitle = row.QuerySelector(".tooltip-content > div:nth-of-type(1)") ?? qDetails;
                    release.Title = qTitle.TextContent;

                    var qDesciption = row.QuerySelectorAll(".tooltip-content > div");
                    if (qDesciption.Any())
                    {
                        release.Description = qDesciption[1].TextContent.Replace("|", ",").Replace(" ", "").Trim();
                        if (release.Genres == null)
                            release.Genres = new List<string>();
                        release.Genres = release.Genres.Union(release.Description.Split(',')).ToList();
                    }

                    var qLink = row.QuerySelector("a[href*=\"download.php\"]");
                    release.Link = new Uri(qLink.GetAttribute("href"));
                    release.Guid = release.Link;
                    release.Details = new Uri(qDetails.GetAttribute("href"));

                    // 2021-03-17 03:39 AM
                    // requests can be 'Pre Release Time: 2013-04-22 02:00 AM Uploaded: 3 Years, 6 Months, 4 Weeks, 2 Days, 16 Hours, 52 Minutes, 41 Seconds after Pre'
                    var dateMatch = _dateMatchRegex.Match(row.QuerySelector("td:nth-of-type(2) > div:last-child").TextContent.Trim());
                    if (dateMatch.Success)
                        release.PublishDate = DateTime.ParseExact(dateMatch.Value, "yyyy-MM-dd hh:mm tt", CultureInfo.InvariantCulture);

                    release.Size = ParseUtil.GetBytes(row.QuerySelector("td:nth-of-type(5)").TextContent.Trim());
                    release.Seeders = ParseUtil.CoerceInt(row.QuerySelector("td:nth-of-type(7)").TextContent.Trim());
                    release.Peers = ParseUtil.CoerceInt(row.QuerySelector("td:nth-of-type(8)").TextContent.Trim()) + release.Seeders;

                    var categoryLink = row.QuerySelector("td:nth-of-type(1) a").GetAttribute("href");
                    var cat = ParseUtil.GetArgumentFromQueryString(categoryLink, "category");
                    release.Category = MapTrackerCatToNewznab(cat);

                    var grabs = row.QuerySelector("td:nth-child(6)").TextContent;
                    release.Grabs = ParseUtil.CoerceInt(grabs);

                    var cover = row.QuerySelector("td:nth-of-type(2) > div > img[src]")?.GetAttribute("src")?.Trim();
                    release.Poster = !string.IsNullOrEmpty(cover) && cover.StartsWith("/") ? new Uri(SiteLink + cover.TrimStart('/')) : null;

                    if (row.QuerySelector("img[title^=\"Free Torrent\"], img[title^=\"Sitewide Free Torrent\"]") != null)
                        release.DownloadVolumeFactor = 0;
                    else if (row.QuerySelector("img[title^=\"Silver Torrent\"]") != null)
                        release.DownloadVolumeFactor = 0.5;
                    else
                        release.DownloadVolumeFactor = 1;

                    release.UploadVolumeFactor = row.QuerySelector("img[title^=\"x2 Torrent\"]") != null ? 2 : 1;

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(results.ContentString, ex);
            }

            return releases;
        }
    }
}
