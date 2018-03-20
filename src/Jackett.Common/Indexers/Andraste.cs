using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
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
    public class Andraste : BaseWebIndexer
    {
        private string LoginUrl { get { return SiteLink + "takelogin.php"; } }
        private string BrowseUrl { get { return SiteLink + "browse.php"; } }

        private new ConfigurationDataBasicLoginWithRSSAndDisplay configData
        {
            get { return (ConfigurationDataBasicLoginWithRSSAndDisplay)base.configData; }
            set { base.configData = value; }
        }

        public Andraste(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(name: "Andraste",
                   description: "A German general tracker.",
                   link: "https://andraste.io/",
                   caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   configData: new ConfigurationDataBasicLoginWithRSSAndDisplay())
        {
            Encoding = Encoding.GetEncoding("iso-8859-1");
            Language = "de-de";
            Type = "private";

            AddCategoryMapping(9,  TorznabCatType.Other); // Anderes
            AddCategoryMapping(23, TorznabCatType.TVAnime); // Animation - Film &; Serie
            AddCategoryMapping(1,  TorznabCatType.PC); // Appz
            AddCategoryMapping(52, TorznabCatType.Other); // Botuploads
            AddCategoryMapping(25, TorznabCatType.TVDocumentary); // Doku - Alle Formate
            AddCategoryMapping(27, TorznabCatType.Books); // E-Books
            AddCategoryMapping(51, TorznabCatType.Movies3D); // Film/3D
            AddCategoryMapping(20, TorznabCatType.MoviesDVD); // Film/DVDr
            AddCategoryMapping(37, TorznabCatType.MoviesHD); // Film/HD 1080p++
            AddCategoryMapping(38, TorznabCatType.MoviesSD); // Film/HD 720p
            AddCategoryMapping(36, TorznabCatType.Movies); // Film/im Kino
            AddCategoryMapping(19, TorznabCatType.Movies); // Film/XviD,DivX,x264
            AddCategoryMapping(4,  TorznabCatType.PCGames); // Games/PC
            AddCategoryMapping(12, TorznabCatType.ConsolePS4); // Games/Playstation
            AddCategoryMapping(22, TorznabCatType.ConsoleWii); // Games/Wii & DS
            AddCategoryMapping(21, TorznabCatType.ConsoleXbox); // Games/Xbox & 360
            AddCategoryMapping(48, TorznabCatType.PCPhoneAndroid); // Handy & PDA/Android
            AddCategoryMapping(47, TorznabCatType.PCPhoneIOS); // Handy & PDA/iOS
            AddCategoryMapping(44, TorznabCatType.PCMac); // Macintosh
            AddCategoryMapping(41, TorznabCatType.Other); // MegaPack
            AddCategoryMapping(24, TorznabCatType.AudioAudiobook); // Musik/Hörbuch & Hörspiel
            AddCategoryMapping(46, TorznabCatType.Audio); // Musik/HQ 320++
            AddCategoryMapping(6,  TorznabCatType.Audio); // Musik/Musik
            AddCategoryMapping(26, TorznabCatType.AudioVideo); // Musik/Musikvideos
            AddCategoryMapping(29, TorznabCatType.TVSD); // Serien/DVDr
            AddCategoryMapping(35, TorznabCatType.TVHD); // Serien/HD 720p++
            AddCategoryMapping(7,  TorznabCatType.TV); // Serien/XviD,DivX,x264
            AddCategoryMapping(45, TorznabCatType.TV); // Shows
            AddCategoryMapping(40, TorznabCatType.TVSport); // Sport
            AddCategoryMapping(32, TorznabCatType.XXX); // XXX
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
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("logout.php"), () =>
            {
                CQ dom = result.Content;
                var errorMessage = dom["table.tableinborder"].Html();
                errorMessage = result.Content;
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
            queryCollection.Add("showsearch", "1");
            queryCollection.Add("incldead", "1");
            queryCollection.Add("orderby", "added");
            queryCollection.Add("sort", "desc");

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
            var results = response.Content;
            try
            {
                CQ dom = results;
                var globalFreeleech = dom.Find("div > img[alt=\"Only Upload\"][title^=\"ONLY UPLOAD \"]").Any();
                var rows = dom["table.tableinborder > tbody > tr:has(td.tableb)"];

                foreach (var row in rows)
                {
                    var release = new ReleaseInfo();
                    var qRow = row.Cq();

                    var qDetailsLink = qRow.Find("a[href^=details.php?id=]").First();
                    release.Title = qDetailsLink.Attr("title");

                    if (!query.MatchQueryStringAND(release.Title))
                        continue;

                    var qCatLink = qRow.Find("a[href^=browse.php?cat=]").First();
                    var qDLLink = qRow.Find("a[href^=download.php?torrent=]").First();
                    var qSeeders = qRow.Find("span:contains(Seeder) > b:eq(0)");
                    var qLeechers = qRow.Find("span:contains(Seeder) > b:eq(1)");
                    var qDateStr = qRow.Find("td > table > tbody > tr > td:eq(7)").First();
                    var qSize = qRow.Find("span:contains(Volumen) > b:eq(0)").First();
                    var qOnlyUpload = qRow.Find("img[title=OnlyUpload]");

                    if (qOnlyUpload.Any())
                    {
                        release.MinimumRatio = 2;
                        release.MinimumSeedTime = 144 * 60 * 60;
                    }
                    else
                    {
                        release.MinimumRatio = 1;
                        release.MinimumSeedTime = 72 * 60 * 60;
                    }

                    var catStr = qCatLink.Attr("href").Split('=')[1];
                    release.Category = MapTrackerCatToNewznab(catStr);

                    release.Link = new Uri(SiteLink + qDLLink.Attr("href"));
                    release.Comments = new Uri(SiteLink + qDetailsLink.Attr("href"));
                    release.Guid = release.Link;

                    var sizeStr = qSize.Text();
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    release.Seeders = ParseUtil.CoerceInt(qSeeders.Text());
                    release.Peers = ParseUtil.CoerceInt(qLeechers.Text()) + release.Seeders;

                    var dateStr = qDateStr.Text().Trim().Replace('\xA0', ' ');
                    DateTime dateGerman = DateTime.SpecifyKind(DateTime.ParseExact(dateStr, "dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture), DateTimeKind.Unspecified);

                    DateTime pubDateUtc = TimeZoneInfo.ConvertTimeToUtc(dateGerman, germanyTz);
                    release.PublishDate = pubDateUtc.ToLocalTime();

                    var files = qRow.Find("a[href*=\"&filelist=1\"] ~ font ~ b").Text();
                    release.Files = ParseUtil.CoerceInt(files);

                    var grabs = qRow.Find("a[href*=\"&tosnatchers=1\"] ~ font ~ b").Text();
                    release.Grabs = ParseUtil.CoerceInt(grabs);

                    if (globalFreeleech)
                        release.DownloadVolumeFactor = 0;
                    else if (qRow.Find("img[alt=\"OU\"]").Length >= 1)
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
