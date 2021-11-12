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
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class Hebits : BaseWebIndexer
    {
        private string SearchUrl => SiteLink + "torrents.php?order_way=desc&order_by=time";
        private new ConfigurationDataCookie configData => (ConfigurationDataCookie)base.configData;

        public Hebits(IIndexerConfigurationService configService, Utils.Clients.WebClient wc, Logger l,
            IProtectionService ps, ICacheService cs)
            : base(id: "hebits",
                   name: "Hebits",
                   description: "The Israeli Tracker",
                   link: "https://hebits.net/",
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
                   configData: new ConfigurationDataCookie())
        {
            Encoding = Encoding.GetEncoding("UTF-8");
            Language = "he-IL";
            Type = "private";

            AddCategoryMapping(1, TorznabCatType.Movies, "סרטים (Movies)");
            AddCategoryMapping(2, TorznabCatType.TV, "סדרות (TV)");
            AddCategoryMapping(3, TorznabCatType.TVOther, "הצגות והופעות (Theater)");
            AddCategoryMapping(4, TorznabCatType.PC, "תוכנות (Apps)");
            AddCategoryMapping(5, TorznabCatType.Console, "משחקים (Games)");
            AddCategoryMapping(6, TorznabCatType.Audio, "מוזיקה (Music)");
            AddCategoryMapping(7, TorznabCatType.Books, "ספרים (Books)");
            AddCategoryMapping(8, TorznabCatType.MoviesOther, "חבילות סרטים (Movies Packs)");
            AddCategoryMapping(9, TorznabCatType.XXX, "פורנו (Porn)");
            AddCategoryMapping(10, TorznabCatType.Other, "שונות (Other)");
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
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();
            var searchUrl = SearchUrl;
            if (!string.IsNullOrWhiteSpace(searchString))
                searchUrl += "&action=advanced&searchsubmit=1&filelist=" + WebUtilityHelpers.UrlEncode(searchString, Encoding);
            var cats = MapTorznabCapsToTrackers(query);
            if (cats.Count > 0)
                searchUrl = cats.Aggregate(searchUrl, (url, cat) => $"{url}&filter_cat[{cat}]=1");
            var response = await RequestWithCookiesAsync(searchUrl);
            try
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(response.ContentString);
                var rows = dom.QuerySelectorAll("table#torrent_table > tbody > tr.torrent");
                foreach (var row in rows)
                {
                    var release = new ReleaseInfo
                    {
                        MinimumRatio = 1.0,
                        MinimumSeedTime = 604800 // 168 hours
                    };
                    var qCat = row.QuerySelector("div[class*=\"cats_\"]");
                    var catStr = qCat.GetAttribute("class").Split('_')[1];
                    release.Category = catStr switch
                    {
                        "movies" => MapTrackerCatToNewznab("1"),
                        "tv" => MapTrackerCatToNewznab("2"),
                        "theater" => MapTrackerCatToNewznab("3"),
                        "software" => MapTrackerCatToNewznab("4"),
                        "games" => MapTrackerCatToNewznab("5"),
                        "music" => MapTrackerCatToNewznab("6"),
                        "books" => MapTrackerCatToNewznab("7"),
                        "moviespacks" => MapTrackerCatToNewznab("8"),
                        "porno" => MapTrackerCatToNewznab("9"),
                        "other" => MapTrackerCatToNewznab("10"),
                        _ => throw new Exception("Error parsing category! Unknown cat=" + catStr),
                    };
                    var qTitle = row.QuerySelector("div.torrent_info");
                    release.Title = qTitle.TextContent.Trim();
                    var qDetailsLink = row.QuerySelector("a.torrent_name");
                    // I don't understand, I correctly build the poster as
                    // https://hebits.net/images/oRbJr/3776T8DeNsKbyUM3tsV0LBY_v_JfdfmkhShh_LguTP.jpg
                    // yet the dashboard shows a broken icon symbol! No idea why this does not work.
                    //if (!string.IsNullOrEmpty(qDetailsLink.GetAttribute("data-cover")))
                    //{
                    //    release.Poster = new Uri(SiteLink.TrimEnd('/') + qDetailsLink.GetAttribute("data-cover"));
                    //    logger.Debug("poster=" + release.Poster);
                    //}
                    release.Details = new Uri(SiteLink + qDetailsLink.GetAttribute("href"));
                    release.Link = new Uri(SiteLink + row.QuerySelector("a[href^=\"torrents.php?action=download&id=\"]").GetAttribute("href"));
                    release.Guid = release.Link;
                    var qDate = row.QuerySelector("span.time").GetAttribute("title");
                    release.PublishDate = DateTime.ParseExact(qDate, "dd/MM/yyyy, HH:mm", CultureInfo.InvariantCulture);
                    var qSize = row.QuerySelector("td:nth-last-child(4)").TextContent.Trim();
                    release.Size = ReleaseInfo.GetBytes(qSize);
                    release.Seeders = ParseUtil.CoerceInt(row.QuerySelector("td:nth-last-child(2)").TextContent.Trim());
                    release.Peers = release.Seeders + ParseUtil.CoerceInt(row.QuerySelector("td:nth-last-child(1)").TextContent.Trim());
                    release.Files = ParseUtil.CoerceInt(row.QuerySelector("td:nth-last-child(6)").TextContent.Trim());
                    release.Grabs = ParseUtil.CoerceInt(row.QuerySelector("td:nth-last-child(3)").TextContent.Trim());
                    release.DownloadVolumeFactor = release.Title.Contains("פריליץ")
                        ? 0
                        : release.Title.Contains("חצי פריליץ")
                            ? 0.5
                            : release.Title.Contains("75% פריליץ")
                                ? 0.25
                                : 1;
                    release.UploadVolumeFactor = release.Title.Contains("העלאה משולשת")
                        ? 3
                        : release.Title.Contains("העלאה כפולה")
                            ? 2
                            : 1;
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
