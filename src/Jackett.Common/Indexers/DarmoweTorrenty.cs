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
    public class DarmoweTorrenty : BaseWebIndexer
    {
        private string LoginUrl => SiteLink + "login.php";
        private string BrowseUrl => SiteLink + "torrenty.php";

        private static readonly Regex _SizeRegex = new Regex("Rozmiar: (\\d{1,4}\\.\\d{2}\\s[K|M|G][B])", RegexOptions.Compiled);
        private static readonly Regex _DateRegex = new Regex("Dodano: (\\d{2}\\/\\d{2}\\/\\d{4})", RegexOptions.Compiled);
        private static readonly Regex _SeedsRegex = new Regex("Seedów: (\\d+)", RegexOptions.Compiled);
        private static readonly Regex _LeechersRegex = new Regex("Leecherów: (\\d+)", RegexOptions.Compiled);

        private TimeZoneInfo _polandTz;
        private new ConfigurationDataBasicLoginWithRSSAndDisplay configData
        {
            get => (ConfigurationDataBasicLoginWithRSSAndDisplay)base.configData;
            set => base.configData = value;
        }

        public DarmoweTorrenty(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(id: "darmowetorrenty",
                   name: "Darmowe torrenty",
                   description: "Darmowe torrenty is a POLISH Semi-Private Torrent Tracker for MOVIES / TV / GENERAL",
                   link: "https://darmowe-torenty.pl/",
                   caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   configData: new ConfigurationDataBasicLoginWithRSSAndDisplay())
        {
            Encoding = Encoding.GetEncoding("iso-8859-2");
            Language = "pl-pl";
            Type = "semi-private";
            AddCategoryMapping(14, TorznabCatType.Movies, "Filmy");
            AddCategoryMapping(27, TorznabCatType.MoviesDVD, "Filmy DVD-R");
            AddCategoryMapping(28, TorznabCatType.MoviesSD, "Filmy VCD/SVCD");
            AddCategoryMapping(29, TorznabCatType.MoviesBluRay, "Filmy BluRay/x264");
            AddCategoryMapping(30, TorznabCatType.MoviesSD, "Filmy DivX/XviD LEKTOR/NAPISY PL");
            AddCategoryMapping(72, TorznabCatType.MoviesSD, "Filmy DivX/XviD ENG/...");
            AddCategoryMapping(31, TorznabCatType.Movies, "Filmy RMVB");
            AddCategoryMapping(74, TorznabCatType.MoviesHD, "Filmy HD");
            AddCategoryMapping(75, TorznabCatType.Movies3D, "Filmy 3D");
            AddCategoryMapping(16, TorznabCatType.TV, "Seriale");
            AddCategoryMapping(25, TorznabCatType.TV, "Seriale Polskie");
            AddCategoryMapping(26, TorznabCatType.TV, "Seriale Zagraniczne");
            AddCategoryMapping(17, TorznabCatType.Movies, "Dla Dzieci");
            AddCategoryMapping(32, TorznabCatType.Movies, "Bajki Pl/Eng");
            AddCategoryMapping(18, TorznabCatType.PCGames, "Gry");
            AddCategoryMapping(34, TorznabCatType.PCGames, "Gry PC");
            AddCategoryMapping(35, TorznabCatType.ConsolePSP, "Gry PS2/PS3/PSP");
            AddCategoryMapping(36, TorznabCatType.ConsoleXbox, "Gry Xbox");
            AddCategoryMapping(37, TorznabCatType.Console, "Gry Inne Konsole");
            AddCategoryMapping(19, TorznabCatType.Audio, "Muzyka");
            AddCategoryMapping(38, TorznabCatType.Audio, "Muzyka Polska/Zagraniczna");
            AddCategoryMapping(39, TorznabCatType.Audio, "Muzyka Soundtracki");
            AddCategoryMapping(40, TorznabCatType.Audio, "Muzyka Teledyski/Koncerty");
            AddCategoryMapping(20, TorznabCatType.PCPhoneOther, "GSM/PDA");
            AddCategoryMapping(42, TorznabCatType.PCPhoneOther, " Tapety GSM/PDA");
            AddCategoryMapping(43, TorznabCatType.PCPhoneOther, " Programy GSM/PDA");
            AddCategoryMapping(44, TorznabCatType.PCPhoneOther, " Filmy GSM/PDA");
            AddCategoryMapping(45, TorznabCatType.PCPhoneOther, " Dzwonki GSM/PDA");
            AddCategoryMapping(46, TorznabCatType.PCPhoneOther, " Gry GSM/PDA");
            AddCategoryMapping(21, TorznabCatType.Books, "Książki/Czasopisma");
            AddCategoryMapping(47, TorznabCatType.BooksEbook, "Książki/Czasopisma E-Booki");
            AddCategoryMapping(48, TorznabCatType.AudioAudiobook, "Książki/Czasopisma Audio-Booki");
            AddCategoryMapping(49, TorznabCatType.BooksMagazines, "Książki/Czasopisma Czasopisma");
            AddCategoryMapping(50, TorznabCatType.BooksComics, "Książki/Czasopisma Komiksy");
            AddCategoryMapping(22, TorznabCatType.PC, "Programy");
            AddCategoryMapping(51, TorznabCatType.PC0day, "Programy Windows");
            AddCategoryMapping(52, TorznabCatType.PC, "Programy Linux");
            AddCategoryMapping(53, TorznabCatType.PCMac, "Programy Macintosh");
            AddCategoryMapping(23, TorznabCatType.Other, "Inne");
            AddCategoryMapping(55, TorznabCatType.Other, "Inne Tapety");
            AddCategoryMapping(54, TorznabCatType.Other, "Inne Śmieszne");
            AddCategoryMapping(56, TorznabCatType.TVSport, "Inne Sport");
            AddCategoryMapping(57, TorznabCatType.Other, "Inne Pozostałe");
            AddCategoryMapping(24, TorznabCatType.XXX, "Erotyka");
            AddCategoryMapping(58, TorznabCatType.XXX, "Erotyka Czasopisma");
            AddCategoryMapping(59, TorznabCatType.XXX, "Erotyka Zdjęcia");
            AddCategoryMapping(60, TorznabCatType.XXX, "Erotyka Filmy");
            AddCategoryMapping(61, TorznabCatType.XXX, "Erotyka Gry");
            AddCategoryMapping(63, TorznabCatType.XXX, "Erotyka Hentai+18");
            AddCategoryMapping(68, TorznabCatType.TVAnime, "Anime");
            AddCategoryMapping(69, TorznabCatType.TVAnime, "Anime Pl");
            AddCategoryMapping(70, TorznabCatType.TVAnime, "Anime");
            AddCategoryMapping(76, TorznabCatType.Other, "Archiwum");
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
                var errorMessage = dom.QuerySelector("div:contains(\"Podany login jest\")").InnerHtml;
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
                var polishDate = DateTime.SpecifyKind(DateTime.ParseExact(dateMatch.Groups[1].Value, "dd/MM/yyyy", CultureInfo.InvariantCulture), DateTimeKind.Unspecified);
                var pubDateUtc = TimeZoneInfo.ConvertTimeToUtc(polishDate, _polandTz);
                date = pubDateUtc.ToLocalTime();
            }
            var details = titleRow.QuerySelector("a[href^=\"details.php?id=\"]:has(span)");
            var encodedDownloadLink = detailsRow.QuerySelector("a[id^=\"download_\"]").GetAttribute("data-href");
            var downloadLink = new Uri(SiteLink + Uri.UnescapeDataString(StringUtil.FromBase64(encodedDownloadLink)));
            var bannerLink = detailsRow.QuerySelector("img[src^=\"./imgtorrent/\"]")?.GetAttribute("src");
            var seeders = seedsMatch.Success ? int.Parse(seedsMatch.Groups[1].Value) : (int?)null;
            var leechers = leechersMatch.Success ? int.Parse(leechersMatch.Groups[1].Value) : (int?)null;
            var peers = (seeders == null && leechers == null) ? (int?)null : (seeders ?? 0) + (leechers ?? 0);
            var release = new ReleaseInfo
            {
                MinimumRatio = 1,
                MinimumSeedTime = 90 * 60,
                Title = details.TextContent,
                Category = categories,
                Seeders = seeders,
                BannerUrl = !string.IsNullOrEmpty(bannerLink) ? new Uri(SiteLink + bannerLink) : null,
                Peers = peers,
                PublishDate = date,
                DownloadVolumeFactor = 0,
                UploadVolumeFactor = 1,
                Link = downloadLink,
                Guid = downloadLink,
                Size = sizeMatch.Success ? ReleaseInfo.GetBytes(sizeMatch.Groups[1].Value) : (long?)null
            };
            return release;
        }
        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var startTransition = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 3, 0, 0), 3, 5, DayOfWeek.Sunday);
            var endTransition = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 4, 0, 0), 10, 5, DayOfWeek.Sunday);
            var delta = new TimeSpan(1, 0, 0);
            var adjustment = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(new DateTime(1999, 10, 1), DateTime.MaxValue.Date, delta, startTransition, endTransition);
            TimeZoneInfo.AdjustmentRule[] adjustments = { adjustment };
            _polandTz = TimeZoneInfo.CreateCustomTimeZone("C. Europe Standard Time", new TimeSpan(1, 0, 0), "(GMT+01:00) C. Europe Standard Time", "C. Europe Standard Time", "C. Europe DST Time", adjustments);


            var releases = new List<ReleaseInfo>();

            var searchString = query.GetQueryString();
            var searchUrl = BrowseUrl;
            var queryCollection = new NameValueCollection
            {
                {"search", searchString},
                {"category", "0"}, // multi category search not supported
                {"erotyka", "1"}
            };
            searchUrl += "?" + queryCollection.GetQueryString();

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
                for (var i = 0; i < rows.Length; i+=2)
                {
                    // First row contains table, the second row contains the rest of the details
                    var releaseInfo = ParseRow(rows[i], rows[i+1]);
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
