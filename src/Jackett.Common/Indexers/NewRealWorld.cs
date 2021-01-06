using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Dom;
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
    public class NewRealWorld : BaseWebIndexer
    {
        private string LoginUrl => SiteLink + "login.php";
        private string BrowseUrl => SiteLink + "browse.php";

        private new ConfigurationDataBasicLoginWithRSSAndDisplay configData
        {
            get => (ConfigurationDataBasicLoginWithRSSAndDisplay)base.configData;
            set => base.configData = value;
        }

        public NewRealWorld(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "newrealworld",
                   name: "New Real World",
                   description: "A German general tracker.",
                   link: "https://nrw-tracker.eu/",
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
                   configData: new ConfigurationDataBasicLoginWithRSSAndDisplay())
        {
            Encoding = Encoding.GetEncoding("iso-8859-1");
            Language = "de-de";
            Type = "private";

            AddCategoryMapping(39, TorznabCatType.TVAnime, "Anime: HD|1080p");
            AddCategoryMapping(38, TorznabCatType.TVAnime, "Anime: HD|720p");
            AddCategoryMapping(1, TorznabCatType.TVAnime, "Anime: SD");
            AddCategoryMapping(7, TorznabCatType.PCMobileOther, "Appz: Handy-PDA");
            AddCategoryMapping(36, TorznabCatType.PCMac, "Appz: Mac");
            AddCategoryMapping(18, TorznabCatType.PC, "Appz: Sonstiges");
            AddCategoryMapping(17, TorznabCatType.PC, "Appz: Win");
            AddCategoryMapping(15, TorznabCatType.Audio, "Audio: DVD-R");
            AddCategoryMapping(49, TorznabCatType.AudioLossless, "Audio: Flac");
            AddCategoryMapping(30, TorznabCatType.AudioAudiobook, "Audio: Hörspiele");
            AddCategoryMapping(14, TorznabCatType.AudioMP3, "Audio: MP3");
            AddCategoryMapping(22, TorznabCatType.AudioVideo, "Audio: Videoclip");
            AddCategoryMapping(19, TorznabCatType.Other, "Diverses: Sonstiges");
            AddCategoryMapping(43, TorznabCatType.TVDocumentary, "Dokus: HD");
            AddCategoryMapping(2, TorznabCatType.TVDocumentary, "Dokus: SD");
            AddCategoryMapping(3, TorznabCatType.Books, "Ebooks: Bücher");
            AddCategoryMapping(52, TorznabCatType.BooksComics, "Ebooks: Comics");
            AddCategoryMapping(53, TorznabCatType.BooksMags, "Ebooks: Magazine");
            AddCategoryMapping(55, TorznabCatType.BooksOther, "Ebooks: XXX");
            AddCategoryMapping(54, TorznabCatType.BooksOther, "Ebooks: Zeitungen");
            AddCategoryMapping(47, TorznabCatType.PCMobileOther, "Games: Andere");
            AddCategoryMapping(32, TorznabCatType.PCMac, "Games: Mac");
            AddCategoryMapping(41, TorznabCatType.ConsoleNDS, "Games: NDS/3DS");
            AddCategoryMapping(4, TorznabCatType.PCGames, "Games: PC");
            AddCategoryMapping(5, TorznabCatType.ConsolePS3, "Games: PS2");
            AddCategoryMapping(9, TorznabCatType.ConsolePS3, "Games: PS3");
            AddCategoryMapping(6, TorznabCatType.ConsolePSP, "Games: PSP");
            AddCategoryMapping(28, TorznabCatType.ConsoleWii, "Games: Wii");
            AddCategoryMapping(31, TorznabCatType.ConsoleXBox, "Games: XboX");
            AddCategoryMapping(51, TorznabCatType.Movies3D, "Movies: 3D");
            AddCategoryMapping(37, TorznabCatType.MoviesBluRay, "Movies: BluRay");
            AddCategoryMapping(25, TorznabCatType.MoviesHD, "Movies: HD|1080p");
            AddCategoryMapping(29, TorznabCatType.MoviesHD, "Movies: HD|720p");
            AddCategoryMapping(11, TorznabCatType.MoviesDVD, "Movies: SD|DVD-R");
            AddCategoryMapping(8, TorznabCatType.MoviesSD, "Movies: SD|x264");
            AddCategoryMapping(13, TorznabCatType.MoviesSD, "Movies: SD|XviD");
            AddCategoryMapping(40, TorznabCatType.MoviesForeign, "Movies: US Movies");
            AddCategoryMapping(33, TorznabCatType.TV, "Serien: DVD-R");
            AddCategoryMapping(34, TorznabCatType.TVHD, "Serien: HD");
            AddCategoryMapping(56, TorznabCatType.TVHD, "Serien: Packs|HD");
            AddCategoryMapping(44, TorznabCatType.TVSD, "Serien: Packs|SD");
            AddCategoryMapping(16, TorznabCatType.TVSD, "Serien: SD");
            AddCategoryMapping(10, TorznabCatType.TVOther, "Serien: TV/Shows");
            AddCategoryMapping(21, TorznabCatType.TVForeign, "Serien: US TV");
            AddCategoryMapping(24, TorznabCatType.TVSport, "Sport: Diverses");
            AddCategoryMapping(23, TorznabCatType.TVSport, "Sport: Wrestling");
            AddCategoryMapping(57, TorznabCatType.Movies, "Tracker - Crew: pmHD");
            AddCategoryMapping(58, TorznabCatType.MoviesHD, "Ultra-HD: 4K");
            AddCategoryMapping(46, TorznabCatType.XXXOther, "XXX: Diverses");
            AddCategoryMapping(50, TorznabCatType.XXX, "XXX: HD");
            AddCategoryMapping(45, TorznabCatType.XXXPack, "XXX: Packs");
            AddCategoryMapping(27, TorznabCatType.XXX, "XXX: SD");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string>
            {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "submit", "Log+in!" }
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, LoginUrl, true);
            await ConfigureIfOK(result.Cookies, result.ContentString != null && result.ContentString.Contains("logout.php"), () =>
                {
                    var parser = new HtmlParser();
                    var dom = parser.ParseDocument(result.ContentString);
                    var errorMessage = dom.QuerySelector("table.tableinborder").InnerHtml;
                    throw new ExceptionWithConfigData(errorMessage, configData);
                });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var startTransition = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 3, 0, 0), 3, 5, DayOfWeek.Sunday);
            var endTransition = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 4, 0, 0), 10, 5, DayOfWeek.Sunday);
            var delta = new TimeSpan(1, 0, 0);
            var adjustment = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(new DateTime(1999, 10, 1), DateTime.MaxValue.Date, delta, startTransition, endTransition);
            TimeZoneInfo.AdjustmentRule[] adjustments = { adjustment };
            var germanyTz = TimeZoneInfo.CreateCustomTimeZone("W. Europe Standard Time", new TimeSpan(1, 0, 0), "(GMT+01:00) W. Europe Standard Time", "W. Europe Standard Time", "W. Europe DST Time", adjustments);

            var releases = new List<ReleaseInfo>();

            var searchString = query.GetQueryString();
            var searchUrl = BrowseUrl;
            var queryCollection = new NameValueCollection
            {
                {"showsearch", "1"},
                {"incldead", "1"},
                {"orderby", "added"},
                {"sort", "desc"},
                {"cat", MapTorznabCapsToTrackers(query).FirstIfSingleOrDefault("0")}
            };

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                queryCollection.Add("search", searchString);
            }

            searchUrl += "?" + queryCollection.GetQueryString();

            var response = await RequestWithCookiesAsync(searchUrl);
            if (response.IsRedirect)
            {
                // re-login
                await ApplyConfiguration(null);
                response = await RequestWithCookiesAsync(searchUrl);
            }

            try
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(response.ContentString);
                var rows = dom.QuerySelectorAll("table.testtable> tbody > tr:has(td.tableb)");

                foreach (var row in rows)
                {

                    var qDetailsLink = row.QuerySelector("a[href^=\"details.php?id=\"]");
                    var title = qDetailsLink.TextContent;
                    if (!query.MatchQueryStringAND(title))
                        continue;

                    var qCatLink = row.QuerySelector("a[href^=\"browse.php?cat=\"]");
                    var qSeeders = row.QuerySelector("td > table.testtable2 > tbody > tr > td:nth-of-type(2) > strong:nth-of-type(1)");
                    var qLeechers = row.QuerySelector("td > table.testtable2 > tbody > tr > td:nth-of-type(2) > strong:nth-of-type(2)");
                    var qDateStr = row.QuerySelector("td > table.testtable2 > tbody > tr > td:nth-of-type(5)");
                    var qSize = row.QuerySelector("td > table.testtable2 > tbody > tr > td:nth-of-type(1) > strong:nth-of-type(1)");
                    var qDownloadLink = row.QuerySelector("a[href*=\"download\"]");

                    var catStr = qCatLink.GetAttribute("href").Split('=')[1];

                    var dlLink = qDownloadLink.GetAttribute("href");
                    if (dlLink.Contains("javascript")) // depending on the user agent the DL link is a javascript call
                    {
                        var dlLinkParts = dlLink.Split(new[] { '\'', ',' });
                        dlLink = SiteLink + "download/" + dlLinkParts[3] + "/" + dlLinkParts[5];
                    }
                    var link = new Uri(dlLink);
                    var seeders = ParseUtil.CoerceInt(qSeeders.Text());
                    var dateStr = qDateStr.TextContent.Replace('\xA0', ' ');
                    var dateGerman = DateTime.SpecifyKind(DateTime.ParseExact(dateStr, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture), DateTimeKind.Unspecified);
                    var pubDateUtc = TimeZoneInfo.ConvertTimeToUtc(dateGerman, germanyTz);
                    var files = ParseUtil.CoerceInt(row.QuerySelector("td:contains(Datei) > strong ~ strong").TextContent);
                    var details = new Uri(SiteLink + qDetailsLink.GetAttribute("href"));
                    var leechers = ParseUtil.CoerceInt(qLeechers.Text());
                    var size = ReleaseInfo.GetBytes(qSize.TextContent.Replace(".", "").Replace(",", "."));
                    var downloadVolumeFactor = row.QuerySelector("img[title=\"OnlyUpload\"]") != null ? 0 : 1;
                    var release = new ReleaseInfo
                    {
                        MinimumRatio = 0.75,
                        MinimumSeedTime = 0,
                        Title = title,
                        Category = MapTrackerCatToNewznab(catStr),
                        Link = link,
                        Details = details,
                        Guid = link,
                        Size = size,
                        Seeders = seeders,
                        Peers = leechers + seeders,
                        PublishDate = pubDateUtc,
                        Files = files,
                        DownloadVolumeFactor = downloadVolumeFactor,
                        UploadVolumeFactor = 1
                    };
                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(response.ContentString, ex);
            }

            return releases;
        }
    }
}
