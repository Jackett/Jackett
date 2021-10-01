using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
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
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class TorrentSeeds : BaseWebIndexer
    {
        private string LoginUrl => SiteLink + "takelogin.php";
        private string CaptchaUrl => SiteLink + "simpleCaptcha.php?numImages=1";
        private string SearchUrl => SiteLink + "browse_elastic.php";

        private new ConfigurationDataBasicLoginWithRSSAndDisplay configData => (ConfigurationDataBasicLoginWithRSSAndDisplay)base.configData;

        public TorrentSeeds(IIndexerConfigurationService configService, Utils.Clients.WebClient wc, Logger l,
            IProtectionService ps, ICacheService cs)
            : base(id: "torrentseeds",
                   name: "TorrentSeeds",
                   description: "TorrentSeeds is a Private site for MOVIES / TV / GENERAL",
                   link: "https://torrentseeds.org/",
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
                   configData: new ConfigurationDataBasicLoginWithRSSAndDisplay("For best results, change the <b>Torrents per page:</b> setting to <b>100</b> on your account profile."))
        {
            Encoding = Encoding.UTF8;
            Language = "en-US";
            Type = "private";

            // NOTE: Tracker Category Description must match Type/Category in details page!
            AddCategoryMapping(37, TorznabCatType.TVAnime, "Anime/HD");
            AddCategoryMapping(9, TorznabCatType.TVAnime, "Anime/SD");
            AddCategoryMapping(72, TorznabCatType.TVAnime, "Anime/UHD");
            AddCategoryMapping(13, TorznabCatType.PC0day, "Apps/0DAY");
            AddCategoryMapping(27, TorznabCatType.Books, "Apps/Bookware");
            AddCategoryMapping(1, TorznabCatType.PCISO, "Apps/ISO");
            AddCategoryMapping(73, TorznabCatType.AudioAudiobook, "Music/Audiobooks");
            AddCategoryMapping(47, TorznabCatType.ConsoleOther, "Console/NSW");
            AddCategoryMapping(8, TorznabCatType.ConsolePS3, "Console/PS3");
            AddCategoryMapping(30, TorznabCatType.ConsolePS4, "Console/PS4");
            AddCategoryMapping(71, TorznabCatType.ConsolePS4, "Console/PS5");
            AddCategoryMapping(7, TorznabCatType.ConsolePSP, "Console/PSP");
            AddCategoryMapping(70, TorznabCatType.ConsolePSVita, "Console/PSV");
            AddCategoryMapping(16, TorznabCatType.ConsoleWii, "Console/WII");
            AddCategoryMapping(29, TorznabCatType.ConsoleWiiU, "Console/WIIU");
            AddCategoryMapping(17, TorznabCatType.ConsoleXBox360, "Console/XBOX360");
            AddCategoryMapping(32, TorznabCatType.BooksEBook, "E-books");
            AddCategoryMapping(63, TorznabCatType.ConsoleOther, "Games/DOX");
            AddCategoryMapping(2, TorznabCatType.PCGames, "Games/ISO");
            AddCategoryMapping(12, TorznabCatType.PCGames, "Games/PC Rips");
            AddCategoryMapping(31, TorznabCatType.MoviesBluRay, "Movies/Bluray");
            AddCategoryMapping(50, TorznabCatType.MoviesBluRay, "Movies/Bluray-UHD");
            AddCategoryMapping(3, TorznabCatType.MoviesDVD, "Movies/DVDR");
            AddCategoryMapping(69, TorznabCatType.MoviesForeign, "Movies/DVDR-Foreign");
            AddCategoryMapping(19, TorznabCatType.MoviesHD, "Movies/HD");
            AddCategoryMapping(39, TorznabCatType.MoviesForeign, "Movies/HD-Foreign");
            AddCategoryMapping(74, TorznabCatType.MoviesHD, "Movies/Remuxes");
            AddCategoryMapping(25, TorznabCatType.MoviesSD, "Movies/SD");
            AddCategoryMapping(62, TorznabCatType.MoviesForeign, "Movies/SD-Foreign");
            AddCategoryMapping(49, TorznabCatType.MoviesUHD, "Movies/UHD");
            AddCategoryMapping(76, TorznabCatType.MoviesForeign, "Movies/UHD-Foreign");
            AddCategoryMapping(33, TorznabCatType.AudioLossless, "Music/FLAC");
            AddCategoryMapping(89, TorznabCatType.AudioVideo, "Music/MBluRay");
            AddCategoryMapping(28, TorznabCatType.AudioVideo, "Music/MBluRay-Rips");
            AddCategoryMapping(34, TorznabCatType.AudioVideo, "Music/MDVDR");
            AddCategoryMapping(4, TorznabCatType.AudioMP3, "Music/MP3");
            AddCategoryMapping(20, TorznabCatType.AudioVideo, "Music/MVID");
            AddCategoryMapping(77, TorznabCatType.TVAnime, "Anime/Packs");
            AddCategoryMapping(78, TorznabCatType.BooksEBook, "Books/Packs");
            AddCategoryMapping(80, TorznabCatType.MoviesHD, "Movies/HD-Packs");
            AddCategoryMapping(81, TorznabCatType.MoviesHD, "Movies/Remux-Packs");
            AddCategoryMapping(79, TorznabCatType.MoviesSD, "Movies/SD-Packs");
            AddCategoryMapping(68, TorznabCatType.Audio, "Music/Packs");
            AddCategoryMapping(67, TorznabCatType.TVHD, "TV/HD-Packs");
            AddCategoryMapping(82, TorznabCatType.TVHD, "TV/Remux-Packs");
            AddCategoryMapping(65, TorznabCatType.TVSD, "TV/SD-Packs");
            AddCategoryMapping(84, TorznabCatType.TVUHD, "TV/UHD-Packs");
            AddCategoryMapping(85, TorznabCatType.XXX, "XXX/Packs");
            AddCategoryMapping(23, TorznabCatType.TVSD, "TV/DVDR");
            AddCategoryMapping(26, TorznabCatType.TVHD, "TV/HD");
            AddCategoryMapping(64, TorznabCatType.TVForeign, "TV/HD-Foreign");
            AddCategoryMapping(11, TorznabCatType.TVHD, "TV/HD-Retail");
            AddCategoryMapping(36, TorznabCatType.TVSport, "TV/HD-Sport");
            AddCategoryMapping(18, TorznabCatType.TVSD, "TV/SD");
            AddCategoryMapping(86, TorznabCatType.TVForeign, "TV/SD-Foreign");
            AddCategoryMapping(24, TorznabCatType.TVSD, "TV/SD-Retail");
            AddCategoryMapping(35, TorznabCatType.TVSport, "TV/SD-Sport");
            AddCategoryMapping(61, TorznabCatType.TVUHD, "TV/UHD");
            AddCategoryMapping(87, TorznabCatType.TVForeign, "TV/UHD-Foreign");
            AddCategoryMapping(53, TorznabCatType.XXX, "XXX/HD");
            AddCategoryMapping(88, TorznabCatType.XXXImageSet, "XXX/Image-Sets");
            AddCategoryMapping(57, TorznabCatType.XXX, "XXX/Paysite");
            AddCategoryMapping(6, TorznabCatType.XXXSD, "XXX/SD");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            CookieHeader = ""; // clear old cookies

            var result1 = await RequestWithCookiesAsync(CaptchaUrl);
            var json1 = JObject.Parse(result1.ContentString);
            var captchaSelection = json1["images"][0]["hash"];

            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "submitme", "X" },
                { "captchaSelection", (string)captchaSelection }
            };

            var result2 = await RequestLoginAndFollowRedirect(LoginUrl, pairs, result1.Cookies, true, null, null, true);

            await ConfigureIfOK(result2.Cookies, result2.ContentString.Contains("logout.php"), () =>
                                    throw new ExceptionWithConfigData("Login Failed", configData));
            return IndexerConfigurationStatus.RequiresTesting;

        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            // remove operator characters
            var cleanSearchString = Regex.Replace(query.GetQueryString().Trim(), "[ _.+-]+", " ", RegexOptions.Compiled);

            var searchUrl = SearchUrl;
            var queryCollection = new NameValueCollection
            {
                { "search_in", "name" },
                { "search_mode", "all" },
                { "order_by", "added" },
                { "order_way", "desc" }
            };
            if (!string.IsNullOrWhiteSpace(cleanSearchString))
                queryCollection.Add("query", cleanSearchString);
            foreach (var cat in MapTorznabCapsToTrackers(query))
                queryCollection.Add($"cat[{cat}]", "1");

            searchUrl += "?" + queryCollection.GetQueryString();
            var response = await RequestWithCookiesAndRetryAsync(searchUrl);

            // handle cookie expiration
            var results = response.ContentString;
            if ((response.IsRedirect && response.RedirectingTo.Contains("/login.php?")) ||
                (!response.IsRedirect && !results.Contains("/logout.php?")))
            {
                await ApplyConfiguration(null); // re-login
                response = await RequestWithCookiesAndRetryAsync(searchUrl);
            }

            // handle single entries
            if (response.IsRedirect)
            {
                var detailsLink = new Uri(response.RedirectingTo);
                await FollowIfRedirect(response, accumulateCookies: true);
                return ParseSingleResult(response, detailsLink);
            }

            return ParseMultiResult(response);
        }

        private List<ReleaseInfo> ParseSingleResult(WebResult response, Uri detailsLink)
        {
            var releases = new List<ReleaseInfo>();
            var results = response.ContentString;

            try
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(results);
                var content = dom.QuerySelector("tbody:has(script)");
                if (content == null)
                    return releases; // no results
                var release = new ReleaseInfo();
                release.MinimumRatio = 1;
                release.MinimumSeedTime = 72 * 60 * 60;
                var catStr = content.QuerySelector("tr:has(td.heading:contains(\"Type\"))").Children[1].TextContent;
                release.Category = MapTrackerCatDescToNewznab(catStr);
                var qLink = content.QuerySelector("tr:has(td.heading:contains(\"Download\"))")
                                   .QuerySelector("a[href*=\"download.php?torrent=\"]");
                release.Link = new Uri(SiteLink + qLink.GetAttribute("href"));
                release.Title = dom.QuerySelector("h1").TextContent.Trim();
                release.Details = detailsLink;
                release.Guid = detailsLink;
                var qSize = content.QuerySelector("tr:has(td.heading:contains(\"Size\"))").Children[1].TextContent
                                   .Split('(')[0].Trim();
                release.Size = ReleaseInfo.GetBytes(qSize);
                var peerStats = content.QuerySelector("tr:has(td:has(a[href^=\"./peerlist_xbt.php?id=\"]))").Children[1]
                                       .TextContent.Split(',');
                var qSeeders = peerStats[0].Replace(" seeder(s)", "").Trim();
                var qLeechers = peerStats[1].Split('=')[0].Replace(" leecher(s) ", "").Trim();
                release.Seeders = ParseUtil.CoerceInt(qSeeders);
                release.Peers = ParseUtil.CoerceInt(qLeechers) + release.Seeders;
                var rawDateStr = content.QuerySelector("tr:has(td.heading:contains(\"Added\"))").Children[1].TextContent;
                var dateUpped = DateTimeUtil.FromUnknown(rawDateStr.Replace(",", string.Empty));

                // Mar 4 2020, 05:47 AM
                release.PublishDate = dateUpped.ToLocalTime();
                var qGrabs = content.QuerySelector("tr:has(td.heading:contains(\"Snatched\"))").Children[1];
                release.Grabs = ParseUtil.CoerceInt(qGrabs.TextContent.Replace(" time(s)", ""));
                var qFiles = content.QuerySelector("tr:has(td.heading:has(a[href^=\"./filelist.php?id=\"]))").Children[1];
                release.Files = ParseUtil.CoerceInt(qFiles.TextContent.Replace(" files", ""));
                var qRatio = content.QuerySelector("tr:has(td.heading:contains(\"Ratio After Download\"))").Children[1];
                release.DownloadVolumeFactor = qRatio.QuerySelector("del") != null ? 0 : 1;
                release.UploadVolumeFactor = 1;
                releases.Add(release);
            }
            catch (Exception ex)
            {
                OnParseError(results, ex);
            }

            return releases;
        }

        private List<ReleaseInfo> ParseMultiResult(WebResult response)
        {
            var releases = new List<ReleaseInfo>();
            var results = response.ContentString;

            try
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(results);
                var rows = dom.QuerySelectorAll("table.table-bordered > tbody > tr[class*=\"torrent_row_\"]");
                foreach (var row in rows)
                {
                    var release = new ReleaseInfo();
                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 72 * 60 * 60;
                    var qCatLink = row.QuerySelector("a[href^=\"/browse_elastic.php?cat=\"]");
                    var catStr = qCatLink.GetAttribute("href").Split('=')[1];
                    release.Category = MapTrackerCatToNewznab(catStr);
                    var qDetailsLink = row.QuerySelector("a[href^=\"/details.php?id=\"]");
                    var qDetailsTitle = row.QuerySelector("td:has(a[href^=\"/details.php?id=\"]) b");
                    release.Title = qDetailsTitle.TextContent.Trim();
                    var qDlLink = row.QuerySelector("a[href^=\"/download.php?torrent=\"]");

                    release.Link = new Uri(SiteLink + qDlLink.GetAttribute("href").TrimStart('/'));
                    release.Details = new Uri(SiteLink + qDetailsLink.GetAttribute("href").TrimStart('/'));
                    release.Guid = release.Details;

                    var qColumns = row.QuerySelectorAll("td");
                    release.Files = ParseUtil.CoerceInt(qColumns[3].TextContent);
                    release.PublishDate = DateTimeUtil.FromUnknown(qColumns[5].TextContent);
                    release.Size = ReleaseInfo.GetBytes(qColumns[6].TextContent);
                    release.Grabs = ParseUtil.CoerceInt(qColumns[7].TextContent.Replace("Times", ""));
                    release.Seeders = ParseUtil.CoerceInt(qColumns[8].TextContent);
                    release.Peers = ParseUtil.CoerceInt(qColumns[9].TextContent) + release.Seeders;

                    var qImdb = row.QuerySelector("a[href*=\"www.imdb.com\"]");
                    if (qImdb != null)
                    {
                        var deRefUrl = qImdb.GetAttribute("href");
                        release.Imdb = ParseUtil.GetImdbID(WebUtility.UrlDecode(deRefUrl).Split('/').Last());
                    }

                    release.DownloadVolumeFactor = row.QuerySelector("span.freeleech") != null ? 0 : 1;
                    release.UploadVolumeFactor = 1;
                    releases.Add(release);
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
