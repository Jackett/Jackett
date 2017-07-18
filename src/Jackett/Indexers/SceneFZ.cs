using Jackett.Utils.Clients;
using NLog;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Models;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using CsQuery;
using System.Web;
using System;
using System.Text;
using System.Globalization;
using Jackett.Models.IndexerConfig;

namespace Jackett.Indexers
{
    public class SceneFZ : BaseWebIndexer
    {
        string LoginUrl { get { return SiteLink + "takelogin.php"; } }

        string BrowseUrl { get { return SiteLink + "ajax_browse.php"; } }

        new ConfigurationDataBasicLogin configData
        {
            get { return (ConfigurationDataBasicLogin)base.configData; }
            set { base.configData = value; }
        }

        public SceneFZ(IIndexerConfigurationService configService, IWebClient wc, Logger l, IProtectionService ps)
            : base(name: "SceneFZ",
                   description: "Torrent tracker. Tracking over 50.000 torrent files.",
                   link: "http://scenefz.me/",
                   caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   configData: new ConfigurationDataBasicLogin())
        {
            Encoding = Encoding.UTF8;
            Language = "ro-ro";
            Type = "private";

            this.configData.Instructions.Value = "The published date is only available if you set \"Torrent Listing\" to Complex is your profile.";

            AddCategoryMapping("mc32", TorznabCatType.Movies); // Movies
            AddCategoryMapping("scat22", TorznabCatType.MoviesBluRay); // BluRay
            AddCategoryMapping("scat40", TorznabCatType.MoviesSD); // Xvid
            AddCategoryMapping("scat41", TorznabCatType.MoviesDVD); // Dvd
            AddCategoryMapping("scat47", TorznabCatType.MoviesHD); // HD
            AddCategoryMapping("scat52", TorznabCatType.MoviesDVD); // DVD-RO
            AddCategoryMapping("scat53", TorznabCatType.MoviesHD); // HD-RO
            AddCategoryMapping("scat54", TorznabCatType.MoviesBluRay); // BluRay-RO
            AddCategoryMapping("scat55", TorznabCatType.MoviesSD); // XVID-RO
            AddCategoryMapping("scat60", TorznabCatType.MoviesOther); // Sport
            AddCategoryMapping("mc33", TorznabCatType.TV); // TV
            AddCategoryMapping("scat66", TorznabCatType.TVSD); // SD
            AddCategoryMapping("scat67", TorznabCatType.TVSD); // SD-RO
            AddCategoryMapping("scat68", TorznabCatType.TVHD); // HD
            AddCategoryMapping("scat69", TorznabCatType.TVHD); // HDTV-RO
            AddCategoryMapping("mc30", TorznabCatType.Console); // Games
            AddCategoryMapping("scat58", TorznabCatType.ConsolePS3); // PS2
            AddCategoryMapping("scat16", TorznabCatType.PCGames); // Pc-Iso
            AddCategoryMapping("scat17", TorznabCatType.Console); // Misc
            AddCategoryMapping("scat18", TorznabCatType.PCGames); // Pc-Rip
            AddCategoryMapping("scat19", TorznabCatType.Console); // Consoles
            AddCategoryMapping("scat57", TorznabCatType.ConsoleXbox360); // Xbox 360
            AddCategoryMapping("scat46", TorznabCatType.Console); // Oldies
            AddCategoryMapping("scat59", TorznabCatType.ConsolePS3); // PS3
            AddCategoryMapping("mc31", TorznabCatType.PC); // Soft
            AddCategoryMapping("scat20", TorznabCatType.PC); // Pc-Iso
            AddCategoryMapping("scat21", TorznabCatType.PC); // Misc
            AddCategoryMapping("scat48", TorznabCatType.PCMac); // Mac OS
            AddCategoryMapping("mc27", TorznabCatType.Audio); // Music
            AddCategoryMapping("scat8", TorznabCatType.AudioMP3); // MP3
            AddCategoryMapping("scat45", TorznabCatType.AudioVideo); // Videoclips
            AddCategoryMapping("scat61", TorznabCatType.AudioLossless); // FLAC
            AddCategoryMapping("mc35", TorznabCatType.PCPhoneOther); // Mobile
            AddCategoryMapping("scat44", TorznabCatType.PCPhoneOther); // Misc
            AddCategoryMapping("scat64", TorznabCatType.PCPhoneIOS); // iOS
            AddCategoryMapping("scat65", TorznabCatType.PCPhoneAndroid); // Android
            AddCategoryMapping("mc28", TorznabCatType.TVAnime); // Anime
            AddCategoryMapping("scat13", TorznabCatType.TVAnime); // Tv-Eps
            AddCategoryMapping("scat12", TorznabCatType.TVAnime); // Cartoons
            AddCategoryMapping("mc29", TorznabCatType.TVDocumentary); // Docs
            AddCategoryMapping("scat14", TorznabCatType.Books); // Books
            AddCategoryMapping("scat15", TorznabCatType.Other); // Misc
            AddCategoryMapping("mc36", TorznabCatType.PC0day); // 0Day
            AddCategoryMapping("mc34", TorznabCatType.XXX); // XXX 18
            AddCategoryMapping("scat33", TorznabCatType.Other); // Images
            AddCategoryMapping("scat34", TorznabCatType.Other); // Video
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
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("Please wait..."), () =>
                {
                    CQ dom = result.Content;
                    var errorMessage = dom[".tableinborder:eq(1) td"].Text().Trim();
                    throw new ExceptionWithConfigData(errorMessage, configData);
                });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchUrl = BrowseUrl;
            var searchString = query.GetQueryString();

