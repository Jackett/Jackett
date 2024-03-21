using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Jackett.Common.Helpers;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class SpeedCDcookie : IndexerBase
    {
        public override string Id => "speedcdcookie";
        public override string Name => "Speed.cd (cookie)";
        public override string Description => "Your home now!";
        public override string SiteLink { get; protected set; } = "https://speed.cd/";
        public override string[] AlternativeSiteLinks => new[]
        {
            "https://speed.cd/",
            "https://speed.click/",
            "https://speeders.me/"
        };
        public override string Language => "en-US";
        public override string Type => "private";

        public override bool SupportsPagination => true;

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private string LoginUrl1 => SiteLink + "login";
        private string LoginUrl2 => SiteLink + "login/API";
        private string SearchUrl => SiteLink + "browse/";
        private new ConfigurationDataCookieUA configData => (ConfigurationDataCookieUA)base.configData;


        public SpeedCDcookie(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataCookieUA(
                       @"Speed.Cd have increased their security. If you are having problems please check the security tab in your 
                    Speed.Cd profile. Eg. Geo Locking, your seedbox may be in a different country to the one where you login via your
                    web browser.<br><br>For best results, change the 'Torrents per page' setting to 100 in<br>'Profile Settings > Torrents'.
                    <br><br>This site may use Cloudflare DDoS Protection, therefore Jackett requires <a 
                    href='https://github.com/Jackett/Jackett#configuring-flaresolverr' target='_blank'>FlareSolverr</a> to access it."))
        {
            configData.AddDynamic("Freeleech", new BoolConfigurationItem("Search freeleech only") { Value = false });
            configData.AddDynamic("ExcludeArchives", new BoolConfigurationItem("Exclude torrents with RAR files") { Value = false });
            configData.AddDynamic("AccountActivity", new DisplayInfoConfigurationItem("Account Inactivity", "Accounts not being used for 3 months will be removed to make room for active members."));
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
                }
            };

            caps.Categories.AddCategoryMapping(1, TorznabCatType.MoviesOther, "Movies/XviD");
            caps.Categories.AddCategoryMapping(42, TorznabCatType.Movies, "Movies/Packs");
            caps.Categories.AddCategoryMapping(32, TorznabCatType.Movies, "Movies/Kids");
            caps.Categories.AddCategoryMapping(43, TorznabCatType.MoviesHD, "Movies/HD");
            caps.Categories.AddCategoryMapping(47, TorznabCatType.Movies, "Movies/DiVERSiTY");
            caps.Categories.AddCategoryMapping(28, TorznabCatType.MoviesBluRay, "Movies/B-Ray");
            caps.Categories.AddCategoryMapping(48, TorznabCatType.Movies3D, "Movies/3D");
            caps.Categories.AddCategoryMapping(40, TorznabCatType.MoviesDVD, "Movies/DVD-R");
            caps.Categories.AddCategoryMapping(56, TorznabCatType.Movies, "Movies/Anime");
            caps.Categories.AddCategoryMapping(50, TorznabCatType.TVSport, "TV/Sports");
            caps.Categories.AddCategoryMapping(52, TorznabCatType.TVHD, "TV/B-Ray");
            caps.Categories.AddCategoryMapping(53, TorznabCatType.TVSD, "TV/DVD-R");
            caps.Categories.AddCategoryMapping(41, TorznabCatType.TV, "TV/Packs");
            caps.Categories.AddCategoryMapping(55, TorznabCatType.TV, "TV/Kids");
            caps.Categories.AddCategoryMapping(57, TorznabCatType.TV, "TV/DiVERSiTY");
            caps.Categories.AddCategoryMapping(49, TorznabCatType.TVHD, "TV/HD");
            caps.Categories.AddCategoryMapping(2, TorznabCatType.TVSD, "TV/Episodes");
            caps.Categories.AddCategoryMapping(30, TorznabCatType.TVAnime, "TV/Anime");
            caps.Categories.AddCategoryMapping(25, TorznabCatType.PCISO, "Games/PC ISO");
            caps.Categories.AddCategoryMapping(39, TorznabCatType.ConsoleWii, "Games/Wii");
            caps.Categories.AddCategoryMapping(45, TorznabCatType.ConsolePS3, "Games/PS3");
            caps.Categories.AddCategoryMapping(35, TorznabCatType.Console, "Games/Nintendo");
            caps.Categories.AddCategoryMapping(33, TorznabCatType.ConsoleXBox360, "Games/XboX360");
            caps.Categories.AddCategoryMapping(46, TorznabCatType.PCMobileOther, "Mobile");
            caps.Categories.AddCategoryMapping(24, TorznabCatType.PC0day, "Apps/0DAY");
            caps.Categories.AddCategoryMapping(51, TorznabCatType.PCMac, "Mac");
            caps.Categories.AddCategoryMapping(54, TorznabCatType.Books, "Educational");
            caps.Categories.AddCategoryMapping(27, TorznabCatType.Books, "Books-Mags");
            caps.Categories.AddCategoryMapping(26, TorznabCatType.Audio, "Music/Audio");
            caps.Categories.AddCategoryMapping(3, TorznabCatType.Audio, "Music/Flac");
            caps.Categories.AddCategoryMapping(44, TorznabCatType.Audio, "Music/Pack");
            caps.Categories.AddCategoryMapping(29, TorznabCatType.AudioVideo, "Music/Video");

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
            var headers = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(configData.UserAgent.Value))
            {
                headers.Add("User-Agent", configData.UserAgent.Value);
            }

            var releases = new List<ReleaseInfo>();

            // the order of the params is important!
            var qc = new List<string>();

            var catList = MapTorznabCapsToTrackers(query);
            foreach (var cat in catList)
                qc.Add(cat);

            if (((BoolConfigurationItem)configData.GetDynamic("Freeleech")).Value)
                qc.Add("freeleech");

            if (((BoolConfigurationItem)configData.GetDynamic("ExcludeArchives")).Value)
                qc.Add("norar");

            if (query.IsImdbQuery)
            {
                var term = query.ImdbID;

                if (!string.IsNullOrWhiteSpace(query.GetEpisodeSearchString()))
                {
                    term += $" {query.GetEpisodeSearchString()}";

                    if (query.Season > 0 && string.IsNullOrEmpty(query.Episode))
                        term += "*";
                }

                qc.Add("deep");
                qc.Add("q");
                qc.Add(WebUtilityHelpers.UrlEncode(term.Trim(), Encoding));
            }
            else
            {
                var term = query.GetQueryString();

                if (!string.IsNullOrWhiteSpace(query.GetEpisodeSearchString()) && query.Season > 0 && string.IsNullOrEmpty(query.Episode))
                    term += "*";

                qc.Add("q");
                qc.Add(WebUtilityHelpers.UrlEncode(term.Trim(), Encoding));
            }

            if (query.Limit > 0 && query.Offset > 0)
            {
                var page = query.Offset / query.Limit + 1;

                qc.Add("p");
                qc.Add(page.ToString());
            }

            var searchUrl = SearchUrl + string.Join("/", qc);
            var response = await RequestWithCookiesAndRetryAsync(searchUrl, headers: headers);

            try
            {
                var parser = new HtmlParser();
                using var dom = parser.ParseDocument(response.ContentString);

                var rows = dom.QuerySelectorAll("div.boxContent > table > tbody > tr");
                foreach (var row in rows)
                {
                    var title = CleanTitle(row.QuerySelector("td:nth-child(2) > div > a[href^=\"/t/\"]")?.TextContent);
                    var link = new Uri(SiteLink + row.QuerySelector("td:nth-child(4) a[href^=\"/download/\"]")?.GetAttribute("href")?.TrimStart('/'));
                    var details = new Uri(SiteLink + row.QuerySelector("td:nth-child(2) > div > a[href^=\"/t/\"]")?.GetAttribute("href")?.TrimStart('/'));
                    var seeders = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(8)")?.TextContent);
                    var leechers = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(9)")?.TextContent);
                    var pubDateStr = row.QuerySelector("td:nth-child(2) span[class^=\"elapsedDate\"]")?.GetAttribute("title")?.Replace(" at", "");
                    var cat = row.QuerySelector("td:nth-child(1) a")?.GetAttribute("href")?.Split('/').Last();
                    var downloadVolumeFactor = row.QuerySelector("td:nth-child(2) span:contains(\"[Freeleech]\")") != null ? 0 : 1;

                    var release = new ReleaseInfo
                    {
                        Guid = link,
                        Link = link,
                        Details = details,
                        Title = title,
                        Category = MapTrackerCatToNewznab(cat),
                        PublishDate = DateTime.ParseExact(pubDateStr, "dddd, MMMM d, yyyy h:mmtt", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
                        Size = ParseUtil.GetBytes(row.QuerySelector("td:nth-child(6)")?.TextContent),
                        Grabs = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(7)")?.TextContent),
                        Seeders = seeders,
                        Peers = seeders + leechers,
                        DownloadVolumeFactor = downloadVolumeFactor,
                        UploadVolumeFactor = 1,
                        MinimumRatio = 1,
                        MinimumSeedTime = 259200 // 72 hours
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

        private static string CleanTitle(string title)
        {
            title = Regex.Replace(title, @"\[REQ(UEST)?\]", string.Empty, RegexOptions.Compiled | RegexOptions.IgnoreCase);

            return title.Trim(' ', '.');
        }
    }
}
