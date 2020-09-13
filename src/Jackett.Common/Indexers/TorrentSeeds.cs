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
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class TorrentSeeds : BaseWebIndexer
    {
        private string LoginUrl => SiteLink + "takelogin.php";
        private string SearchUrl => SiteLink + "browse_elastic.php";
        private string TokenUrl => SiteLink + "login.php";

        private new ConfigurationDataBasicLoginWithRSSAndDisplay configData => (ConfigurationDataBasicLoginWithRSSAndDisplay)base.configData;

        public TorrentSeeds(IIndexerConfigurationService configService, Utils.Clients.WebClient wc, Logger l, IProtectionService ps) :
            base(id: "torrentseeds",
                 name: "TorrentSeeds",
                 description: "TorrentSeeds is a Private site for MOVIES / TV / GENERAL",
                 link: "https://torrentseeds.org/",
                 caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                 configService: configService,
                 client: wc,
                 logger: l,
                 p: ps,
                 configData: new ConfigurationDataBasicLoginWithRSSAndDisplay())
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";
            AddCategoryMapping(13, TorznabCatType.PC0day, "Apps/0DAY");
            AddCategoryMapping(37, TorznabCatType.TVAnime, "Anime/HD");
            AddCategoryMapping(9, TorznabCatType.TVAnime, "Anime/SD");
            AddCategoryMapping(1, TorznabCatType.PC0day, "Apps");
            AddCategoryMapping(27, TorznabCatType.Books, "APPS/TUTORIALS");
            AddCategoryMapping(32, TorznabCatType.BooksEbook, "EBooks");
            AddCategoryMapping(47, TorznabCatType.ConsoleOther, "Games/NSW");
            AddCategoryMapping(60, TorznabCatType.ConsoleOther, "Games/ATARI");
            AddCategoryMapping(63, TorznabCatType.ConsoleOther, "Games/UPDATES");
            AddCategoryMapping(2, TorznabCatType.PCGames, "Games/PC");
            AddCategoryMapping(8, TorznabCatType.ConsolePS3, "Games/PS3");
            AddCategoryMapping(30, TorznabCatType.ConsolePS4, "Games/PS4");
            AddCategoryMapping(7, TorznabCatType.ConsolePSP, "Games/PSP");
            AddCategoryMapping(16, TorznabCatType.ConsoleWii, "Games/WII");
            AddCategoryMapping(29, TorznabCatType.ConsoleWiiU, "Games/WIIU");
            AddCategoryMapping(17, TorznabCatType.ConsoleXbox360, "Games/XBOX360");
            AddCategoryMapping(50, TorznabCatType.MoviesBluRay, "Movies/Bluray-UHD");
            AddCategoryMapping(31, TorznabCatType.MoviesBluRay, "Movies/COMPLETE-BLURAY");
            AddCategoryMapping(3, TorznabCatType.MoviesDVD, "Movies/DVDR");
            AddCategoryMapping(39, TorznabCatType.MoviesForeign, "Movies/HD-Foreign");
            AddCategoryMapping(62, TorznabCatType.MoviesForeign, "Movies/SD-Foreign");
            AddCategoryMapping(19, TorznabCatType.MoviesHD, "Movies/X264");
            AddCategoryMapping(49, TorznabCatType.MoviesHD, "Movies/X265");
            AddCategoryMapping(25, TorznabCatType.MoviesSD, "Movies/XVID");
            AddCategoryMapping(6, TorznabCatType.XXX, "Movies/XXX");
            AddCategoryMapping(53, TorznabCatType.XXX, "Movies/XXX-HD");
            AddCategoryMapping(57, TorznabCatType.XXX, "Movies/XXX-PAYSITE");
            AddCategoryMapping(55, TorznabCatType.XXX, "Movies/XXX-DVDR");
            AddCategoryMapping(33, TorznabCatType.AudioLossless, "Music/FLAC");
            AddCategoryMapping(28, TorznabCatType.AudioOther, "Music/MBluRay");
            AddCategoryMapping(34, TorznabCatType.AudioOther, "Music/MDVDR");
            AddCategoryMapping(4, TorznabCatType.AudioMP3, "Music/MP3");
            AddCategoryMapping(20, TorznabCatType.AudioVideo, "Music/MVID");
            AddCategoryMapping(38, TorznabCatType.TVAnime, "P2P/ANIME");
            AddCategoryMapping(48, TorznabCatType.PC0day, "P2P/APPS");
            AddCategoryMapping(43, TorznabCatType.MoviesBluRay, "P2P/BLURAY");
            AddCategoryMapping(52, TorznabCatType.MoviesBluRay, "P2P/Bluray-UHD");
            AddCategoryMapping(40, TorznabCatType.MoviesDVD, "P2P/DVDR");
            AddCategoryMapping(46, TorznabCatType.BooksEbook, "P2P/EBOOKS");
            AddCategoryMapping(45, TorznabCatType.PCGames, "P2P/GAMES");
            AddCategoryMapping(42, TorznabCatType.MoviesHD, "P2P/HD-MOVIES");
            AddCategoryMapping(44, TorznabCatType.TVHD, "P2P/TV-HD");
            AddCategoryMapping(51, TorznabCatType.MoviesHD, "P2P/X265");
            AddCategoryMapping(41, TorznabCatType.MoviesSD, "P2P/XVID");
            AddCategoryMapping(35, TorznabCatType.TVSport, "TV/SPORT");
            AddCategoryMapping(36, TorznabCatType.TVSport, "TV/SPORT-HD");
            AddCategoryMapping(11, TorznabCatType.TVHD, "TV/BluRay");
            AddCategoryMapping(23, TorznabCatType.TVSD, "TV/DVDR");
            AddCategoryMapping(24, TorznabCatType.TVSD, "TV/DVDRIP");
            AddCategoryMapping(18, TorznabCatType.TVSD, "TV/SD");
            AddCategoryMapping(26, TorznabCatType.TVHD, "TV/X264");
            AddCategoryMapping(61, TorznabCatType.TVUHD, "TV/2160P");
            AddCategoryMapping(64, TorznabCatType.TVFOREIGN, "TV/X264-FOREIGN");
            AddCategoryMapping(66, TorznabCatType.ConsoleOther, "ARCHIVE/NSW");
            AddCategoryMapping(68, TorznabCatType.Audio, "Music/Packs");
            AddCategoryMapping(67, TorznabCatType.TVHD, "TV-HD/Pack");
            AddCategoryMapping(65, TorznabCatType.TVSD, "TV-SD/Pack");
            AddCategoryMapping(12, TorznabCatType.PCGames, "Games/PC Rips");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var loginPage = await RequestStringWithCookies(TokenUrl);
            var parser = new HtmlParser();
            var dom = parser.ParseDocument(loginPage.Content);
            var token = dom.QuerySelector("form.form-horizontal > span");
            var csrf = token.Children[1].GetAttribute("value");
            var pairs = new Dictionary<string, string>
            {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "perm_ssl", "1" },
                { "returnto", "/" },
                { "csrf_token", csrf }
            };
            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, loginPage.Cookies, true, accumulateCookies: true);
            await ConfigureIfOK(
                result.Cookies, result.Content.Contains("/logout.php?"),
                () =>
                {
                    var errorDom = parser.ParseDocument(result.Content);
                    var errorMessage = errorDom.QuerySelector("td.colhead2").InnerHtml;
                    throw new ExceptionWithConfigData(errorMessage, configData);
                });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

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
            var response = await RequestStringWithCookiesAndRetry(searchUrl);
            var results = response.Content;
            if (!results.Contains("/logout.php?"))
            {
                await ApplyConfiguration(null);
                response = await RequestStringWithCookiesAndRetry(searchUrl);
                results = response.Content;
            }

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
                    release.Comments = new Uri(SiteLink + qDetailsLink.GetAttribute("href").TrimStart('/'));
                    release.Guid = release.Comments;

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
