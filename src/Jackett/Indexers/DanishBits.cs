using CsQuery;
using Jackett.Models;
using Jackett.Services;
using Jackett.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using CsQuery.ExtensionMethods;
using Jackett.Models.IndexerConfig;
using Jackett.Utils;

namespace Jackett.Indexers
{
    public class DanishBits : BaseWebIndexer
    {
        private string LoginUrl { get { return SiteLink + "login.php"; } }
        private string SearchUrl { get { return SiteLink + "torrents.php"; } }

        new NxtGnConfigurationData configData
        {
            get { return (NxtGnConfigurationData)base.configData; }
            set { base.configData = value; }
        }

        public DanishBits(IIndexerConfigurationService configService, IWebClient c, Logger l, IProtectionService ps)
            : base(name: "DanishBits",
                description: "A danish closed torrent tracker",
                link: "https://danishbits.org/",
                caps: new TorznabCapabilities(),
                configService: configService,
                client: c,
                logger: l,
                p: ps,
                configData: new NxtGnConfigurationData())
        {
            Encoding = Encoding.GetEncoding("UTF-8");
            Language = "da-dk";
            Type = "private";

            // Movies Mapping
            // DanishBits HD
            AddCategoryMapping(2, TorznabCatType.MoviesHD);
            AddCategoryMapping(2, TorznabCatType.MoviesWEBDL);

            // Danske film
            AddCategoryMapping(3, TorznabCatType.MoviesHD);
            AddCategoryMapping(3, TorznabCatType.MoviesWEBDL);
            AddCategoryMapping(3, TorznabCatType.MoviesDVD);
            AddCategoryMapping(3, TorznabCatType.MoviesForeign);
            AddCategoryMapping(3, TorznabCatType.MoviesSD);

            // DVDR Nordic
            AddCategoryMapping(10, TorznabCatType.MoviesDVD);
            AddCategoryMapping(10, TorznabCatType.MoviesForeign);

            // Custom
            AddCategoryMapping(28, TorznabCatType.MoviesHD);
            AddCategoryMapping(28, TorznabCatType.MoviesDVD);

            // Custom HD
            AddCategoryMapping(29, TorznabCatType.MoviesHD);
            AddCategoryMapping(29, TorznabCatType.MoviesWEBDL);

            // Custom Tablet
            AddCategoryMapping(31, TorznabCatType.MoviesSD);

            if (!configData.OnlyDanishCategories.Value)
            {
                // Bluray
                AddCategoryMapping(8, TorznabCatType.MoviesBluRay);

                // Boxset
                AddCategoryMapping(9, TorznabCatType.MoviesHD);
                AddCategoryMapping(9, TorznabCatType.MoviesForeign);
                AddCategoryMapping(9, TorznabCatType.MoviesDVD);

                // DVDR
                AddCategoryMapping(11, TorznabCatType.MoviesDVD);

                // HDx264
                AddCategoryMapping(22, TorznabCatType.MoviesHD);

                // XviD/MP4/SDx264
                AddCategoryMapping(24, TorznabCatType.MoviesSD);
            }

            // TV Mapping
            // DanishBits TV
            AddCategoryMapping(1, TorznabCatType.TVHD);
            AddCategoryMapping(1, TorznabCatType.TVWEBDL);

            // Dansk TV
            AddCategoryMapping(4, TorznabCatType.TVHD);
            AddCategoryMapping(4, TorznabCatType.TVWEBDL);
            AddCategoryMapping(4, TorznabCatType.TVFOREIGN);
            AddCategoryMapping(4, TorznabCatType.TVSD);

            // Custom TV
            AddCategoryMapping(30, TorznabCatType.TVHD);
            AddCategoryMapping(30, TorznabCatType.TVWEBDL);

            if (!configData.OnlyDanishCategories.Value)
            {
                // TV
                AddCategoryMapping(20, TorznabCatType.TVHD);
                AddCategoryMapping(20, TorznabCatType.TVSD);
                AddCategoryMapping(20, TorznabCatType.TVWEBDL);

                // TV Boxset
                AddCategoryMapping(21, TorznabCatType.TVHD);
                AddCategoryMapping(21, TorznabCatType.TVSD);
                AddCategoryMapping(21, TorznabCatType.TVWEBDL);
            }

            // E-book
            AddCategoryMapping(12, TorznabCatType.BooksEbook);
            // Audiobooks
            AddCategoryMapping(6, TorznabCatType.AudioAudiobook);
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "langlang", null },
                { "login", "login" }
            };
            // Get inital cookies
            CookieHeader = string.Empty;
            var response = await RequestLoginAndFollowRedirect(LoginUrl, pairs, CookieHeader, true, null, LoginUrl);

