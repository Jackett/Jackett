using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
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
    public class TorrentBytes : BaseWebIndexer
    {
        private string BrowseUrl => SiteLink + "browse.php";
        private string LoginUrl => SiteLink + "takelogin.php";

        private new ConfigurationDataBasicLogin configData
        {
            get => (ConfigurationDataBasicLogin)base.configData;
            set => base.configData = value;
        }

        public TorrentBytes(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(name: "TorrentBytes",
                description: "A decade of torrentbytes",
                link: "https://www.torrentbytes.net/",
                caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                configService: configService,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLogin("For best results, change the 'Torrents per page' setting to 30 or greater (100 recommended) in your profile on the TorrentBytes webpage."))
        {
            Encoding = Encoding.GetEncoding("iso-8859-1");
            Language = "en-us";
            Type = "private";

            AddCategoryMapping(23, TorznabCatType.TVAnime, "Anime");
            AddCategoryMapping(52, TorznabCatType.PCMac, "Apple/All");
            AddCategoryMapping(22, TorznabCatType.PC, "Apps/misc");
            AddCategoryMapping(1, TorznabCatType.PC, "Apps/PC");
            AddCategoryMapping(28, TorznabCatType.TVFOREIGN, "Foreign Titles");
            AddCategoryMapping(50, TorznabCatType.Console, "Games/Consoles");
            AddCategoryMapping(42, TorznabCatType.PCGames, "Games/Pack");
            AddCategoryMapping(4, TorznabCatType.PCGames, "Games/PC");
            AddCategoryMapping(51, TorznabCatType.PC, "Linux/All");
            AddCategoryMapping(31, TorznabCatType.OtherMisc, "Misc");
            AddCategoryMapping(20, TorznabCatType.MoviesDVD, "Movies/DVD-R");
            AddCategoryMapping(12, TorznabCatType.MoviesBluRay, "Movies/Full Blu-ray");
            AddCategoryMapping(5, TorznabCatType.MoviesHD, "Movies/HD");
            AddCategoryMapping(40, TorznabCatType.Movies, "Movies/Pack");
            AddCategoryMapping(19, TorznabCatType.MoviesSD, "Movies/SD");
            AddCategoryMapping(49, TorznabCatType.MoviesUHD, "Movies/UHD");
            AddCategoryMapping(25, TorznabCatType.Audio, "Music/DVDR");
            AddCategoryMapping(48, TorznabCatType.AudioLossless, "Music/Flac");
            AddCategoryMapping(6, TorznabCatType.AudioMP3, "Music/MP3");
            AddCategoryMapping(43, TorznabCatType.Audio, "Music/Pack");
            AddCategoryMapping(34, TorznabCatType.AudioVideo, "Music/Videos");
            AddCategoryMapping(45, TorznabCatType.MoviesBluRay, "NonScene/BRrip");
            AddCategoryMapping(46, TorznabCatType.MoviesHD, "NonScene/x264");
            AddCategoryMapping(44, TorznabCatType.MoviesSD, "NonScene/Xvid");
            AddCategoryMapping(37, TorznabCatType.TVHD, "TV/BRrip");
            AddCategoryMapping(38, TorznabCatType.TVHD, "TV/HD");
            AddCategoryMapping(41, TorznabCatType.TV, "TV/Pack");
            AddCategoryMapping(33, TorznabCatType.TVSD, "TV/SD");
            AddCategoryMapping(32, TorznabCatType.TVUHD, "TV/UHD");
            AddCategoryMapping(39, TorznabCatType.XXXx264, "XXX/HD");
            AddCategoryMapping(24, TorznabCatType.XXXImageset, "XXX/IMGSET");
            AddCategoryMapping(21, TorznabCatType.XXXPacks, "XXX/Pack");
            AddCategoryMapping(9, TorznabCatType.XXXXviD, "XXX/SD");
            AddCategoryMapping(29, TorznabCatType.XXX, "XXX/Web");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "returnto", "/" },
                { "login", "Log in!" }
            };

            var loginPage = await RequestStringWithCookies(SiteLink, string.Empty);

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, loginPage.Cookies, true, SiteLink, SiteLink);
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("my.php"), () =>
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(result.Content);
                var messageEl = dom.QuerySelector("td.embedded");
                var errorMessage = messageEl != null ? messageEl.TextContent : result.Content;
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();
            var searchUrl = BrowseUrl;
            var trackerCats = MapTorznabCapsToTrackers(query);
            var queryCollection = new NameValueCollection();

            // Tracker can only search OR return things in categories
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                queryCollection.Add("search", searchString);
                queryCollection.Add("cat", "0");
                queryCollection.Add("sc", "1");
            }
            else
            {
                foreach (var cat in MapTorznabCapsToTrackers(query))
                {
                    queryCollection.Add("c" + cat, "1");
                }

                queryCollection.Add("incldead", "0");
            }

            searchUrl += "?" + queryCollection.GetQueryString();

            await ProcessPage(releases, searchUrl);
            return releases;
        }

        private async Task ProcessPage(List<ReleaseInfo> releases, string searchUrl)
        {
            var response = await RequestStringWithCookiesAndRetry(searchUrl, null, BrowseUrl);
            // On IP change the cookies become invalid, login again and retry
            if (response.IsRedirect)
            {
                await ApplyConfiguration(null);
                response = await RequestStringWithCookiesAndRetry(searchUrl, null, BrowseUrl);
            }

            try
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(response.Content);
                var rows = dom.QuerySelectorAll("table > tbody:has(tr > td.colhead) > tr:not(:has(td.colhead))");
                foreach (var row in rows)
                {
                    var release = new ReleaseInfo();

                    var link = row.QuerySelector("td:nth-of-type(2) a:nth-of-type(2)");
                    release.Guid = new Uri(SiteLink + link.GetAttribute("href"));
                    release.Comments = release.Guid;
                    release.Title = link.GetAttribute("title");

                    // There isn't a title attribute if the release name isn't truncated.
                    if (string.IsNullOrWhiteSpace(release.Title))
                        release.Title = link.FirstChild.ToString().Trim();

                    release.Description = release.Title;

                    // If we search an get no results, we still get a table just with no info.
                    if (string.IsNullOrWhiteSpace(release.Title))
                        break;

                    // Check if the release has been assigned a category
                    var qCat = row.QuerySelector("td:nth-of-type(1) a");
                    if (qCat != null)
                    {
                        var cat = qCat.GetAttribute("href").Substring(15);
                        release.Category = MapTrackerCatToNewznab(cat);
                    }

                    var qLink = row.QuerySelector("td:nth-of-type(2) a");
                    release.Link = new Uri(SiteLink + qLink.GetAttribute("href"));

                    var added = row.QuerySelector("td:nth-of-type(5)").TextContent.Trim();
                    release.PublishDate = DateTime.ParseExact(added, "yyyy-MM-ddHH:mm:ss", CultureInfo.InvariantCulture);

                    var sizeStr = row.QuerySelector("td:nth-of-type(7)").TextContent.Trim();
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    release.Seeders = ParseUtil.CoerceInt(row.QuerySelector("td:nth-of-type(9)").TextContent.Trim());
                    release.Peers = ParseUtil.CoerceInt(row.QuerySelector("td:nth-of-type(10)").TextContent.Trim()) + release.Seeders;

                    var files = row.QuerySelector("td:nth-child(3)").TextContent;
                    release.Files = ParseUtil.CoerceInt(files);

                    var grabs = row.QuerySelector("td:nth-child(8)").TextContent;
                    if (grabs != "----")
                        release.Grabs = ParseUtil.CoerceInt(grabs);

                    release.DownloadVolumeFactor = row.QuerySelector("font[color=\"green\"]:contains(\"F\"):contains(\"L\")") != null ? 0 : 1;
                    release.UploadVolumeFactor = 1;

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(response.Content, ex);
            }
        }
    }
}
