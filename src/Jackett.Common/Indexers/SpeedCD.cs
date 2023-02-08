using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
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

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class SpeedCD : BaseWebIndexer
    {
        private string LoginUrl1 => SiteLink + "checkpoint/API";
        private string LoginUrl2 => SiteLink + "checkpoint/";
        private string SearchUrl => SiteLink + "browse/";

        public override string[] AlternativeSiteLinks { get; protected set; } = {
            "https://speed.cd/",
            "https://speed.click/",
            "https://speeders.me/"
        };

        private new ConfigurationDataSpeedCD configData => (ConfigurationDataSpeedCD)base.configData;

        public SpeedCD(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "speedcd",
                   name: "Speed.cd",
                   description: "Your home now!",
                   link: "https://speed.cd/",
                   caps: new TorznabCapabilities
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
                   },
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataSpeedCD(
                       @"Speed.Cd have increased their security. If you are having problems please check the security tab
                    in your Speed.Cd profile. Eg. Geo Locking, your seedbox may be in a different country to the one where you login via your
                    web browser.<br><br>For best results, change the 'Torrents per page' setting to 100 in 'Profile Settings > Torrents'."))
        {
            Encoding = Encoding.UTF8;
            Language = "en-US";
            Type = "private";

            AddCategoryMapping(1, TorznabCatType.MoviesOther, "Movies/XviD");
            AddCategoryMapping(42, TorznabCatType.Movies, "Movies/Packs");
            AddCategoryMapping(32, TorznabCatType.Movies, "Movies/Kids");
            AddCategoryMapping(43, TorznabCatType.MoviesHD, "Movies/HD");
            AddCategoryMapping(47, TorznabCatType.Movies, "Movies/DiVERSiTY");
            AddCategoryMapping(28, TorznabCatType.MoviesBluRay, "Movies/B-Ray");
            AddCategoryMapping(48, TorznabCatType.Movies3D, "Movies/3D");
            AddCategoryMapping(40, TorznabCatType.MoviesDVD, "Movies/DVD-R");
            AddCategoryMapping(56, TorznabCatType.Movies, "Movies/Anime");
            AddCategoryMapping(50, TorznabCatType.TVSport, "TV/Sports");
            AddCategoryMapping(52, TorznabCatType.TVHD, "TV/B-Ray");
            AddCategoryMapping(53, TorznabCatType.TVSD, "TV/DVD-R");
            AddCategoryMapping(41, TorznabCatType.TV, "TV/Packs");
            AddCategoryMapping(55, TorznabCatType.TV, "TV/Kids");
            AddCategoryMapping(57, TorznabCatType.TV, "TV/DiVERSiTY");
            AddCategoryMapping(49, TorznabCatType.TVHD, "TV/HD");
            AddCategoryMapping(2, TorznabCatType.TVSD, "TV/Episodes");
            AddCategoryMapping(30, TorznabCatType.TVAnime, "TV/Anime");
            AddCategoryMapping(25, TorznabCatType.PCISO, "Games/PC ISO");
            AddCategoryMapping(39, TorznabCatType.ConsoleWii, "Games/Wii");
            AddCategoryMapping(45, TorznabCatType.ConsolePS3, "Games/PS3");
            AddCategoryMapping(35, TorznabCatType.Console, "Games/Nintendo");
            AddCategoryMapping(33, TorznabCatType.ConsoleXBox360, "Games/XboX360");
            AddCategoryMapping(46, TorznabCatType.PCMobileOther, "Mobile");
            AddCategoryMapping(24, TorznabCatType.PC0day, "Apps/0DAY");
            AddCategoryMapping(51, TorznabCatType.PCMac, "Mac");
            AddCategoryMapping(54, TorznabCatType.Books, "Educational");
            AddCategoryMapping(27, TorznabCatType.Books, "Books-Mags");
            AddCategoryMapping(26, TorznabCatType.Audio, "Music/Audio");
            AddCategoryMapping(3, TorznabCatType.Audio, "Music/Flac");
            AddCategoryMapping(44, TorznabCatType.Audio, "Music/Pack");
            AddCategoryMapping(29, TorznabCatType.AudioVideo, "Music/Video");
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
            var pairs = new Dictionary<string, string>
            {
                { "username", configData.Username.Value }
            };
            var result = await RequestLoginAndFollowRedirect(LoginUrl1, pairs, null, true, null, SiteLink);
            var tokenRegex = new Regex(@"name=\\""a\\"" value=\\""([^""]+)\\""");
            var matches = tokenRegex.Match(result.ContentString);
            if (!matches.Success)
                throw new Exception("Error parsing the login form");
            var token = matches.Groups[1].Value;

            // second request with token and password
            pairs = new Dictionary<string, string>
            {
                { "pwd", configData.Password.Value },
                { "a", token }
            };
            result = await RequestLoginAndFollowRedirect(LoginUrl2, pairs, result.Cookies, true, null, SiteLink);

            await ConfigureIfOK(result.Cookies, result.ContentString?.Contains("/browse.php") == true, () =>
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(result.ContentString);
                var errorMessage = dom.QuerySelector("h5")?.TextContent;

                if (result.ContentString.Contains("Wrong Captcha!"))
                    errorMessage = "Captcha required due to a failed login attempt. Login via a browser to whitelist your IP and then reconfigure Jackett.";

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
                qc.Add(cat);

            if (configData.Freeleech.Value)
                qc.Add("freeleech");

            if (configData.ExcludeArchives.Value)
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
                var dom = parser.ParseDocument(response.ContentString);

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
                        Size = ReleaseInfo.GetBytes(row.QuerySelector("td:nth-child(6)")?.TextContent),
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