            await ConfigureIfOK(response.Cookies, response.Content != null && response.Content.Contains("logout.php"), () =>
            {
                CQ dom = response.Content;
                var messageEl = dom["#loginform .warning"];
                var errorMessage = messageEl.Text().Trim();
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            TimeZoneInfo.TransitionTime startTransition = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 2, 0, 0), 3, 5, DayOfWeek.Sunday);
            TimeZoneInfo.TransitionTime endTransition = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 3, 0, 0), 10, 5, DayOfWeek.Sunday);
            TimeSpan delta = new TimeSpan(1, 0, 0);
            TimeZoneInfo.AdjustmentRule adjustment = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(new DateTime(1999, 10, 1), DateTime.MaxValue.Date, delta, startTransition, endTransition);
            TimeZoneInfo.AdjustmentRule[] adjustments = { adjustment };
            TimeZoneInfo denmarkTz = TimeZoneInfo.CreateCustomTimeZone("Denmark Time", new TimeSpan(1, 0, 0), "(GMT+01:00) Denmark Time", "Denmark Time", "Denmark DST", adjustments);

            var releasesPerPage = 100;
            var releases = new List<ReleaseInfo>();

            var page = (query.Offset / releasesPerPage) + 1;

            string episodeSearchUrl;
            if (string.IsNullOrEmpty(query.GetQueryString()))
            {
                episodeSearchUrl = SearchUrl + "?page=" + page;
            }
            else
            {
                var cats = MapTorznabCapsToTrackers(query);
                var catsUrlPart = string.Join("&", cats.Select(c => $"filter_{c}=on"));
                episodeSearchUrl = $"{SearchUrl}?page={page}&group=0&{catsUrlPart}&search={HttpUtility.UrlEncode(query.GetQueryString())}&pre_type=torrents&type=";
            }
            var results = await RequestStringWithCookiesAndRetry(episodeSearchUrl);
            if (string.IsNullOrEmpty(results.Content))
            {
                CookieHeader = string.Empty;
                var pairs = new Dictionary<string, string>
                {
                    {"username", configData.Username.Value},
                    {"password", configData.Password.Value},
                    {"langlang", null},
                    {"login", "login"}
                };
                var response = await RequestLoginAndFollowRedirect(LoginUrl, pairs, CookieHeader, true, null, LoginUrl);

                await ConfigureIfOK(response.Cookies, response.Content != null && response.Content.Contains("logout.php"), () =>
                {
                    CQ dom = response.Content;
                    var messageEl = dom["#loginform .warning"];
                    var errorMessage = messageEl.Text().Trim();
                    throw new ExceptionWithConfigData(errorMessage, configData);
                });
                results = await RequestStringWithCookiesAndRetry(episodeSearchUrl);
            }
            try
            {
                CQ dom = results.Content;
                var rows = dom["#torrent_table tr.torrent"];
                foreach (var row in rows)
                {
                    var qRow = row.Cq();
                    var release = new ReleaseInfo
                    {
                        MinimumRatio = 1,
                        MinimumSeedTime = 172800
                    };

                    var catAnchor = row.FirstChild.FirstChild;
                    var catUrl = catAnchor.GetAttribute("href");
                    var catStr = Regex.Match(catUrl, "filter_(?<catNo>[0-9]+)=on").Groups["catNo"].Value;
                    var catNo = int.Parse(catStr);
                    var moviesCatsDanish = new[] { 2, 3, 10, 28, 29, 31 };
                    var moviesCatsIntl = new[] { 8, 9, 11, 22, 24 };
                    var moviesCats = configData.OnlyDanishCategories.Value
                        ? moviesCatsDanish
                        : moviesCatsDanish.Concat(moviesCatsIntl);
                    var seriesCatsDanish = new[] { 1, 4, 30 };
                    var seriesCatsIntl = new[] { 20, 21 };
                    var seriesCats = configData.OnlyDanishCategories.Value
                        ? seriesCatsDanish
                        : seriesCatsDanish.Concat(seriesCatsIntl);
                    if (moviesCats.Contains(catNo))
                        release.Category = new List<int> { TorznabCatType.Movies.ID };
                    else if (seriesCats.Contains(catNo))
                        release.Category = new List<int> { TorznabCatType.TV.ID };
                    else if (catNo == 12)
                        release.Category = new List<int> { TorznabCatType.BooksEbook.ID };
                    else if (catNo == 6)
                        release.Category = new List<int> { TorznabCatType.AudioAudiobook.ID };
                    else
                        continue;

                    var titleAnchor = qRow.Find("div.croptorrenttext a").FirstElement();
                    var title = titleAnchor.GetAttribute("title");
                    release.Title = title;

                    var dlUrlAnchor = qRow.Find("span.right a[title=\"Direkte download link\"]").FirstElement();
                    var dlUrl = dlUrlAnchor.GetAttribute("href");
                    release.Link = new Uri(SiteLink + dlUrl);

                    var torrentLink = titleAnchor.GetAttribute("href");
                    release.Guid = new Uri(SiteLink + torrentLink);
                    release.Comments = new Uri(SearchUrl + torrentLink);

                    var addedElement = qRow.Find("span.time").FirstElement();
                    var addedStr = addedElement.GetAttribute("title");
                    release.PublishDate = TimeZoneInfo.ConvertTimeToUtc(DateTime.ParseExact(addedStr, "MMM dd yyyy, HH:mm", CultureInfo.InvariantCulture), denmarkTz).ToLocalTime();

                    var columns = qRow.Children();
                    var seedersElement = columns.Reverse().Skip(1).First();
                    release.Seeders = int.Parse(seedersElement.InnerText);

                    var leechersElement = columns.Last().FirstElement();
                    release.Peers = release.Seeders + int.Parse(leechersElement.InnerText);

                    var sizeElement = columns.Skip(2).First();
                    var sizeStr = sizeElement.InnerText;
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    var imdbAnchor = qRow.Find(".torrentnotes a")
                        .FirstOrDefault(a => a.GetAttribute("href").Contains("imdb.com"));
                    if (imdbAnchor != null)
                    {
                        var referrerUrl = imdbAnchor.GetAttribute("href");
                        release.Imdb = long.Parse(Regex.Match(referrerUrl, "tt(?<imdbId>[0-9]+)").Groups["imdbId"].Value);
                    }

                    var Files = qRow.Find("td:nth-child(3) > div");
                    release.Files = ParseUtil.CoerceLong(Files.Text().Split(' ')[0]);

                    var Grabs = qRow.Find("td:nth-child(6)");
                    release.Grabs = ParseUtil.CoerceLong(Grabs.Text());

                    if (qRow.Find("span.freeleech, img[src=\"/static/common/torrents/gratis.png\"]").Length >= 1)
                        release.DownloadVolumeFactor = 0;
                    else
                        release.DownloadVolumeFactor = 1;

                    if (qRow.Find("img[src=\"/static/common/torrents/toxupload.png\"]").Length >= 1)
                        release.UploadVolumeFactor = 2;
                    else
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

        public class NxtGnConfigurationData : ConfigurationData
        {
            public NxtGnConfigurationData()
            {
                Username = new StringItem { Name = "Username" };
                Password = new StringItem { Name = "Password" };
                OnlyDanishCategories = new BoolItem { Name = "Only Danish Categories" };
            }
            public StringItem Username { get; private set; }
            public StringItem Password { get; private set; }
            public BoolItem OnlyDanishCategories { get; private set; }
        }
    }
}
