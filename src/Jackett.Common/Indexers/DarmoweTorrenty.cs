using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
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
    public class DarmoweTorenty : IndexerBase
    {
        public override string Id => "darmowetorenty";
        public override string Name => "Darmowe torenty";
        public override string Description => "Darmowe torenty is a POLISH Semi-Private Torrent Tracker for MOVIES / TV / GENERAL";
        public override string SiteLink { get; protected set; } = "https://darmowe-torenty.pl/";
        public override Encoding Encoding => Encoding.GetEncoding("iso-8859-2");
        public override string Language => "pl-PL";
        public override string Type => "semi-private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private string LoginUrl => SiteLink + "login.php";
        private string BrowseUrl => SiteLink + "torrenty.php";

        private static readonly Regex _SizeRegex = new Regex("Rozmiar: (\\d{1,4}\\.\\d{2}\\s[K|M|G][B])", RegexOptions.Compiled);
        private static readonly Regex _DateRegex = new Regex("Dodano: (\\d{2}\\/\\d{2}\\/\\d{4})", RegexOptions.Compiled);
        private static readonly Regex _SeedsRegex = new Regex("Seedów: (\\d+)", RegexOptions.Compiled);
        private static readonly Regex _LeechersRegex = new Regex("Leecherów: (\\d+)", RegexOptions.Compiled);
        private new ConfigurationDataBasicLogin configData
        {
            get => (ConfigurationDataBasicLogin)base.configData;
            set => base.configData = value;
        }

        public DarmoweTorenty(IIndexerConfigurationService configService, WebClient wc, Logger l,
            IProtectionService ps, ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataBasicLogin())
        {
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
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
            };

            caps.Categories.AddCategoryMapping(14, TorznabCatType.Movies, "Filmy");
            caps.Categories.AddCategoryMapping(27, TorznabCatType.MoviesDVD, "Filmy DVD-R");
            caps.Categories.AddCategoryMapping(28, TorznabCatType.MoviesSD, "Filmy VCD/SVCD");
            caps.Categories.AddCategoryMapping(29, TorznabCatType.MoviesBluRay, "Filmy BluRay/x264");
            caps.Categories.AddCategoryMapping(30, TorznabCatType.MoviesSD, "Filmy DivX/XviD LEKTOR/NAPISY PL");
            caps.Categories.AddCategoryMapping(72, TorznabCatType.MoviesSD, "Filmy DivX/XviD ENG/...");
            caps.Categories.AddCategoryMapping(31, TorznabCatType.Movies, "Filmy RMVB");
            caps.Categories.AddCategoryMapping(74, TorznabCatType.MoviesHD, "Filmy HD");
            caps.Categories.AddCategoryMapping(75, TorznabCatType.Movies3D, "Filmy 3D");
            caps.Categories.AddCategoryMapping(16, TorznabCatType.TV, "Seriale");
            caps.Categories.AddCategoryMapping(25, TorznabCatType.TV, "Seriale Polskie");
            caps.Categories.AddCategoryMapping(26, TorznabCatType.TV, "Seriale Zagraniczne");
            caps.Categories.AddCategoryMapping(17, TorznabCatType.Movies, "Dla Dzieci");
            caps.Categories.AddCategoryMapping(32, TorznabCatType.Movies, "Bajki Pl/Eng");
            caps.Categories.AddCategoryMapping(18, TorznabCatType.PCGames, "Gry");
            caps.Categories.AddCategoryMapping(34, TorznabCatType.PCGames, "Gry PC");
            caps.Categories.AddCategoryMapping(35, TorznabCatType.ConsolePSP, "Gry PS2/PS3/PSP");
            caps.Categories.AddCategoryMapping(36, TorznabCatType.ConsoleXBox, "Gry Xbox");
            caps.Categories.AddCategoryMapping(37, TorznabCatType.Console, "Gry Inne Konsole");
            caps.Categories.AddCategoryMapping(19, TorznabCatType.Audio, "Muzyka");
            caps.Categories.AddCategoryMapping(38, TorznabCatType.Audio, "Muzyka Polska/Zagraniczna");
            caps.Categories.AddCategoryMapping(39, TorznabCatType.Audio, "Muzyka Soundtracki");
            caps.Categories.AddCategoryMapping(40, TorznabCatType.Audio, "Muzyka Teledyski/Koncerty");
            caps.Categories.AddCategoryMapping(20, TorznabCatType.PCMobileOther, "GSM/PDA");
            caps.Categories.AddCategoryMapping(42, TorznabCatType.PCMobileOther, "Tapety GSM/PDA");
            caps.Categories.AddCategoryMapping(43, TorznabCatType.PCMobileOther, "Programy GSM/PDA");
            caps.Categories.AddCategoryMapping(44, TorznabCatType.PCMobileOther, "Filmy GSM/PDA");
            caps.Categories.AddCategoryMapping(45, TorznabCatType.PCMobileOther, "Dzwonki GSM/PDA");
            caps.Categories.AddCategoryMapping(46, TorznabCatType.PCMobileOther, "Gry GSM/PDA");
            caps.Categories.AddCategoryMapping(21, TorznabCatType.Books, "Książki/Czasopisma");
            caps.Categories.AddCategoryMapping(47, TorznabCatType.BooksEBook, "Książki/Czasopisma E-Booki");
            caps.Categories.AddCategoryMapping(48, TorznabCatType.AudioAudiobook, "Książki/Czasopisma Audio-Booki");
            caps.Categories.AddCategoryMapping(49, TorznabCatType.BooksMags, "Książki/Czasopisma Czasopisma");
            caps.Categories.AddCategoryMapping(50, TorznabCatType.BooksComics, "Książki/Czasopisma Komiksy");
            caps.Categories.AddCategoryMapping(22, TorznabCatType.PC, "Programy");
            caps.Categories.AddCategoryMapping(51, TorznabCatType.PC0day, "Programy Windows");
            caps.Categories.AddCategoryMapping(52, TorznabCatType.PC, "Programy Linux");
            caps.Categories.AddCategoryMapping(53, TorznabCatType.PCMac, "Programy Macintosh");
            caps.Categories.AddCategoryMapping(23, TorznabCatType.Other, "Inne");
            caps.Categories.AddCategoryMapping(55, TorznabCatType.Other, "Inne Tapety");
            caps.Categories.AddCategoryMapping(54, TorznabCatType.Other, "Inne Śmieszne");
            caps.Categories.AddCategoryMapping(56, TorznabCatType.TVSport, "Inne Sport");
            caps.Categories.AddCategoryMapping(57, TorznabCatType.Other, "Inne Pozostałe");
            caps.Categories.AddCategoryMapping(24, TorznabCatType.XXX, "Erotyka");
            caps.Categories.AddCategoryMapping(58, TorznabCatType.XXX, "Erotyka Czasopisma");
            caps.Categories.AddCategoryMapping(59, TorznabCatType.XXX, "Erotyka Zdjęcia");
            caps.Categories.AddCategoryMapping(60, TorznabCatType.XXX, "Erotyka Filmy");
            caps.Categories.AddCategoryMapping(61, TorznabCatType.XXX, "Erotyka Gry");
            caps.Categories.AddCategoryMapping(63, TorznabCatType.XXX, "Erotyka Hentai+18");
            caps.Categories.AddCategoryMapping(68, TorznabCatType.TVAnime, "Anime");
            caps.Categories.AddCategoryMapping(69, TorznabCatType.TVAnime, "Anime Pl");
            caps.Categories.AddCategoryMapping(70, TorznabCatType.TVAnime, "Anime");
            caps.Categories.AddCategoryMapping(76, TorznabCatType.Other, "Archiwum");

            return caps;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string>
            {
                { "uid", configData.Username.Value },
                { "pwd", configData.Password.Value }
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, LoginUrl, true);

            await ConfigureIfOK(result.Cookies, result.ContentString != null && result.Cookies.Contains("pass=") && !result.Cookies.Contains("deleted"), () =>
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(result.ContentString);
                var invalidPasswordDiv = dom.QuerySelector("div:contains(\"Podane hasło jest\")");
                var invalidLoginDiv = dom.QuerySelector("div:contains(\"Podany login jest\")");
                var bannedUserElement = dom.QuerySelector("b:contains(\"has been blocked - fill captcha that\")");
                var errorMessage = invalidLoginDiv?.TextContent ??
                                   invalidPasswordDiv?.TextContent ??
                                   bannedUserElement?.TextContent;
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        private ReleaseInfo ParseRow(IParentNode titleRow, IElement detailsRow)
        {
            var categoryAttribute = detailsRow.QuerySelector("a[href^=\"/torrenty.php?category=\"]").GetAttribute("href");
            var categoryUrl = new Uri(SiteLink + Uri.UnescapeDataString(categoryAttribute));
            var trackerCategory = HttpUtility.ParseQueryString(categoryUrl.Query)["category"];
            var categories = MapTrackerCatToNewznab(trackerCategory);
            var seedsMatch = _SeedsRegex.Match(detailsRow.TextContent);
            var leechersMatch = _LeechersRegex.Match(detailsRow.TextContent);
            var dateMatch = _DateRegex.Match(detailsRow.TextContent);
            var sizeMatch = _SizeRegex.Match(detailsRow.TextContent);
            var date = DateTime.MinValue; // In case of parsing failure
            if (dateMatch.Success)
            {
                date = DateTime.ParseExact(
                    $"{dateMatch.Groups[1].Value} +01:00", "dd/MM/yyyy zzz", CultureInfo.InvariantCulture);
            }
            var details = titleRow.QuerySelector("a[href^=\"details.php?id=\"]:has(span)");
            var detailsLink = new Uri(SiteLink + details.GetAttribute("href"));
            var encodedDownloadLink = detailsRow.QuerySelector("a[id^=\"download_\"]").GetAttribute("data-href");
            var siteDownloadLink = new Uri(SiteLink + Uri.UnescapeDataString(StringUtil.FromBase64(encodedDownloadLink)));
            var infoHash = HttpUtility.ParseQueryString(siteDownloadLink.Query)["id"];
            var posterStr = detailsRow.QuerySelector("img[src^=\"./imgtorrent/\"]")?.GetAttribute("src");
            var poster = !string.IsNullOrEmpty(posterStr) ? new Uri(SiteLink + posterStr) : null;
            var seeders = seedsMatch.Success ? int.Parse(seedsMatch.Groups[1].Value) : 0;
            var leechers = leechersMatch.Success ? int.Parse(leechersMatch.Groups[1].Value) : 0;
            var peers = seeders + leechers;
            var release = new ReleaseInfo
            {
                Title = details.TextContent,
                Category = categories,
                Seeders = seeders,
                Poster = poster,
                Peers = peers,
                PublishDate = date,
                DownloadVolumeFactor = 0,
                UploadVolumeFactor = 1,
                InfoHash = infoHash, // magnet link is auto generated from infohash
                Guid = detailsLink,
                Details = detailsLink,
                Size = sizeMatch.Success ? ParseUtil.GetBytes(sizeMatch.Groups[1].Value) : 0
            };
            return release;
        }
        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            var searchString = query.GetQueryString();
            var searchUrl = BrowseUrl;
            var queryCollection = new NameValueCollection
            {
                {"search", searchString},
                {"category", "0"}, // multi category search not supported
                {"erotyka", "1"}
            };
            searchUrl += "?" + queryCollection.GetQueryString(Encoding);

            var response = await RequestWithCookiesAsync(searchUrl);
            if (response.IsRedirect || response.Cookies != null && response.Cookies.Contains("pass=deleted;"))
            {
                // re-login
                await ApplyConfiguration(null);
                response = await RequestWithCookiesAsync(searchUrl);
            }

            var results = response.ContentString;
            try
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(results);
                var rows = dom.QuerySelectorAll("table.header > tbody > tr:has(td)");
                if (rows[0].TextContent.Contains("Nie ma torrentów")) // issue #9782
                {
                    return releases;
                }
                for (var i = 0; i < rows.Length; i += 2)
                {
                    // First row contains table, the second row contains the rest of the details
                    var releaseInfo = ParseRow(rows[i], rows[i + 1]);
                    releases.Add(releaseInfo);
                }
            }
            catch (Exception ex)
            {
                OnParseError(results, ex);
            }

            return releases;
        }
    }
}
