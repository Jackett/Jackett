using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public class SpeedCD : BaseWebIndexer
    {
        private string SearchUrl => SiteLink + "browse.php";

        public override string[] AlternativeSiteLinks { get; protected set; } = {
            "https://speed.cd/",
            "https://speed.click/"
        };

        private new ConfigurationDataCookie configData => (ConfigurationDataCookie)base.configData;

        public SpeedCD(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base("Speed.cd",
                   description: "Your home now!",
                   link: "https://speed.cd/",
                   caps: new TorznabCapabilities
                   {
                       SupportsImdbMovieSearch = true
                       // SupportsImdbTVSearch = true (supported by the site but disabled due to #8107)
                   },
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   configData: new ConfigurationDataCookie(
                       @"Speed.Cd have increased their security. If you are having problems please check the security tab
                    in your Speed.Cd profile. Eg. Geo Locking, your seedbox may be in a different country to the one where you login via your
                    web browser.<br><br>For best results, change the 'Torrents per page' setting to 100 in 'Profile Settings > Torrents'."))
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";

            AddCategoryMapping(1, TorznabCatType.MoviesOther, "Movies/XviD");
            AddCategoryMapping(42, TorznabCatType.Movies, "Movies/Packs");
            AddCategoryMapping(32, TorznabCatType.Movies, "Movies/Kids");
            AddCategoryMapping(43, TorznabCatType.MoviesHD, "Movies/HD");
            AddCategoryMapping(47, TorznabCatType.Movies, "Movies/DiVERSiTY");
            AddCategoryMapping(28, TorznabCatType.MoviesBluRay, "Movies/B-Ray");
            AddCategoryMapping(48, TorznabCatType.Movies3D, "Movies/3D");
            AddCategoryMapping(40, TorznabCatType.MoviesDVD, "Movies/DVD-R");
            AddCategoryMapping(56, TorznabCatType.Movies, "Movies/Anime");
            AddCategoryMapping(50, TorznabCatType.TVSport, "TV/Sports");
            AddCategoryMapping(52, TorznabCatType.TVHD, "TV/B-Ray");
            AddCategoryMapping(53, TorznabCatType.TVSD, "TV/DVD-R");
            AddCategoryMapping(41, TorznabCatType.TV, "TV/Packs");
            AddCategoryMapping(55, TorznabCatType.TV, "TV/Kids");
            AddCategoryMapping(57, TorznabCatType.TV, "TV/DiVERSiTY");
            AddCategoryMapping(49, TorznabCatType.TVHD, "TV/HD");
            AddCategoryMapping(2, TorznabCatType.TVSD, "TV/Episodes");
            AddCategoryMapping(30, TorznabCatType.TVAnime, "TV/Anime");
            AddCategoryMapping(25, TorznabCatType.PCISO, "Games/PC ISO");
            AddCategoryMapping(39, TorznabCatType.ConsoleWii, "Games/Wii");
            AddCategoryMapping(45, TorznabCatType.ConsolePS3, "Games/PS3");
            AddCategoryMapping(35, TorznabCatType.Console, "Games/Nintendo");
            AddCategoryMapping(33, TorznabCatType.ConsoleXbox360, "Games/XboX360");
            AddCategoryMapping(46, TorznabCatType.PCPhoneOther, "Mobile");
            AddCategoryMapping(24, TorznabCatType.PC0day, "Apps/0DAY");
            AddCategoryMapping(51, TorznabCatType.PCMac, "Mac");
            AddCategoryMapping(54, TorznabCatType.Books, "Educational");
            AddCategoryMapping(27, TorznabCatType.Books, "Books-Mags");
            AddCategoryMapping(26, TorznabCatType.Audio, "Music/Audio");
            AddCategoryMapping(44, TorznabCatType.Audio, "Music/Pack");
            AddCategoryMapping(29, TorznabCatType.AudioVideo, "Music/Video");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            CookieHeader = configData.Cookie.Value;
            try
            {
                var results = await PerformQuery(new TorznabQuery());
                if (!results.Any())
                    throw new Exception("Found 0 results in the tracker");
                IsConfigured = true;
                SaveConfig();
                return IndexerConfigurationStatus.Completed;
            }
            catch (Exception e)
            {
                IsConfigured = false;
                throw new Exception("Your cookie did not work: " + e.Message);
            }
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            var qc = new List<KeyValuePair<string, string>>(); // NameValueCollection don't support c[]=30&c[]=52
            if (query.IsImdbQuery)
            {
                qc.Add("search", query.ImdbID);
                qc.Add("d", "on");
            }
            else
                qc.Add("search", query.GetQueryString());

            var catList = MapTorznabCapsToTrackers(query);
            foreach (var cat in catList)
                qc.Add("c[]", cat);

            var searchUrl = SearchUrl + "?" + qc.GetQueryString();
            var response = await RequestStringWithCookiesAndRetry(searchUrl);
            if (!response.Content.Contains("/logout.php"))
                throw new Exception("The user is not logged in. It is possible that the cookie has expired or you " +
                                    "made a mistake when copying it. Please check the settings.");

            try
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(response.Content);
                var rows = dom.QuerySelectorAll("div.boxContent > table > tbody > tr");

                foreach (var row in rows)
                {
                    var cells = row.QuerySelectorAll("td");

                    var title = row.QuerySelector("td[class='lft'] > div > a").TextContent.Trim();
                    var link = new Uri(SiteLink + row.QuerySelector("img[title='Download']").ParentElement.GetAttribute("href").TrimStart('/'));
                    var comments = new Uri(SiteLink + row.QuerySelector("td[class='lft'] > div > a").GetAttribute("href").TrimStart('/'));
                    var size = ReleaseInfo.GetBytes(cells[5].TextContent);
                    var grabs = ParseUtil.CoerceInt(cells[6].TextContent);
                    var seeders = ParseUtil.CoerceInt(cells[7].TextContent);
                    var leechers = ParseUtil.CoerceInt(cells[8].TextContent);

                    var pubDateStr = row.QuerySelector("span[class^='elapsedDate']").GetAttribute("title").Replace(" at", "");
                    var publishDate = DateTime.ParseExact(pubDateStr, "dddd, MMMM d, yyyy h:mmtt", CultureInfo.InvariantCulture);

                    var cat = row.QuerySelector("a").GetAttribute("href").Split('/').Last();
                    var downloadVolumeFactor = row.QuerySelector("span:contains(\"[Freeleech]\")") != null ? 0 : 1;

                    var release = new ReleaseInfo
                    {
                        Title = title,
                        Link = link,
                        Guid = link,
                        Comments = comments,
                        PublishDate = publishDate,
                        Category = MapTrackerCatToNewznab(cat),
                        Size = size,
                        Grabs = grabs,
                        Seeders = seeders,
                        Peers = seeders + leechers,
                        MinimumRatio = 1,
                        MinimumSeedTime = 172800, // 48 hours
                        DownloadVolumeFactor = downloadVolumeFactor,
                        UploadVolumeFactor = 1
                    };

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(response.Content, ex);
            }
            return releases;
        }
    }
}
