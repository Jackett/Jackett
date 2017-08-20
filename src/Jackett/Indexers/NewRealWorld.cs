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
    public class NewRealWorld : BaseWebIndexer
    {
        string LoginUrl { get { return SiteLink + "login.php"; } }
        string BrowseUrl { get { return SiteLink + "browse.php"; } }

        new ConfigurationDataBasicLoginWithRSSAndDisplay configData
        {
            get { return (ConfigurationDataBasicLoginWithRSSAndDisplay)base.configData; }
            set { base.configData = value; }
        }

        public NewRealWorld(IIndexerConfigurationService configService, IWebClient wc, Logger l, IProtectionService ps)
            : base(name: "New Real World",
                   description: "A German general tracker.",
                   link: "https://nrw-tracker.eu/",
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

            AddCategoryMapping(39, TorznabCatType.TVAnime); // Anime: HD|1080p
            AddCategoryMapping(38, TorznabCatType.TVAnime); // Anime: HD|720p
            AddCategoryMapping(1,  TorznabCatType.TVAnime); // Anime: SD
            AddCategoryMapping(7,  TorznabCatType.PCPhoneOther); // Appz: Handy-PDA
            AddCategoryMapping(36, TorznabCatType.PCMac); // Appz: Mac
            AddCategoryMapping(18, TorznabCatType.PC); // Appz: Sonstiges
            AddCategoryMapping(17, TorznabCatType.PC); // Appz: Win
            AddCategoryMapping(15, TorznabCatType.Audio); // Audio: DVD-R
            AddCategoryMapping(49, TorznabCatType.AudioLossless); // Audio: Flac
            AddCategoryMapping(30, TorznabCatType.AudioAudiobook); // Audio: Hörspiele
            AddCategoryMapping(14, TorznabCatType.AudioMP3); // Audio: MP3
            AddCategoryMapping(22, TorznabCatType.AudioVideo); // Audio: Videoclip
            AddCategoryMapping(19, TorznabCatType.Other); // Diverses: Sonstiges
            AddCategoryMapping(43, TorznabCatType.TVDocumentary); // Dokus: HD
            AddCategoryMapping(2,  TorznabCatType.TVDocumentary); // Dokus: SD
            AddCategoryMapping(3,  TorznabCatType.Books); // Ebooks: Bücher
            AddCategoryMapping(52, TorznabCatType.BooksComics); // Ebooks: Comics
            AddCategoryMapping(53, TorznabCatType.BooksMagazines); // Ebooks: Magazine
            AddCategoryMapping(55, TorznabCatType.BooksOther); // Ebooks: XXX
            AddCategoryMapping(54, TorznabCatType.BooksOther); // Ebooks: Zeitungen
            AddCategoryMapping(47, TorznabCatType.PCPhoneOther); // Games: Andere
            AddCategoryMapping(32, TorznabCatType.PCMac); // Games: Mac
            AddCategoryMapping(41, TorznabCatType.ConsoleNDS); // Games: NDS/3DS
            AddCategoryMapping(4,  TorznabCatType.PCGames); // Games: PC
            AddCategoryMapping(5,  TorznabCatType.ConsolePS3); // Games: PS2
            AddCategoryMapping(9,  TorznabCatType.ConsolePS3); // Games: PS3
            AddCategoryMapping(6,  TorznabCatType.ConsolePSP); // Games: PSP
            AddCategoryMapping(28, TorznabCatType.ConsoleWii); // Games: Wii
            AddCategoryMapping(31, TorznabCatType.ConsoleXbox); // Games: XboX
            AddCategoryMapping(51, TorznabCatType.Movies3D); // Movies: 3D
            AddCategoryMapping(37, TorznabCatType.MoviesBluRay); // Movies: BluRay
            AddCategoryMapping(25, TorznabCatType.MoviesHD); // Movies: HD|1080p
            AddCategoryMapping(29, TorznabCatType.MoviesHD); // Movies: HD|720p
            AddCategoryMapping(11, TorznabCatType.MoviesDVD); // Movies: SD|DVD-R
            AddCategoryMapping(8,  TorznabCatType.MoviesSD); // Movies: SD|x264
            AddCategoryMapping(13, TorznabCatType.MoviesSD); // Movies: SD|XviD
            AddCategoryMapping(40, TorznabCatType.MoviesForeign); // Movies: US Movies
            AddCategoryMapping(33, TorznabCatType.TV); // Serien: DVD-R
            AddCategoryMapping(34, TorznabCatType.TVHD); // Serien: HD
            AddCategoryMapping(56, TorznabCatType.TVHD); // Serien: Packs|HD
            AddCategoryMapping(44, TorznabCatType.TVSD); // Serien: Packs|SD
            AddCategoryMapping(16, TorznabCatType.TVSD); // Serien: SD
            AddCategoryMapping(10, TorznabCatType.TVOTHER); // Serien: TV/Shows
            AddCategoryMapping(21, TorznabCatType.TVFOREIGN); // Serien: US TV
            AddCategoryMapping(24, TorznabCatType.TVSport); // Sport: Diverses
            AddCategoryMapping(23, TorznabCatType.TVSport); // Sport: Wrestling
            AddCategoryMapping(57, TorznabCatType.Movies); // Tracker - Crew: pmHD
            AddCategoryMapping(58, TorznabCatType.MoviesHD); // Ultra-HD: 4K
            AddCategoryMapping(46, TorznabCatType.XXXOther); // XXX: Diverses
            AddCategoryMapping(50, TorznabCatType.XXX); // XXX: HD
            AddCategoryMapping(45, TorznabCatType.XXXPacks); // XXX: Packs
            AddCategoryMapping(27, TorznabCatType.XXX); // XXX: SD
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string>
            {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "submit", "Log+in!" }
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, LoginUrl, true);
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("logout.php"), () =>
                {
                    CQ dom = result.Content;
                    var errorMessage = dom["table.tableinborder"].Html();
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

            var cats = MapTorznabCapsToTrackers(query);
            string cat = "0";
            if (cats.Count == 1)
            {
                cat = cats[0];
            }
            queryCollection.Add("cat", cat);

            searchUrl += "?" + queryCollection.GetQueryString();

            var response = await RequestStringWithCookies(searchUrl);
            if (response.IsRedirect)
            {
                // re-login
                await ApplyConfiguration(null);
                response = await RequestStringWithCookies(searchUrl);
            }

            var results = response.Content;
            try
            {
                CQ dom = results;
                var rows = dom["table.testtable> tbody > tr:has(td.tableb)"];

                foreach (var row in rows)
                {
                    var release = new ReleaseInfo();
                    release.MinimumRatio = 0.75;
                    release.MinimumSeedTime = 0;
                    var qRow = row.Cq();

                    var qDetailsLink = qRow.Find("a[href^=details.php?id=]").First();
                    release.Title = qDetailsLink.Text();

                    if (!query.MatchQueryStringAND(release.Title))
                        continue;

                    var qCatLink = qRow.Find("a[href^=browse.php?cat=]").First();
                    var qSeeders = qRow.Find("td > table.testtable > tbody > tr > td > strong:eq(3)");
                    var qLeechers = qRow.Find("td > table.testtable > tbody > tr > td > strong:eq(4)");
                    var qDateStr = qRow.Find("td > table.testtable > tbody > tr > td:eq(6)");
                    var qSize = qRow.Find("td > table.testtable > tbody > tr > td > strong:eq(1)");
                    var qDownloadLink = qRow.Find("a[href*=download]").First();

                    var catStr = qCatLink.Attr("href").Split('=')[1];
                    release.Category = MapTrackerCatToNewznab(catStr);

                    var dlLink = qDownloadLink.Attr("href");
                    if(dlLink.Contains("javascript")) // depending on the user agent the DL link is a javascript call
                    {
                        var dlLinkParts = dlLink.Split(new char[] { '\'', ',' });
                        dlLink = SiteLink + "download/" + dlLinkParts[3] + "/" + dlLinkParts[5];
                    }
                    release.Link = new Uri(dlLink);
                    release.Comments = new Uri(SiteLink + qDetailsLink.Attr("href"));
                    release.Guid = release.Link;

                    var sizeStr = qSize.Text();
                    release.Size = ReleaseInfo.GetBytes(sizeStr.Replace(".", "").Replace(",", "."));

                    release.Seeders = ParseUtil.CoerceInt(qSeeders.Text());
                    release.Peers = ParseUtil.CoerceInt(qLeechers.Text()) + release.Seeders;

                    var dateStr = qDateStr.Text().Replace('\xA0', ' ');
                    var dateGerman = DateTime.SpecifyKind(DateTime.ParseExact(dateStr, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture), DateTimeKind.Unspecified);
                    DateTime pubDateUtc = TimeZoneInfo.ConvertTimeToUtc(dateGerman, germanyTz);
                    release.PublishDate = pubDateUtc;

                    var files = qRow.Find("td:contains(Datei) > strong ~ strong").Text();
                    release.Files = ParseUtil.CoerceInt(files);

                    if (qRow.Find("img[title=\"OnlyUpload\"]").Length >= 1)
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

