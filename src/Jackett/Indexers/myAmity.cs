using Jackett.Utils.Clients;
using NLog;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Models;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using CsQuery;
using System;
using System.Globalization;
using Jackett.Models.IndexerConfig;
using System.Collections.Specialized;
using System.Text;

namespace Jackett.Indexers
{
    public class myAmity : BaseWebIndexer
    {
        string LoginUrl { get { return SiteLink + "account-login.php"; } }
        string BrowseUrl { get { return SiteLink + "torrents-search.php"; } }

        new ConfigurationDataBasicLoginWithRSSAndDisplay configData
        {
            get { return (ConfigurationDataBasicLoginWithRSSAndDisplay)base.configData; }
            set { base.configData = value; }
        }

        public myAmity(IIndexerConfigurationService configService, IWebClient wc, Logger l, IProtectionService ps)
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
            Encoding = Encoding.GetEncoding("UTF-8");
            Language = "de-de";
            Type = "private";

            AddCategoryMapping(20, TorznabCatType.PC); // Apps - PC
            AddCategoryMapping(24, TorznabCatType.AudioAudiobook); // Audio - Hoerbuch/-spiel
            AddCategoryMapping(22, TorznabCatType.Audio); // Audio - Musik
            AddCategoryMapping(52, TorznabCatType.Movies3D); // Filme - 3D
            AddCategoryMapping(51, TorznabCatType.MoviesBluRay); // Filme - BluRay Complete
            AddCategoryMapping(1,  TorznabCatType.MoviesDVD); // Filme - DVD
            AddCategoryMapping(54, TorznabCatType.MoviesHD); // Filme - HD/1080p
            AddCategoryMapping(3,  TorznabCatType.MoviesHD); // Filme - HD/720p
            AddCategoryMapping(48, TorznabCatType.XXX); // Filme - Heimatfilme.XXX
            AddCategoryMapping(50, TorznabCatType.Movies); // Filme - x264/H.264
            AddCategoryMapping(2,  TorznabCatType.MoviesSD); // Filme - XViD
            AddCategoryMapping(11, TorznabCatType.Console); // Games - Konsolen
            AddCategoryMapping(10, TorznabCatType.PCGames); // Games - PC
            AddCategoryMapping(53, TorznabCatType.Other); // International - Complete
            AddCategoryMapping(36, TorznabCatType.Books); // Sonstige - E-Books
            AddCategoryMapping(38, TorznabCatType.Other); // Sonstige - Handy
            AddCategoryMapping(7,  TorznabCatType.TVDocumentary); // TV/HDTV - Dokus
            AddCategoryMapping(8,  TorznabCatType.TV); // TV/HDTV - Serien
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
                CQ dom = result.Content;
                var errorMessage = dom["div.myFrame-content"].Html();
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            TimeZoneInfo.TransitionTime startTransition = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 3, 0, 0), 3, 5, DayOfWeek.Sunday);
            TimeZoneInfo.TransitionTime endTransition = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 4, 0, 0), 10, 5, DayOfWeek.Sunday);
            TimeSpan delta = new TimeSpan(1, 0, 0);
            TimeZoneInfo.AdjustmentRule adjustment = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(new DateTime(1999, 10, 1), DateTime.MaxValue.Date, delta, startTransition, endTransition);
            TimeZoneInfo.AdjustmentRule[] adjustments = { adjustment };
            TimeZoneInfo germanyTz = TimeZoneInfo.CreateCustomTimeZone("W. Europe Standard Time", new TimeSpan(1, 0, 0), "(GMT+01:00) W. Europe Standard Time", "W. Europe Standard Time", "W. Europe DST Time", adjustments);

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
                CQ dom = results;
                var rows = dom["table.ttable_headinner > tbody > tr.t-row"];

                foreach (var row in rows)
                {
                    var release = new ReleaseInfo();
                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 90 * 60;

                    var qRow = row.Cq();

                    var qDetailsLink = qRow.Find("a[href^=torrents-details.php?id=]").First();
                    release.Title = qDetailsLink.Attr("title");

                    if (!query.MatchQueryStringAND(release.Title))
                        continue;

                    var qCatLink = qRow.Find("a[href^=torrents.php?cat=]").First();
                    var qDLLink = qRow.Find("a[href^=download.php]").First();
                    var qSeeders = qRow.Find("td:eq(6)");
                    var qLeechers = qRow.Find("td:eq(7)");
                    var qDateStr = qRow.Find("td:eq(9)").First();
                    var qSize = qRow.Find("td:eq(4)").First();

                    var catStr = qCatLink.Attr("href").Split('=')[1];
                    release.Category = MapTrackerCatToNewznab(catStr);

                    release.Link = new Uri(SiteLink + qDLLink.Attr("href"));
                    release.Comments = new Uri(SiteLink + qDetailsLink.Attr("href"));
                    release.Guid = release.Link;

                    var sizeStr = qSize.Text();
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    release.Seeders = ParseUtil.CoerceInt(qSeeders.Text());
                    release.Peers = ParseUtil.CoerceInt(qLeechers.Text()) + release.Seeders;

                    var dateStr = qDateStr.Text().Trim();
                    DateTime dateGerman = DateTime.SpecifyKind(DateTime.ParseExact(dateStr, "dd.MM.yy HH:mm:ss", CultureInfo.InvariantCulture), DateTimeKind.Unspecified);

                    DateTime pubDateUtc = TimeZoneInfo.ConvertTimeToUtc(dateGerman, germanyTz);
                    release.PublishDate = pubDateUtc.ToLocalTime();

                    var grabs = qRow.Find("td:nth-child(6)").Text();
                    release.Grabs = ParseUtil.CoerceInt(grabs);

                    if (qRow.Find("img[src=\"images/free.gif\"]").Length >= 1)
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

