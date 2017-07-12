using CsQuery;
using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jackett.Models.IndexerConfig;
using System.Collections.Specialized;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Jackett.Indexers
{
    public class HouseOfTorrents : BaseWebIndexer
    {
        private string SearchUrl { get { return SiteLink + "browse.php"; } }
        private string LoginUrl { get { return SiteLink + "takelogin.php"; } }
        private string CaptchaUrl { get { return SiteLink + "simpleCaptcha.php?numImages=1"; } }

        new ConfigurationDataBasicLoginWithRSSAndDisplay configData
        {
            get { return (ConfigurationDataBasicLoginWithRSSAndDisplay)base.configData; }
            set { base.configData = value; }
        }

        public HouseOfTorrents(IIndexerConfigurationService configService, IWebClient w, Logger l, IProtectionService ps)
            : base(name: "House-of-Torrents",
                description: "A general tracker",
                link: "https://houseoftorrents.club/",
                caps: new TorznabCapabilities(),
                configService: configService,
                client: w,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLoginWithRSSAndDisplay())
        {
            Encoding = Encoding.GetEncoding("UTF-8");
            Language = "en-us";
            Type = "private";

            AddCategoryMapping(42, TorznabCatType.PCMac); // Applications/Mac
            AddCategoryMapping(34, TorznabCatType.PC); // Applications/PC
            AddCategoryMapping(66, TorznabCatType.MoviesForeign); // Foreign
            AddCategoryMapping(38, TorznabCatType.MoviesForeign); // Foreign/French
            AddCategoryMapping(39, TorznabCatType.MoviesForeign); // Foreign/German
            AddCategoryMapping(40, TorznabCatType.MoviesForeign); // Foreign/Spanish
            AddCategoryMapping(41, TorznabCatType.MoviesForeign); // Foreign/Swedish
            AddCategoryMapping(67, TorznabCatType.ConsoleNDS); // Games/Nintendo
            AddCategoryMapping(9 , TorznabCatType.PCGames); // Games/PC 
            AddCategoryMapping(8,  TorznabCatType.ConsolePS3); // Games/PS3
            AddCategoryMapping(30, TorznabCatType.ConsolePS4); // Games/PS4
            AddCategoryMapping(7,  TorznabCatType.ConsolePSP); // Games/PSP
            AddCategoryMapping(29, TorznabCatType.ConsoleWii); // Games/Wii
            AddCategoryMapping(31, TorznabCatType.ConsoleXbox360); // Games/XBOX360
            AddCategoryMapping(32, TorznabCatType.ConsoleXboxOne); // Games/XBOXONE
            AddCategoryMapping(71, TorznabCatType.PCPhoneAndroid); // Mobile/Android
            AddCategoryMapping(72, TorznabCatType.PCPhoneIOS); // Mobile/iOS
            AddCategoryMapping(47, TorznabCatType.Movies3D); // Movies/3D
            AddCategoryMapping(43, TorznabCatType.MoviesBluRay); // Movies/Bluray
            AddCategoryMapping(84, TorznabCatType.MoviesSD); // Movies/Cam
            AddCategoryMapping(44, TorznabCatType.MoviesDVD); // Movies/DVD-R
            AddCategoryMapping(45, TorznabCatType.Movies); // Movies/MP4
            AddCategoryMapping(69, TorznabCatType.Movies); // Movies/Packs
            AddCategoryMapping(46, TorznabCatType.MoviesSD); // Movies/SD
            AddCategoryMapping(11, TorznabCatType.MoviesHD); // Movies/x264
            AddCategoryMapping(83, TorznabCatType.MoviesHD); // Movies/x265
            AddCategoryMapping(10, TorznabCatType.MoviesOther); // Movies/XviD
            AddCategoryMapping(36, TorznabCatType.AudioLossless); // Music/FLAC
            AddCategoryMapping(12, TorznabCatType.AudioMP3); // Music/MP3
            AddCategoryMapping(79, TorznabCatType.Audio); // Music/Pack
            AddCategoryMapping(28, TorznabCatType.AudioVideo); // Music/Video
            AddCategoryMapping(49, TorznabCatType.TVAnime); // Others/Anime
            AddCategoryMapping(80, TorznabCatType.AudioAudiobook); // Others/AudioBook
            AddCategoryMapping(60, TorznabCatType.Other); // Others/Boxsets
            AddCategoryMapping(65, TorznabCatType.TVDocumentary); // Others/Documentary
            AddCategoryMapping(61, TorznabCatType.Books); // Others/E-Book
            AddCategoryMapping(51, TorznabCatType.Other); // Others/RARFIX
            AddCategoryMapping(74, TorznabCatType.TVSport); // Sports
            AddCategoryMapping(75, TorznabCatType.TVSport); // Sports/Boxing
            AddCategoryMapping(76, TorznabCatType.TVSport); // Sports/Racing
            AddCategoryMapping(77, TorznabCatType.TVSport); // Sports/UFC
            AddCategoryMapping(78, TorznabCatType.TVSport); // Sports/WWE
            AddCategoryMapping(68, TorznabCatType.TV); // TV/Packs
            AddCategoryMapping(53, TorznabCatType.TVSD); // TV/SD
            AddCategoryMapping(54, TorznabCatType.TVHD); // TV/x264
            AddCategoryMapping(82, TorznabCatType.TVHD); // TV/x265
            AddCategoryMapping(55, TorznabCatType.TVOTHER); // Tv/XviD
            AddCategoryMapping(63, TorznabCatType.XXX); // XXX
            AddCategoryMapping(57, TorznabCatType.XXX); // XXX/0-DAY
            AddCategoryMapping(58, TorznabCatType.XXXImageset); // XXX/IMAGESET
            AddCategoryMapping(81, TorznabCatType.XXXPacks); // XXX/Pack
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            // reset cookies, if we send expired cookies for a new session their code seems to get confused
            // Due to the session not getting initiated correctly it will result in errors like this:
            // Notice: Undefined index: simpleCaptchaAnswer in /var/www/html/takelogin.php on line 17
            CookieHeader = null;

            var result1 = await RequestStringWithCookies(CaptchaUrl);
            var json1 = JObject.Parse(result1.Content);
            var captchaSelection = json1["images"][0]["hash"];

            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "captchaSelection", (string)captchaSelection },
                { "submitme", "X" }
            };

            var result2 = await RequestLoginAndFollowRedirect(LoginUrl, pairs, result1.Cookies, true, null, null, true);

            await ConfigureIfOK(result2.Cookies, result2.Content.Contains("logout.php"), () =>
            {
                var errorMessage = result2.Content;
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            var searchString = query.GetQueryString();
            var searchUrl = SearchUrl;
            var queryCollection = new NameValueCollection();
            queryCollection.Add("searchin", "title");
            queryCollection.Add("incldead", "1");
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                // use AND+wildcard operator to avoid getting to many useless results
                var searchStringArray = Regex.Split(searchString.Trim(), "[ _.-]+", RegexOptions.Compiled).ToList();
                searchStringArray = searchStringArray.Where(x => x.Length >= 3).ToList(); //  remove words with less than 3 characters
                searchStringArray = searchStringArray.Select(x => "+" + x).ToList(); // add AND operators+wildcards
                var searchStringFinal = String.Join("", searchStringArray);
                queryCollection.Add("search", searchStringFinal);
            }

            foreach (var cat in MapTorznabCapsToTrackers(query))
            {
                queryCollection.Add("c" + cat, "1");
            }

            searchUrl += "?" + queryCollection.GetQueryString();

            var results = await RequestStringWithCookiesAndRetry(searchUrl);

            if (results.IsRedirect)
            {
                await ApplyConfiguration(null);
                results = await RequestStringWithCookiesAndRetry(searchUrl);
            }

            try
            {
                CQ dom = results.Content;
                var rows = dom["table.tt > tbody > tr"];
                foreach (var row in rows.Skip(1))
                {
                    var release = new ReleaseInfo();
                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 72 * 60 * 60;

                    var qRow = row.Cq();

                    var qDetailsLink = qRow.Find("a[href^=details.php?id=]").First();
                    release.Title = qDetailsLink.Text().Trim();

                    // HoT search returns should support AND search but it simply doesn't work, so we AND filter it manualy
                    if (!query.MatchQueryStringAND(release.Title))
                        continue;

                    var qCatLink = qRow.Find("a[href^=browse.php?cat=]").First();
                    var qSeeders = qRow.Find("td:eq(8)");
                    var qLeechers = qRow.Find("td:eq(9)");
                    var qDownloadLink = qRow.Find("a[href^=download.php]").First();
                    var qTimeAgo = qRow.Find("td:eq(5)");
                    var qSize = qRow.Find("td:eq(6)");

                    var catStr = qCatLink.Attr("href").Split('=')[1];
                    release.Category = MapTrackerCatToNewznab(catStr);

                    release.Link = new Uri(SiteLink + qDownloadLink.Attr("href"));
                    release.Comments = new Uri(SiteLink + qDetailsLink.Attr("href"));
                    release.Guid = release.Link;

                    var sizeStr = qSize.Text();
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    release.Seeders = ParseUtil.CoerceInt(qSeeders.Text());
                    release.Peers = ParseUtil.CoerceInt(qLeechers.Text()) + release.Seeders;

                    var dateStr = qTimeAgo.Text();
                    DateTime pubDateUtc;
                    var Timeparts = dateStr.Split(new char[] { ' ' }, 2)[1];
                    if (dateStr.StartsWith("Today "))
                        pubDateUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Unspecified) + DateTime.ParseExact(dateStr.Split(new char[] { ' ' }, 2)[1], "hh:mm tt", System.Globalization.CultureInfo.InvariantCulture).TimeOfDay;
                    else if (dateStr.StartsWith("Yesterday "))
                        pubDateUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Unspecified) +
                            DateTime.ParseExact(dateStr.Split(new char[] { ' ' }, 2)[1], "hh:mm tt", System.Globalization.CultureInfo.InvariantCulture).TimeOfDay - TimeSpan.FromDays(1);
                    else
                        pubDateUtc = DateTime.SpecifyKind(DateTime.ParseExact(dateStr, "MMM d yyyy hh:mm tt", CultureInfo.InvariantCulture), DateTimeKind.Unspecified);

                    release.PublishDate = pubDateUtc.ToLocalTime();

                    var files = qRow.Find("td:nth-child(4)").Text();
                    release.Files = ParseUtil.CoerceInt(files);

                    var grabs = qRow.Find("td:nth-child(8) > a").Html();
                    release.Grabs = ParseUtil.CoerceInt(grabs.Split('<')[0]);

                    release.DownloadVolumeFactor = 0; // ratioless
                    
                    release.UploadVolumeFactor = 1;

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(results.Content, ex);
            }

            return releases;
        }
    }
}
