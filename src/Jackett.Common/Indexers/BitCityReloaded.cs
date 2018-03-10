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
    public class BitCityReloaded : BaseWebIndexer
    {
        private string LoginUrl { get { return SiteLink + "login.php"; } }
        private string BrowseUrl { get { return SiteLink + "uebersicht.php"; } }
        private TimeZoneInfo germanyTz = TimeZoneInfo.CreateCustomTimeZone("W. Europe Standard Time", new TimeSpan(1, 0, 0), "W. Europe Standard Time", "W. Europe Standard Time");

        private new ConfigurationDataBasicLoginWithRSSAndDisplay configData
        {
            get { return (ConfigurationDataBasicLoginWithRSSAndDisplay)base.configData; }
            set { base.configData = value; }
        }

        public BitCityReloaded(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(name: "Bit-City Reloaded",
                   description: "A German general tracker.",
                   link: "https://bc-reloaded.net/",
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

            this.configData.DisplayText.Value = "Only the results from the first search result page are shown, adjust your profile settings to show a reasonable amount (it looks like there's no maximum).";
            this.configData.DisplayText.Name = "Notice";

            AddCategoryMapping(1,  TorznabCatType.Other); // Anderes
            AddCategoryMapping(2,  TorznabCatType.TVAnime); // Anime
            AddCategoryMapping(34, TorznabCatType.PC); // Appz/Linux
            AddCategoryMapping(35, TorznabCatType.PCMac); // Appz/Mac
            AddCategoryMapping(36, TorznabCatType.PC); // Appz/Other
            AddCategoryMapping(20, TorznabCatType.PC); // Appz/Win
            AddCategoryMapping(3,  TorznabCatType.TVDocumentary); // Doku/Alle Formate
            AddCategoryMapping(4,  TorznabCatType.Books); // EBooks
            AddCategoryMapping(12, TorznabCatType.ConsolePS4); // Games PS / PSX
            AddCategoryMapping(11, TorznabCatType.ConsoleNDS); // Games/Nintendo DS
            AddCategoryMapping(10, TorznabCatType.PCGames); // Games/PC
            AddCategoryMapping(13, TorznabCatType.ConsoleWii); // Games/Wii
            AddCategoryMapping(14, TorznabCatType.ConsoleXbox); // Games/Xbox & 360
            AddCategoryMapping(15, TorznabCatType.PCPhoneOther); // Handy & PDA
            AddCategoryMapping(16, TorznabCatType.AudioAudiobook); // Hörspiel/Hörbuch
            AddCategoryMapping(30, TorznabCatType.Other); // International
            AddCategoryMapping(17, TorznabCatType.Other); // MegaPack
            AddCategoryMapping(43, TorznabCatType.Movies3D); // Movie/3D
            AddCategoryMapping(5,  TorznabCatType.MoviesDVD); // Movie/DVD/R
            AddCategoryMapping(6,  TorznabCatType.MoviesHD); // Movie/HD 1080p
            AddCategoryMapping(7,  TorznabCatType.MoviesHD); // Movie/HD 720p
            AddCategoryMapping(32, TorznabCatType.MoviesOther); // Movie/TVRip
            AddCategoryMapping(9,  TorznabCatType.MoviesOther); // Movie/XviD,DivX,h264
            AddCategoryMapping(26, TorznabCatType.XXX); // Movie/XXX
            AddCategoryMapping(41, TorznabCatType.XXXOther); // Movie/XXX/Other
            AddCategoryMapping(42, TorznabCatType.XXXPacks); // Movie/XXX/Pack
            AddCategoryMapping(45, TorznabCatType.MoviesHD); // Movies/4K
            AddCategoryMapping(33, TorznabCatType.MoviesBluRay); // Movies/BluRay
            AddCategoryMapping(18, TorznabCatType.Audio); // Musik
            AddCategoryMapping(19, TorznabCatType.AudioVideo); // Musik Videos
            AddCategoryMapping(44, TorznabCatType.TVOTHER); // Serie/DVD/R
            AddCategoryMapping(22, TorznabCatType.TVHD); // Serie/HDTV
            AddCategoryMapping(38, TorznabCatType.TV); // Serie/Pack
            AddCategoryMapping(23, TorznabCatType.TVOTHER); // Serie/XviD,DivX,h264
            AddCategoryMapping(25, TorznabCatType.TVSport); // Sport
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string>
            {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value }
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, LoginUrl);
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("logout.php"), () =>
                {
                    CQ dom = result.Content;
                    var errorMessage = dom["#login_error"].Text().Trim();
                    throw new ExceptionWithConfigData(errorMessage, configData);
                });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            
            var searchString = query.GetQueryString();
            var searchUrl = BrowseUrl;
            var queryCollection = new NameValueCollection();
            queryCollection.Add("showsearch", "0");
            queryCollection.Add("incldead", "1");
            queryCollection.Add("blah", "0");
            queryCollection.Add("team", "0");
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

            var response = await RequestStringWithCookiesAndRetry(searchUrl, null, BrowseUrl);
            var results = response.Content;
            try
            {
                CQ dom = results;
                var rows = dom["table.tableinborder[cellpadding=0] > tbody > tr"];

                foreach (var row in rows)
                {
                    var release = new ReleaseInfo();
                    release.MinimumRatio = 0.7;
                    release.MinimumSeedTime = 48 * 60 * 60;
                    release.DownloadVolumeFactor = 1;
                    release.UploadVolumeFactor = 1;

                    var qRow = row.Cq();
                    var flagImgs = qRow.Find("table tbody tr: eq(0) td > img");
                    List<string> flags = new List<string>();
                    flagImgs.Each(flagImg => {
                        var flag = flagImg.GetAttribute("src").Replace("pic/torrent_", "").Replace(".gif", "").ToUpper();
                        if (flag == "OU")
                            release.DownloadVolumeFactor = 0;
                        else
                            flags.Add(flag);
                    });
                        
                    var titleLink = qRow.Find("table tbody tr:eq(0) td a:has(b)").First();
                    var DLLink = qRow.Find("td.tableb > a:has(img[title=\"Torrent herunterladen\"])").First();
                    release.Comments = new Uri(SiteLink + titleLink.Attr("href").Replace("&hit=1", ""));
                    release.Link = new Uri(SiteLink + DLLink.Attr("href"));
                    release.Title = titleLink.Text().Trim();

                    if (!query.MatchQueryStringAND(release.Title))
                        continue;

                    release.Description = String.Join(", ", flags);
                    release.Guid = release.Link;

                    var dateStr = qRow.Find("table tbody tr:eq(1) td:eq(4)").Html().Replace("&nbsp;", " ").Trim();
                    var dateGerman = DateTime.SpecifyKind(DateTime.ParseExact(dateStr, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture), DateTimeKind.Unspecified);
                    DateTime pubDateUtc = TimeZoneInfo.ConvertTimeToUtc(dateGerman, germanyTz);
                    release.PublishDate = pubDateUtc.ToLocalTime();

                    var sizeStr = qRow.Find("table tbody tr:eq(1) td b").First().Text().Trim();
                    release.Size = ReleaseInfo.GetBytes(sizeStr.Replace(",", "."));

                    release.Seeders = ParseUtil.CoerceInt(qRow.Find("table tbody tr:eq(1) td:eq(1) b:eq(0) font").Text().Trim());
                    release.Peers = ParseUtil.CoerceInt(qRow.Find("table tbody tr:eq(1) td:eq(1) b:eq(1) font").Text().Trim()) + release.Seeders;

                    var catId = qRow.Find("td:eq(0) a").First().Attr("href").Split('=')[1];
                    release.Category = MapTrackerCatToNewznab(catId);

                    var files = qRow.Find("td:has(a[href*=\"&filelist=1\"])> b:nth-child(2)").Text();
                    release.Files = ParseUtil.CoerceInt(files);

                    var grabs = qRow.Find("td:has(a[href*=\"&tosnatchers=1\"])> b:nth-child(1)").Text();
                    release.Grabs = ParseUtil.CoerceInt(grabs);

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

