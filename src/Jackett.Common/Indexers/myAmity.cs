using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using CsQuery;
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
        private string LoginUrl => $"{SiteLink}account-login.php";
        private string BrowseUrl => $"{SiteLink}torrents-search.php";

        private new ConfigurationDataBasicLoginWithRSSAndDisplay configData
        {
            get => (ConfigurationDataBasicLoginWithRSSAndDisplay)base.configData;
            set => base.configData = value;
        }

        public myAmity(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps) : base(
            "myAmity", description: "A German general tracker.", link: "https://ttv2.myamity.info/",
            caps: TorznabUtil.CreateDefaultTorznabTVCaps(), configService: configService, client: wc, logger: l, p: ps,
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
                {"username", configData.Username.Value}, {"password", configData.Password.Value}
            };
            var result = await RequestLoginAndFollowRedirectAsync(LoginUrl, pairs, null, true, null, LoginUrl, true);
            await ConfigureIfOkAsync(
                result.Cookies,
                result.Content != null && result.Cookies.Contains("pass=") && !result.Cookies.Contains("deleted"), () =>
                {
                    CQ dom = result.Content;
                    var errorMessage = dom["div.myFrame-content"].Html();
                    throw new ExceptionWithConfigData(errorMessage, configData);
                });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var startTransition = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(
                new DateTime(1, 1, 1, 3, 0, 0), 3, 5, DayOfWeek.Sunday);
            var endTransition = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(
                new DateTime(1, 1, 1, 4, 0, 0), 10, 5, DayOfWeek.Sunday);
            var delta = new TimeSpan(1, 0, 0);
            var adjustment = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
                new DateTime(1999, 10, 1), DateTime.MaxValue.Date, delta, startTransition,
                endTransition);
            TimeZoneInfo.AdjustmentRule[] adjustments =
            {
                adjustment
            };
            var germanyTz = TimeZoneInfo.CreateCustomTimeZone(
                "W. Europe Standard Time", new TimeSpan(1, 0, 0), "(GMT+01:00) W. Europe Standard Time",
                "W. Europe Standard Time", "W. Europe DST Time", adjustments);
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();
            var searchUrl = BrowseUrl;
            var queryCollection = new NameValueCollection
            {
                {"incldead", "1"}, {"freeleech", "0"}, {"inclexternal", "0"}, {"lang", "0"}
            };
            if (!string.IsNullOrWhiteSpace(searchString))
                queryCollection.Add("search", searchString);
            foreach (var cat in MapTorznabCapsToTrackers(query))
                queryCollection.Add($"c{cat}", "1");
            searchUrl += $"?{queryCollection.GetQueryString()}";
            var response = await RequestStringWithCookiesAsync(searchUrl);
            if (response.IsRedirect || response.Cookies?.Contains("pass=deleted;") == true)
            {
                // re-login
                await ApplyConfiguration(null);
                response = await RequestStringWithCookiesAsync(searchUrl);
            }

            var results = response.Content;
            try
            {
                CQ dom = results;
                var rows = dom["table.ttable_headinner > tbody > tr.t-row"];
                foreach (var row in rows)
                {
                    var release = new ReleaseInfo { MinimumRatio = 1, MinimumSeedTime = 90 * 60 };
                    var qRow = row.Cq();
                    var qDetailsLink = qRow.Find("a[href^=torrents-details.php?id=]").First();
                    var qDetailsTitle = qRow.Find("td:has(a[href^=\"torrents-details.php?id=\"]) b"); // #7100
                    release.Title = qDetailsTitle.Text();
                    if (!query.MatchQueryStringAND(release.Title))
                        continue;
                    var qCatLink = qRow.Find("a[href^=torrents.php?cat=]").First();
                    var qDlLink = qRow.Find("a[href^=download.php]").First();
                    var qSeeders = qRow.Find("td:eq(6)");
                    var qLeechers = qRow.Find("td:eq(7)");
                    var qDateStr = qRow.Find("td:eq(9)").First();
                    var qSize = qRow.Find("td:eq(4)").First();
                    var catStr = qCatLink.Attr("href").Split('=')[1];
                    release.Category = MapTrackerCatToNewznab(catStr);
                    release.Link = new Uri(SiteLink + qDlLink.Attr("href"));
                    release.Comments = new Uri(SiteLink + qDetailsLink.Attr("href"));
                    release.Guid = release.Link;
                    var sizeStr = qSize.Text();
                    release.Size = ReleaseInfo.GetBytes(sizeStr);
                    release.Seeders = ParseUtil.CoerceInt(qSeeders.Text());
                    release.Peers = ParseUtil.CoerceInt(qLeechers.Text()) + release.Seeders;
                    var dateStr = qDateStr.Text().Trim();
                    var dateGerman = DateTime.SpecifyKind(
                        DateTime.ParseExact(dateStr, "dd.MM.yy HH:mm:ss", CultureInfo.InvariantCulture),
                        DateTimeKind.Unspecified);
                    var pubDateUtc = TimeZoneInfo.ConvertTimeToUtc(dateGerman, germanyTz);
                    release.PublishDate = pubDateUtc.ToLocalTime();
                    var grabs = qRow.Find("td:nth-child(6)").Text();
                    release.Grabs = ParseUtil.CoerceInt(grabs);
                    release.DownloadVolumeFactor = qRow.Find("img[src=\"images/free.gif\"]").Length >= 1 ? 0 : 1;
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
