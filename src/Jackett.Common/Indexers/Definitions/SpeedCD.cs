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
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class SpeedCD : IndexerBase
    {
        public override string Id => "speedcd";
        public override string[] Replaces => new[] { "speedcdcookie" };
        public override string Name => "Speed.cd";
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

        private string SearchUrl => SiteLink + "browse/";

        private new ConfigurationDataSpeedCD configData => (ConfigurationDataSpeedCD)base.configData;

        public SpeedCD(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataSpeedCD(
                       @"Speed.Cd have increased their security. If you are having problems please check the security tab in your
                    Speed.Cd profile. Eg. Geo Locking, your seedbox may be in a different country to the one where you login via your
                    web browser.<br><br>For best results, change the 'Torrents per page' setting to 100 in<br>'Profile Settings > Torrents'.
                    <br><br>This site may use Cloudflare DDoS Protection, therefore Jackett requires <a
                    href='https://github.com/Jackett/Jackett#configuring-flaresolverr' target='_blank'>FlareSolverr</a> to access it."))
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
            await DoLogin();
            return IndexerConfigurationStatus.RequiresTesting;
        }

        private async Task DoLogin()
        {
            // first request with username
            var result = await RequestLoginAndFollowRedirect(
                $"{SiteLink}checkpoint/API",
                new Dictionary<string, string>
                {
                    { "username", configData.Username.Value }
                },
                null,
                true,
                null,
                SiteLink);

            var tokenRegex = new Regex(@"name=\\""a\\"" value=\\""([^""]+)\\""");
            var matches = tokenRegex.Match(result.ContentString);
            if (!matches.Success)
            {
                throw new Exception("Error parsing the login form");
            }

            var token = matches.Groups[1].Value;

            // second request with token and password
            result = await RequestLoginAndFollowRedirect(
                $"{SiteLink}checkpoint/",
                new Dictionary<string, string>
                {
                    { "pwd", configData.Password.Value },
                    { "a", token }
                },
                result.Cookies,
                true,
                null,
                SiteLink);

            await ConfigureIfOK(result.Cookies, result.ContentString?.Contains("/browse.php") == true, () =>
            {
                var parser = new HtmlParser();
                using var dom = parser.ParseDocument(result.ContentString);
                var errorMessage = dom.QuerySelector("h5")?.TextContent;

                if (result.ContentString.Contains("Wrong Captcha!"))
                {
                    errorMessage = "Captcha required due to a failed login attempt. Login via a browser to whitelist your IP and then reconfigure Jackett.";
                }

                throw new Exception(errorMessage ?? "Login failed.");
            });
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            // the order of the params is important!
            var qc = new List<string>();

            var catList = MapTorznabCapsToTrackers(query);
            foreach (var cat in catList)
            {
                qc.Add(cat);
            }

            if (configData.Freeleech.Value)
            {
                qc.Add("freeleech");
            }

            if (configData.ExcludeArchives.Value)
            {
                qc.Add("norar");
            }

            if (query.IsImdbQuery)
            {
                var term = query.ImdbID;

                if (!string.IsNullOrWhiteSpace(query.GetEpisodeSearchString()))
                {
                    term += $" {query.GetEpisodeSearchString()}";

                    if (query.Season > 0 && string.IsNullOrEmpty(query.Episode))
                    {
                        term += "*";
                    }
                }

                qc.Add("deep");
                qc.Add("q");
                qc.Add(WebUtilityHelpers.UrlEncode(term.Trim(), Encoding));
            }
            else
            {
                var term = query.GetQueryString();

                if (!string.IsNullOrWhiteSpace(query.GetEpisodeSearchString()) && query.Season > 0 && string.IsNullOrEmpty(query.Episode))
                {
                    term += "*";
                }

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
            var response = await RequestWithCookiesAndRetryAsync(searchUrl);

            if (!response.ContentString.Contains("/logout.php")) // re-login
            {
                await DoLogin();
                response = await RequestWithCookiesAndRetryAsync(searchUrl);
            }

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
