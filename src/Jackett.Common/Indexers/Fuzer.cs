using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Jackett.Common.Helpers;
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
    public class Fuzer : BaseWebIndexer
    {
        public override string[] LegacySiteLinks { get; protected set; } =
        {
            "https://fuzer.me/"
        };

        private string SearchUrl => SiteLink + "browse.php";

        private new ConfigurationDataCookie configData => (ConfigurationDataCookie)base.configData;

        public Fuzer(IIndexerConfigurationService configService, WebClient w, Logger l, IProtectionService ps)
            : base(id: "fuzer",
                   name: "Fuzer",
                   description: "Fuzer is a private torrent website with israeli torrents.",
                   link: "https://www.fuzer.me/",
                   caps: new TorznabCapabilities
                   {
                       TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
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
                   client: w,
                   logger: l,
                   p: ps,
                   configData: new ConfigurationDataCookie())
        {
            Encoding = Encoding.GetEncoding("windows-1255");
            Language = "he-il";
            Type = "private";

            // סרטים
            AddCategoryMapping(7, TorznabCatType.MoviesSD, "סרטים");
            AddCategoryMapping(9, TorznabCatType.MoviesHD, "סרטים HD");
            AddCategoryMapping(97, TorznabCatType.MoviesUHD, "סרטים UHD");
            AddCategoryMapping(58, TorznabCatType.MoviesDVD, "סרטים DVD-R");
            AddCategoryMapping(59, TorznabCatType.MoviesSD, "סרטי BDRIP-BRRip");
            AddCategoryMapping(60, TorznabCatType.MoviesSD, "סרטים ישראליים");
            AddCategoryMapping(61, TorznabCatType.MoviesHD, "סרטים ישראליים HD");
            AddCategoryMapping(83, TorznabCatType.MoviesOther, "סרטים מדובבים");

            // סדרות
            AddCategoryMapping(8, TorznabCatType.TVSD, "סדרות");
            AddCategoryMapping(10, TorznabCatType.TVHD, "סדרות HD");
            AddCategoryMapping(62, TorznabCatType.TVSD, "סדרות ישראליות");
            AddCategoryMapping(63, TorznabCatType.TVHD, "סדרות ישראליות HD");
            AddCategoryMapping(84, TorznabCatType.TVOther, "סדרות מדובבות");

            // מוזיקה
            AddCategoryMapping(14, TorznabCatType.Audio, "מוזיקה עולמית");
            AddCategoryMapping(66, TorznabCatType.Audio, "מוזיקה ישראלית");
            AddCategoryMapping(67, TorznabCatType.AudioMP3, "FLAC");
            AddCategoryMapping(68, TorznabCatType.Audio, "פסקולים");

            // משחקים
            AddCategoryMapping(11, TorznabCatType.PCGames, "משחקים PC");
            AddCategoryMapping(12, TorznabCatType.ConsoleOther, "משחקים PS");
            AddCategoryMapping(55, TorznabCatType.ConsoleXBox, "משחקים XBOX");
            AddCategoryMapping(56, TorznabCatType.ConsoleWii, "משחקים WII");
            AddCategoryMapping(57, TorznabCatType.PCMobileOther, "משחקי קונסולות ניידות");

            // תוכנה
            AddCategoryMapping(13, TorznabCatType.PCMobileAndroid, "אפליקציות לאנדרואיד");
            AddCategoryMapping(15, TorznabCatType.PC0day, "תוכנות PC");
            AddCategoryMapping(70, TorznabCatType.PCMobileiOS, "אפליקציות לאייפון");
            AddCategoryMapping(71, TorznabCatType.PCMac, "תוכנות MAC");

            // שונות
            AddCategoryMapping(16, TorznabCatType.XXX, "למבוגרים בלבד");
            AddCategoryMapping(17, TorznabCatType.Other, "שונות");
            AddCategoryMapping(64, TorznabCatType.Other, "ספורט");
            AddCategoryMapping(65, TorznabCatType.Other, "אנימה");
            AddCategoryMapping(69, TorznabCatType.Books, "Ebooks");

            // FuzePacks
            AddCategoryMapping(72, TorznabCatType.Console, "משחקים");
            AddCategoryMapping(73, TorznabCatType.Movies, "סרטים");
            AddCategoryMapping(74, TorznabCatType.PC, "תוכנות");
            AddCategoryMapping(75, TorznabCatType.Audio, "שירים");
            AddCategoryMapping(76, TorznabCatType.TV, "סדרות");
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
            var results = await PerformRegularQueryAsync(query);
            if (!results.Any() && !query.IsImdbQuery)
                return await PerformHebrewQueryAsync(query);
            return results;
        }

        private async Task<IEnumerable<ReleaseInfo>> PerformHebrewQueryAsync(TorznabQuery query)
        {
            var name = await GetHebNameAsync(query.SearchTerm);
            if (string.IsNullOrEmpty(name))
                return new List<ReleaseInfo>();
            return await PerformRegularQueryAsync(query, name);
        }

        private async Task<IEnumerable<ReleaseInfo>> PerformRegularQueryAsync(TorznabQuery query, string hebName = null)
        {
            var releases = new List<ReleaseInfo>();
            var searchUrl = SearchUrl;
            var searchString = query.GetQueryString();
            if (query.IsImdbQuery)
                searchString = query.ImdbID;
            if (hebName != null)
                searchString = hebName + " - עונה " + query.Season + " פרק " + query.Episode;
            searchUrl += "?";
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var strEncoded = WebUtilityHelpers.UrlEncode(searchString, Encoding);
                searchUrl += "&query=" + strEncoded + "&matchquery=any";
            }

            searchUrl = MapTorznabCapsToTrackers(query).Aggregate(searchUrl, (current, cat) => $"{current}&c[]={cat}");
            var data = await RequestWithCookiesAndRetryAsync(searchUrl);
            try
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(data.ContentString);
                var rows = dom.QuerySelectorAll("tr.box_torrent");
                foreach (var row in rows)
                {
                    var release = new ReleaseInfo();
                    var mainTitleLink = row.QuerySelector("div.main_title > a");
                    release.Title = mainTitleLink.GetAttribute("longtitle");
                    if (string.IsNullOrWhiteSpace(release.Title))
                        release.Title = mainTitleLink.TextContent;
                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800; // 48 hours
                    release.Grabs = ParseUtil.CoerceLong(row.QuerySelector("td:nth-child(5)").TextContent.Replace(",", ""));
                    release.Seeders = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(6)").TextContent.Replace(",", ""));
                    release.Peers = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(7)").TextContent.Replace(",", "")) +
                                    release.Seeders;
                    var fullSize = row.QuerySelector("td:nth-child(4)").TextContent;
                    release.Size = ReleaseInfo.GetBytes(fullSize);
                    release.Details = new Uri(SiteLink + row.QuerySelector("a.threadlink[href]").GetAttribute("href"));
                    release.Link = new Uri(SiteLink + row.QuerySelector("a:has(div.dlimg)").GetAttribute("href"));
                    release.Guid = release.Details;
                    //some releases have invalid poster URLs, ignore the posters in this case
                    if (Uri.TryCreate(row.QuerySelector("a[imgsrc]").GetAttribute("imgsrc"),
                                      UriKind.Absolute, out var poster))
                        release.Poster = poster;
                    var dateStringAll = row.QuerySelector("div.up_info2").ChildNodes.Last().TextContent;
                    var dateParts = dateStringAll.Split(' ');
                    var dateString = dateParts[dateParts.Length - 2] + " " + dateParts[dateParts.Length - 1];
                    release.PublishDate = DateTime.ParseExact(dateString, "dd/MM/yy HH:mm", CultureInfo.InvariantCulture);
                    var categoryLink = row.QuerySelector("a[href^=\"/browse.php?cat=\"]").GetAttribute("href");
                    var catid = ParseUtil.GetArgumentFromQueryString(categoryLink, "cat");
                    release.Category = MapTrackerCatToNewznab(catid);
                    if (row.QuerySelector("a[href^=\"?freeleech=1\"]") != null)
                        release.DownloadVolumeFactor = 0;
                    else
                        release.DownloadVolumeFactor = 1;
                    release.UploadVolumeFactor = 1;
                    var subTitle = row.QuerySelector("div.sub_title");
                    var imdbLink = subTitle.QuerySelector("span.imdb-inline > a");
                    if (imdbLink != null)
                        release.Imdb = ParseUtil.GetLongFromString(imdbLink.GetAttribute("href"));
                    release.Description = subTitle.FirstChild.TextContent;
                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(data.ContentString, ex);
            }

            return releases;
        }

        private async Task<string> GetHebNameAsync(string searchTerm)
        {
            var queryString = new NameValueCollection
            {
                {"searchseriesid", ""},
                {"tab", "listseries"},
                {"function", "Search"},
                {"string", searchTerm} // eretz + nehedert
            };
            var site = new UriBuilder
            {
                Scheme = "http",
                Host = "thetvdb.com",
                Path = "index.php",
                Query = queryString.GetQueryString()
            };
            var results = await RequestWithCookiesAsync(site.ToString());
            var parser = new HtmlParser();
            var dom = parser.ParseDocument(results.ContentString);
            var rows = dom.QuerySelectorAll("#listtable > tbody > tr");
            foreach (var row in rows.Skip(1))
            {
                var link = row.QuerySelector("td:nth-child(1) > a");
                if (string.Equals(link.TextContent.Trim(), searchTerm.Trim(), StringComparison.CurrentCultureIgnoreCase))
                {
                    var address = link.GetAttribute("href");
                    if (string.IsNullOrEmpty(address))
                        continue;
                    var realDom = parser.ParseDocument(results.ContentString);
                    return realDom.QuerySelector("#content:nth-child(1) > h1").TextContent;
                }
            }

            return string.Empty;
        }
    }
}
