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
    public class TorrentNetwork : BaseWebIndexer
    {
        string LoginUrl { get { return SiteLink + "takelogin.php"; } }
        string BrowseUrl { get { return SiteLink + "browse.php"; } }

        new ConfigurationDataBasicLoginWithRSSAndDisplay configData
        {
            get { return (ConfigurationDataBasicLoginWithRSSAndDisplay)base.configData; }
            set { base.configData = value; }
        }

        public TorrentNetwork(IIndexerConfigurationService configService, IWebClient wc, Logger l, IProtectionService ps)
            : base(name: "Torrent Network",
                   description: "A German general tracker.",
                   link: "https://tntracker.org/",
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

            AddCategoryMapping(1,  TorznabCatType.AudioAudiobook); // aBook
            AddCategoryMapping(4,  TorznabCatType.PCMac); // App|Mac
            AddCategoryMapping(5,  TorznabCatType.PC); // App|Win
            AddCategoryMapping(7,  TorznabCatType.TVDocumentary); // Docu|HD
            AddCategoryMapping(6,  TorznabCatType.TVDocumentary); // Docu|SD
            AddCategoryMapping(8,  TorznabCatType.Books); // eBook
            AddCategoryMapping(10, TorznabCatType.PCGames); // Game|PC
            AddCategoryMapping(13, TorznabCatType.ConsolePS4); // Game|PSX
            AddCategoryMapping(12, TorznabCatType.ConsoleWii); // Game|Wii
            AddCategoryMapping(14, TorznabCatType.ConsoleXbox); // Game|XBOX
            AddCategoryMapping(30, TorznabCatType.Other); // Misc
            AddCategoryMapping(17, TorznabCatType.MoviesHD); // Movie|DE|1080p
            AddCategoryMapping(20, TorznabCatType.MoviesHD); // Movie|DE|2160p
            AddCategoryMapping(36, TorznabCatType.Movies3D); // Movie|DE|3D
            AddCategoryMapping(18, TorznabCatType.MoviesHD); // Movie|DE|720p
            AddCategoryMapping(34, TorznabCatType.TVAnime); // Movie|DE|Anime
            AddCategoryMapping(19, TorznabCatType.MoviesBluRay); // Movie|DE|BluRay
            AddCategoryMapping(45, TorznabCatType.Movies); // Movie|DE|Remux
            AddCategoryMapping(24, TorznabCatType.MoviesSD); // Movie|DE|SD
            AddCategoryMapping(39, TorznabCatType.Movies); // Movie|EN/JP|Anime
            AddCategoryMapping(43, TorznabCatType.MoviesHD); // Movie|EN|1080p
            AddCategoryMapping(37, TorznabCatType.MoviesHD); // Movie|EN|2160p
            AddCategoryMapping(35, TorznabCatType.MoviesHD); // Movie|EN|720p
            AddCategoryMapping(38, TorznabCatType.MoviesBluRay); // Movie|EN|BluRay
            AddCategoryMapping(46, TorznabCatType.Movies); // Movie|EN|Remux
            AddCategoryMapping(22, TorznabCatType.MoviesSD); // Movie|EN|SD
            AddCategoryMapping(44, TorznabCatType.AudioLossless); // Music|Flac
            AddCategoryMapping(25, TorznabCatType.AudioMP3); // Music|MP3
            AddCategoryMapping(26, TorznabCatType.AudioVideo); // Music|Video
            AddCategoryMapping(31, TorznabCatType.TVSport); // Sport
            AddCategoryMapping(2,  TorznabCatType.TVAnime); // TV|DE|Anime
            AddCategoryMapping(28, TorznabCatType.TVHD); // TV|DE|HD
            AddCategoryMapping(16, TorznabCatType.TV); // TV|DE|Pack
            AddCategoryMapping(27, TorznabCatType.TVSD); // TV|DE|SD
            AddCategoryMapping(41, TorznabCatType.TVAnime); // TV|EN/JP|Anime
            AddCategoryMapping(40, TorznabCatType.TVHD); // TV|EN|HD
            AddCategoryMapping(42, TorznabCatType.TV); // TV|EN|Pack
            AddCategoryMapping(29, TorznabCatType.TVSD); // TV|EN|SD
            AddCategoryMapping(33, TorznabCatType.XXX); // XXX|HD
            AddCategoryMapping(32, TorznabCatType.XXX); // XXX|SD
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
            queryCollection.Add("incldead", "1");
            queryCollection.Add("_by", "0");
            queryCollection.Add("sort", "4");
            queryCollection.Add("type", "desc");

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
                var rows = dom["table[border=1] > tbody > tr:has(td.torrenttable)"];

                foreach (var row in rows)
                {
                    var release = new ReleaseInfo();
                    release.MinimumRatio = 0.8;
                    release.MinimumSeedTime = 48 * 60 * 60;

                    var qRow = row.Cq();

                    var qDetailsLink = qRow.Find("a[href^=details.php?id=]").First();
                    var qTitle = qDetailsLink.Find("b").First();
                    release.Title = qTitle.Text();

                    var qCatLink = qRow.Find("a[href^=browse.php?cat=]").First();
                    var qDLLink = qRow.Find("a.download").First();
                    var qSeeders = qRow.Find("td.torrenttable:eq(7)");
                    var qLeechers = qRow.Find("td.torrenttable:eq(8)");
                    var qDateStr = qRow.Find("td.torrenttable:eq(4)").First();
                    var qSize = qRow.Find("td.torrenttable:eq(5)").First();

                    var catStr = qCatLink.Attr("href").Split('=')[1].Split('\'')[0];
                    release.Category = MapTrackerCatToNewznab(catStr);

                    release.Link = new Uri(SiteLink + qDLLink.Attr("href"));
                    release.Comments = new Uri(SiteLink + qDetailsLink.Attr("href"));
                    release.Guid = release.Link;

                    var sizeStr = qSize.Text();
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    release.Seeders = ParseUtil.CoerceInt(qSeeders.Text());
                    release.Peers = ParseUtil.CoerceInt(qLeechers.Text()) + release.Seeders;

                    var dateStr = qDateStr.Text().Trim();
                    DateTime dateGerman = DateTime.SpecifyKind(DateTime.ParseExact(dateStr, "MMM d yyyy HH:mm", CultureInfo.InvariantCulture), DateTimeKind.Unspecified);

                    DateTime pubDateUtc = TimeZoneInfo.ConvertTimeToUtc(dateGerman, germanyTz);
                    release.PublishDate = pubDateUtc.ToLocalTime();

                    var files = qRow.Find("td:nth-child(4)").Text();
                    release.Files = ParseUtil.CoerceInt(files);

                    var grabs = qRow.Find("td:nth-child(8)").Get(0).FirstChild.ToString();
                    release.Grabs = ParseUtil.CoerceInt(grabs);

                    if (qRow.Find("img[src=\"pic/torrent_ou.gif\"]").Length >= 1)
                        release.DownloadVolumeFactor = 0;
                    else if (qRow.Find("font[color=\"gray\"]:contains(50% Down)").Length >= 1)
                        release.DownloadVolumeFactor = 0.5;
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

