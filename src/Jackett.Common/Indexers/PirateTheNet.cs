using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
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
    public class PirateTheNet : BaseWebIndexer
    {
        private string SearchUrl => SiteLink + "torrentsutils.php";
        private string LoginUrl => SiteLink + "takelogin.php";
        private string CaptchaUrl => SiteLink + "simpleCaptcha.php?numImages=1";

        private new ConfigurationDataBasicLoginWithRSSAndDisplay configData
        {
            get => (ConfigurationDataBasicLoginWithRSSAndDisplay)base.configData;
            set => base.configData = value;
        }

        public PirateTheNet(IIndexerConfigurationService configService, WebClient w, Logger l,
            IProtectionService ps, ICacheService cs)
            : base(id: "piratethenet",
                   name: "PirateTheNet",
                   description: "A movie tracker",
                   link: "http://piratethenet.org/",
                   caps: new TorznabCapabilities
                   {
                       MovieSearchParams = new List<MovieSearchParam>
                       {
                           MovieSearchParam.Q, MovieSearchParam.ImdbId
                       }
                   },
                   configService: configService,
                   client: w,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataBasicLoginWithRSSAndDisplay("Only the results from the first search result page are shown, adjust your profile settings to show the maximum."))
        {
            Encoding = Encoding.UTF8;
            Language = "en-US";
            Type = "private";

            AddCategoryMapping("1080P", TorznabCatType.MoviesHD, "1080P");
            AddCategoryMapping("2160P", TorznabCatType.MoviesHD, "2160P");
            AddCategoryMapping("720P", TorznabCatType.MoviesHD, "720P");
            AddCategoryMapping("BDRip", TorznabCatType.MoviesSD, "BDRip");
            AddCategoryMapping("BluRay", TorznabCatType.MoviesBluRay, "BluRay");
            AddCategoryMapping("BRRip", TorznabCatType.MoviesSD, "BRRip");
            AddCategoryMapping("DVDR", TorznabCatType.MoviesDVD, "DVDR");
            AddCategoryMapping("DVDRip", TorznabCatType.MoviesSD, "DVDRip");
            AddCategoryMapping("FLAC", TorznabCatType.AudioLossless, "FLAC OST");
            AddCategoryMapping("MP3", TorznabCatType.AudioMP3, "MP3 OST");
            AddCategoryMapping("MP4", TorznabCatType.MoviesOther, "MP4");
            AddCategoryMapping("Packs", TorznabCatType.MoviesOther, "Packs");
            AddCategoryMapping("R5", TorznabCatType.MoviesDVD, "R5 / SCR");
            AddCategoryMapping("Remux", TorznabCatType.MoviesOther, "Remux");
            AddCategoryMapping("TVRip", TorznabCatType.MoviesOther, "TVRip");
            AddCategoryMapping("WebRip", TorznabCatType.MoviesWEBDL, "WebRip");
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
                { "captchaSelection", (string)captchaSelection }
            };

            var result2 = await RequestLoginAndFollowRedirect(LoginUrl, pairs, result1.Cookies, true, null, null, true);

            await ConfigureIfOK(result2.Cookies, result2.ContentString.Contains("logout.php"), () =>
                                    throw new ExceptionWithConfigData("Login Failed", configData));
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            var qc = new NameValueCollection
            {
                {"action", "torrentstable"},
                {"viewtype", "0"},
                {"visiblecategories", "Action,Adventure,Animation,Biography,Comedy,Crime,Documentary,Drama,Family,Fantasy,History,Horror,Kids,Music,Mystery,Packs,Romance,Sci-Fi,Short,Sports,Thriller,War,Western"},
                {"page", "1"},
                {"visibility", "showall"},
                {"compression", "showall"},
                {"sort", "added"},
                {"order", "DESC"},
                {"titleonly", "true"},
                {"packs", "showall"},
                {"bookmarks", "showall"},
                {"subscriptions", "showall"},
                {"skw", "showall"}
            };

            if (!string.IsNullOrWhiteSpace(query.ImdbID))
                qc.Add("advancedsearchparameters", $"[imdb={query.ImdbID}]");
            else if (!string.IsNullOrWhiteSpace(query.GetQueryString()))
                // search keywords use OR by default and it seems like there's no way to change it, expect unwanted results
                qc.Add("searchstring", query.GetQueryString());

            var cats = MapTorznabCapsToTrackers(query);
            qc.Add("hiddenqualities", string.Join(",", cats));

            var searchUrl = SearchUrl + "?" + qc.GetQueryString();

            var results = await RequestWithCookiesAndRetryAsync(searchUrl);
            if (results.IsRedirect)
            {
                // re-login
                await ApplyConfiguration(null);
                results = await RequestWithCookiesAndRetryAsync(searchUrl);
            }

            try
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(results.ContentString);
                var rows = dom.QuerySelectorAll("table.main > tbody > tr");
                foreach (var row in rows.Skip(1))
                {

                    var qDetailsLink = row.QuerySelector("td:nth-of-type(2) > a:nth-of-type(1)"); // link to the movie, not the actual torrent

                    var qCatIcon = row.QuerySelector("td:nth-of-type(1) > a > img");
                    var catStr = qCatIcon != null ?
                        qCatIcon.GetAttribute("src").Split('/').Last().Split('.').First() :
                        "packs";

                    var qSeeders = row.QuerySelector("td:nth-of-type(9)");
                    var qLeechers = row.QuerySelector("td:nth-of-type(10)");
                    var qDownloadLink = row.QuerySelector("td > a:has(img[alt=\"Download Torrent\"])");
                    var qPudDate = row.QuerySelector("td:nth-of-type(6) > nobr");
                    var qSize = row.QuerySelector("td:nth-of-type(7)");

                    var link = new Uri(SiteLink + qDownloadLink.GetAttribute("href").Substring(1));

                    var dateStr = qPudDate.Text().Trim();
                    DateTime pubDateUtc;
                    if (dateStr.StartsWith("Today "))
                        pubDateUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Unspecified) + DateTime.ParseExact(dateStr.Split(new[] { ' ' }, 2)[1], "hh:mm tt", CultureInfo.InvariantCulture).TimeOfDay;
                    else if (dateStr.StartsWith("Yesterday "))
                        pubDateUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Unspecified) +
                            DateTime.ParseExact(dateStr.Split(new[] { ' ' }, 2)[1], "hh:mm tt", CultureInfo.InvariantCulture).TimeOfDay - TimeSpan.FromDays(1);
                    else
                        pubDateUtc = DateTime.SpecifyKind(DateTime.ParseExact(dateStr, "MMM d yyyy hh:mm tt", CultureInfo.InvariantCulture), DateTimeKind.Unspecified);

                    var sizeStr = qSize.Text();
                    var seeders = ParseUtil.CoerceInt(qSeeders.Text());
                    var files = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(4)").TextContent);
                    var grabs = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(8)").TextContent);
                    var details = new Uri(SiteLink + qDetailsLink.GetAttribute("href"));
                    var size = ReleaseInfo.GetBytes(sizeStr);
                    var leechers = ParseUtil.CoerceInt(qLeechers.Text());
                    var title = qDetailsLink.GetAttribute("alt");
                    var release = new ReleaseInfo
                    {
                        MinimumRatio = 1,
                        MinimumSeedTime = 72 * 60 * 60,
                        Title = title,
                        Category = MapTrackerCatToNewznab(catStr),
                        Link = link,
                        Details = details,
                        Guid = link,
                        PublishDate = pubDateUtc.ToLocalTime(),
                        Size = size,
                        Seeders = seeders,
                        Peers = leechers + seeders,
                        Files = files,
                        Grabs = grabs,
                        DownloadVolumeFactor = 0, // ratioless
                        UploadVolumeFactor = 1
                    };
                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(results.ContentString, ex);
            }

            return releases;
        }
    }
}
