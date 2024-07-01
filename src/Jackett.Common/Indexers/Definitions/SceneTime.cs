using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using FlareSolverrSharp.Types;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class SceneTime : IndexerBase
    {
        public override string Id => "scenetime";
        public override string Name => "SceneTime";
        public override string Description => "Always on time";
        public override string SiteLink { get; protected set; } = "https://www.scenetime.com/";
        public override string Language => "en-US";
        public override string Type => "private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private string SearchUrl => SiteLink + "browse.php";
        private string DownloadUrl => SiteLink + "download.php/{0}/download.torrent";

        private new ConfigurationDataSceneTime configData => (ConfigurationDataSceneTime)base.configData;

        public SceneTime(IIndexerConfigurationService configService, WebClient w, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(configService: configService,
                   client: w,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataSceneTime())
        {
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId
                },
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q, MovieSearchParam.ImdbId
                },
                MusicSearchParams = new List<MusicSearchParam>
                {
                    MusicSearchParam.Q
                },
                BookSearchParams = new List<BookSearchParam>
                {
                    BookSearchParam.Q
                },
                TvSearchImdbAvailable = true
            };

            caps.Categories.AddCategoryMapping(10, TorznabCatType.XXX, "Movies Adult");
            caps.Categories.AddCategoryMapping(47, TorznabCatType.Movies, "Movie Packs");
            caps.Categories.AddCategoryMapping(57, TorznabCatType.MoviesSD, "Movies SD");
            caps.Categories.AddCategoryMapping(59, TorznabCatType.MoviesHD, "Movies HD");
            caps.Categories.AddCategoryMapping(64, TorznabCatType.Movies3D, "Movies 3D");
            caps.Categories.AddCategoryMapping(82, TorznabCatType.MoviesOther, "Movies CAM/TS");
            caps.Categories.AddCategoryMapping(16, TorznabCatType.MoviesUHD, "Movies UHD");
            caps.Categories.AddCategoryMapping(2, TorznabCatType.TVUHD, "TV UHD");
            caps.Categories.AddCategoryMapping(43, TorznabCatType.TV, "TV Packs");
            caps.Categories.AddCategoryMapping(9, TorznabCatType.TVHD, "TV HD");
            caps.Categories.AddCategoryMapping(77, TorznabCatType.TVSD, "TV SD");
            caps.Categories.AddCategoryMapping(1, TorznabCatType.TVAnime, "TV ANIME");
            caps.Categories.AddCategoryMapping(6, TorznabCatType.PCGames, "Games PC-ISO");
            caps.Categories.AddCategoryMapping(48, TorznabCatType.ConsoleXBox, "Games XBOX");
            caps.Categories.AddCategoryMapping(51, TorznabCatType.ConsoleWii, "Games Wii");
            caps.Categories.AddCategoryMapping(55, TorznabCatType.ConsoleNDS, "Games Nintendo");
            caps.Categories.AddCategoryMapping(12, TorznabCatType.ConsolePS4, "Games PS");
            caps.Categories.AddCategoryMapping(15, TorznabCatType.ConsoleOther, "Games Dreamcast");
            caps.Categories.AddCategoryMapping(52, TorznabCatType.PCMac, "Mac/Linux");
            caps.Categories.AddCategoryMapping(53, TorznabCatType.PC0day, "Apps");
            caps.Categories.AddCategoryMapping(24, TorznabCatType.PCMobileOther, "Mobile Apps");
            caps.Categories.AddCategoryMapping(7, TorznabCatType.Books, "Books and Magazines");
            caps.Categories.AddCategoryMapping(65, TorznabCatType.BooksComics, "Books Comics");
            caps.Categories.AddCategoryMapping(4, TorznabCatType.Audio, "Music");
            caps.Categories.AddCategoryMapping(116, TorznabCatType.Audio, "Music Packs");

            return caps;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            CookieHeader = configData.Cookie.Value;
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
                throw new Exception("Your cookie did not work: " + e.Message);
            }
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            Dictionary<string, string> headers = null;
            if (!string.IsNullOrEmpty(configData.UserAgent.Value))
            {
                headers = new Dictionary<string, string>
                {
                    { "User-Agent", configData.UserAgent.Value }
                };
            }
            var qParams = new NameValueCollection
            {
                {"cata", "yes"}
            };

            var catList = MapTorznabCapsToTrackers(query);
            foreach (var cat in catList)
                qParams.Set($"c{cat}", "1");

            if (query.IsImdbQuery)
                qParams.Set("imdb", query.ImdbID);

            if (!string.IsNullOrEmpty(query.SanitizedSearchTerm))
                qParams.Set("search", query.GetQueryString());

            // If Only Freeleech Enabled
            if (configData.Freeleech.Value)
                qParams.Set("freeleech", "on");

            var searchUrl = SearchUrl + "?" + qParams.GetQueryString();
            var results = await RequestWithCookiesAsync(searchUrl, headers: headers);

            // response without results (the message is misleading)
            if (results.ContentString?.Contains("slow down geek!!!") == true)
            {
                logger.Warn($"SceneTime: Query Limit exceeded, retrying in 5 seconds.");
                webclient.requestDelay = 5;
                results = await RequestWithCookiesAsync(searchUrl);
                webclient.requestDelay = 0;
            }

            // not logged in
            if (results.ContentString == null || !results.ContentString.Contains("/logout.php"))
                throw new Exception("The user is not logged in. It is possible that the cookie has expired or you " +
                                    "made a mistake when copying it. Please check the settings.");

            return ParseResponse(query, results.ContentString);
        }

        private List<ReleaseInfo> ParseResponse(TorznabQuery query, string htmlResponse)
        {
            var releases = new List<ReleaseInfo>();

            try
            {
                var parser = new HtmlParser();
                using var dom = parser.ParseDocument(htmlResponse);

                var table = dom.QuerySelector("table.movehere");
                if (table == null)
                    return releases; // no results

                var headerColumns = table.QuerySelectorAll("tbody > tr > td.cat_Head").Select(x => x.TextContent).ToList();
                var categoryIndex = headerColumns.FindIndex(x => x.Equals("Type"));
                var nameIndex = headerColumns.FindIndex(x => x.Equals("Name"));
                var sizeIndex = headerColumns.FindIndex(x => x.Equals("Size"));
                var seedersIndex = headerColumns.FindIndex(x => x.Equals("Seeders"));
                var leechersIndex = headerColumns.FindIndex(x => x.Equals("Leechers"));

                var rows = dom.QuerySelectorAll("tr.browse");
                foreach (var row in rows)
                {
                    var qDescCol = row.Children[nameIndex];
                    var qLink = qDescCol.QuerySelector("a");
                    var title = qLink.TextContent;

                    if (!query.MatchQueryStringAND(title))
                        continue;

                    var details = new Uri(SiteLink + "/" + qLink.GetAttribute("href"));
                    var torrentId = ParseUtil.GetArgumentFromQueryString(qLink.GetAttribute("href"), "id");
                    var link = new Uri(string.Format(DownloadUrl, torrentId));

                    var categoryLink = row.Children[categoryIndex].QuerySelector("a")?.GetAttribute("href");
                    var cat = categoryLink != null ? ParseUtil.GetArgumentFromQueryString(categoryLink, "cat") : "82"; // default

                    var seeders = ParseUtil.CoerceInt(row.Children[seedersIndex].TextContent.Trim());

                    var dateAdded = qDescCol.QuerySelector("span[class=\"elapsedDate\"]").GetAttribute("title").Trim();
                    var publishDate = DateTime.TryParseExact(dateAdded, "dddd, MMMM d, yyyy \\a\\t h:mmtt", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date)
                        ? date
                        : DateTimeUtil.FromTimeAgo(qDescCol.QuerySelector("span[class=\"elapsedDate\"]").TextContent.Trim());

                    var release = new ReleaseInfo
                    {
                        Guid = details,
                        Details = details,
                        Link = link,
                        Title = title,
                        Category = MapTrackerCatToNewznab(cat),
                        PublishDate = publishDate,
                        Size = ParseUtil.GetBytes(row.Children[sizeIndex].TextContent),
                        Seeders = seeders,
                        Peers = ParseUtil.CoerceInt(row.Children[leechersIndex].TextContent.Trim()) + seeders,
                        DownloadVolumeFactor = row.QuerySelector("font > b:contains(Freeleech)") != null ? 0 : 1,
                        UploadVolumeFactor = 1,
                        MinimumRatio = 1,
                        MinimumSeedTime = 259200 // 72 hours
                    };

                    if (query.IsImdbQuery)
                        release.Imdb = ParseUtil.CoerceLong(query.ImdbIDShort);

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(htmlResponse, ex);
            }

            return releases;
        }
    }
}
