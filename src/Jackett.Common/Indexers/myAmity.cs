using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
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
    public class myAmity : BaseWebIndexer
    {
        private string LoginUrl => SiteLink + "account-login.php";
        private string BrowseUrl => SiteLink + "torrents-search.php";

        private new ConfigurationDataBasicLoginWithRSSAndDisplay configData
        {
            get => (ConfigurationDataBasicLoginWithRSSAndDisplay)base.configData;
            set => base.configData = value;
        }

        public myAmity(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(name: "myAmity",
                   description: "A German general tracker.",
                   link: "https://ttv2.myamity.info/",
                   caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   configData: new ConfigurationDataBasicLoginWithRSSAndDisplay())
        {
            Encoding = Encoding.UTF8;
            Language = "de-de";
            Type = "private";

            AddCategoryMapping(20, TorznabCatType.PC); // Apps - PC
            AddCategoryMapping(24, TorznabCatType.AudioAudiobook); // Audio - Hoerbuch/-spiel
            AddCategoryMapping(22, TorznabCatType.Audio); // Audio - Musik
            AddCategoryMapping(52, TorznabCatType.Movies3D); // Filme - 3D
            AddCategoryMapping(51, TorznabCatType.MoviesBluRay); // Filme - BluRay Complete
            AddCategoryMapping(1, TorznabCatType.MoviesDVD); // Filme - DVD
            AddCategoryMapping(56, TorznabCatType.MoviesUHD); // Filme - UHD/4K
            AddCategoryMapping(54, TorznabCatType.MoviesHD); // Filme - HD/1080p
            AddCategoryMapping(3, TorznabCatType.MoviesHD); // Filme - HD/720p
            AddCategoryMapping(48, TorznabCatType.XXX); // Filme - Heimatfilme.XXX
            AddCategoryMapping(50, TorznabCatType.Movies); // Filme - x264/H.264
            AddCategoryMapping(2, TorznabCatType.MoviesSD); // Filme - XViD
            AddCategoryMapping(11, TorznabCatType.Console); // Games - Konsolen
            AddCategoryMapping(10, TorznabCatType.PCGames); // Games - PC
            AddCategoryMapping(53, TorznabCatType.Other); // International - Complete
            AddCategoryMapping(36, TorznabCatType.Books); // Sonstige - E-Books
            AddCategoryMapping(38, TorznabCatType.Other); // Sonstige - Handy
            AddCategoryMapping(59, TorznabCatType.TVAnime); // Sonstige - Anime
            AddCategoryMapping(7, TorznabCatType.TVDocumentary); // TV/HDTV - Dokus
            AddCategoryMapping(8, TorznabCatType.TV); // TV/HDTV - Serien
            AddCategoryMapping(57, TorznabCatType.TVSport); // Sport - Allgemein
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string>
            {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value }
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, LoginUrl, true);

            await ConfigureIfOK(result.Cookies, result.Content != null && result.Cookies.Contains("pass=") && !result.Cookies.Contains("deleted"), () =>
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(result.Content);
                var errorMessage = dom.QuerySelector("div.myFrame-content").InnerHtml;
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var startTransition = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 3, 0, 0), 3, 5, DayOfWeek.Sunday);
            var endTransition = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 4, 0, 0), 10, 5, DayOfWeek.Sunday);
            var delta = new TimeSpan(1, 0, 0);
            var adjustment = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(new DateTime(1999, 10, 1), DateTime.MaxValue.Date, delta, startTransition, endTransition);
            TimeZoneInfo.AdjustmentRule[] adjustments = { adjustment };
            var germanyTz = TimeZoneInfo.CreateCustomTimeZone("W. Europe Standard Time", new TimeSpan(1, 0, 0), "(GMT+01:00) W. Europe Standard Time", "W. Europe Standard Time", "W. Europe DST Time", adjustments);

            var releases = new List<ReleaseInfo>();

            var searchString = query.GetQueryString();
            var searchUrl = BrowseUrl;
            var queryCollection = new NameValueCollection();
            queryCollection.Add("incldead", "1");
            queryCollection.Add("freeleech", "0");
            queryCollection.Add("inclexternal", "0");
            queryCollection.Add("lang", "0");

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                queryCollection.Add("search", searchString);
            }

            foreach (var cat in MapTorznabCapsToTrackers(query))
            {
                queryCollection.Add("c" + cat, "1");
            }
            searchUrl += "?" + queryCollection.GetQueryString();

            var response = await RequestStringWithCookies(searchUrl);
            if (response.IsRedirect || response.Cookies != null && response.Cookies.Contains("pass=deleted;"))
            {
                // re-login
                await ApplyConfiguration(null);
                response = await RequestStringWithCookies(searchUrl);
            }

            var results = response.Content;
            try
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(results);
                var rows = dom.QuerySelectorAll("table.ttable_headinner > tbody > tr.t-row");

                foreach (var row in rows)
                {
                    var release = new ReleaseInfo();
                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 90 * 60;

                    var qDetailsLink = row.QuerySelector("a[href^=\"torrents-details.php?id=\"]");
                    var qDetailsTitle = row.QuerySelector("td:has(a[href^=\"torrents-details.php?id=\"]) b"); // #7100
                    release.Title = qDetailsTitle.TextContent;

                    if (!query.MatchQueryStringAND(release.Title))
                        continue;

                    var qCatLink = row.QuerySelector("a[href^=\"torrents.php?cat=\"]");
                    var qDLLink = row.QuerySelector("a[href^=\"download.php\"]");
                    var qSeeders = row.QuerySelector("td:nth-of-type(7)");
                    var qLeechers = row.QuerySelector("td:nth-of-type(8)");
                    var qDateStr = row.QuerySelector("td:nth-of-type(10)");
                    var qSize = row.QuerySelector("td:nth-of-type(5)");

                    var catStr = qCatLink.GetAttribute("href").Split('=')[1];
                    release.Category = MapTrackerCatToNewznab(catStr);

                    release.Link = new Uri(SiteLink + qDLLink.GetAttribute("href"));
                    release.Comments = new Uri(SiteLink + qDetailsLink.GetAttribute("href"));
                    release.Guid = release.Link;

                    var sizeStr = qSize.TextContent;
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    release.Seeders = ParseUtil.CoerceInt(qSeeders.TextContent);
                    release.Peers = ParseUtil.CoerceInt(qLeechers.TextContent) + release.Seeders;

                    var dateStr = qDateStr.TextContent.Trim();
                    var dateGerman = DateTime.SpecifyKind(DateTime.ParseExact(dateStr, "dd.MM.yy HH:mm:ss", CultureInfo.InvariantCulture), DateTimeKind.Unspecified);

                    var pubDateUtc = TimeZoneInfo.ConvertTimeToUtc(dateGerman, germanyTz);
                    release.PublishDate = pubDateUtc.ToLocalTime();

                    var grabs = row.QuerySelector("td:nth-child(6)").TextContent;
                    release.Grabs = ParseUtil.CoerceInt(grabs);

                    if (row.QuerySelector("img[src=\"images/free.gif\"]") != null)
                        release.DownloadVolumeFactor = 0;
                    else
                        release.DownloadVolumeFactor = 1;

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