            var cats = MapTorznabCapsToTrackers(query);
            string cat = "a1";

            if (cats.Count == 1)
            {
                cat = cats[0];
            }

            searchUrl += string.Format("?search={0}&param_val=0&complex_search=0&incldead={1}&orderby=added&sort=desc", HttpUtility.UrlEncode(searchString), cat);

            var response = await RequestStringWithCookiesAndRetry(searchUrl, null, BrowseUrl);
            var results = response.Content;
            try
            {
                CQ dom = results;
                var rows = dom["table#torrenttable > tbody > tr:has(td.tablea), table#torrents_table > tbody > tr#torrent-row"]; // selector for old and new style

                foreach (var row in rows)
                {
                    var release = new ReleaseInfo();
                    var qRow = row.Cq();
                    var qTitleLink = qRow.Find("a[href^=\"details\"]").First();
                    if (qTitleLink.HasAttr("title"))
                        release.Title = qTitleLink.Attr("title");
                    else
                        release.Title = qTitleLink.Text();
                    release.Description = qRow.Find("small > i").Text();
                    release.Guid = new Uri(SiteLink + qTitleLink.Attr("href"));
                    release.Comments = release.Guid;

                    // date is only available with Complex listing
                    var dateStr = qRow.Find("table > tbody > tr:nth-child(2) > td:nth-child(5)").Html().Replace("&nbsp;", " ");
                    if (!string.IsNullOrEmpty(dateStr))
                        release.PublishDate = DateTime.ParseExact(dateStr + " +0200", "dd.MM.yyyy HH:mm:ss zzz", CultureInfo.InvariantCulture);

                    var qLink = qRow.Find("a[href^=\"download/\"]");
                    release.Link = new Uri(SiteLink + qLink.Attr("href"));

                    var sizeStr = qRow.Find("td[nowrap]:nth-child(3), table > tbody > tr:nth-child(2) > td:nth-child(1) > b").Text().Trim();
                    release.Size = ReleaseInfo.GetBytes(sizeStr.Replace(".", "").Replace(",", "."));

                    release.Seeders = ParseUtil.CoerceInt(qRow.Find("td > a[href*=\"&toseeders=1\"]:first-child, td:has(a[href*=\"&toseeders=1\"]) > b:nth-child(1)").Text());
                    release.Peers = ParseUtil.CoerceInt(qRow.Find("td > a[href*=\"&todlers=1\"]:last-child, a[href*=\"&toseeders=1\"] + b").Text().Replace("L.", "")) + release.Seeders;
                    release.Grabs = ParseUtil.CoerceLong(qRow.Find("td[style]:has(a[href*=\"tosnatchers=1\"])").Text().Replace(" Completed", ""));

                    release.DownloadVolumeFactor = 0;
                    release.UploadVolumeFactor = 1;

                    var catLink = qRow.Find("a[onclick^=\"bparam(\"][onclick*=\"cat\"]");
                    var catId = catLink.Attr("onclick").Split('=')[1].Replace("');", "");
                    if (!catId.StartsWith("scat"))
                        catId = "mc" + catId;
                    release.Category = MapTrackerCatToNewznab(catId);

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

