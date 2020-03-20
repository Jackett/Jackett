using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
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
    public class HDTorrents : BaseWebIndexer
    {
        private string SearchUrl => SiteLink + "torrents.php?";
        private string LoginUrl => SiteLink + "login.php";

        public override string[] AlternativeSiteLinks { get; protected set; } =
        {
            "https://hdts.ru/",
            "https://hd-torrents.org/",
            "https://hd-torrents.net/",
            "https://hd-torrents.me/"
        };

        private new ConfigurationDataBasicLogin configData
        {
            get => (ConfigurationDataBasicLogin)base.configData;
            set => base.configData = value;
        }

        public HDTorrents(IIndexerConfigurationService configService, WebClient w, Logger l, IProtectionService ps)
            : base(name: "HD-Torrents",
                description: "HD-Torrents is a private torrent website with HD torrents and strict rules on their content.",
                link: "https://hdts.ru/",// Of the accessible domains the .ru seems the most reliable.  https://hdts.ru | https://hd-torrents.org | https://hd-torrents.net | https://hd-torrents.me
                configService: configService,
                client: w,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLogin())
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";

            TorznabCaps.SupportsImdbMovieSearch = true;
            TorznabCaps.SupportsImdbTVSearch = true;

            TorznabCaps.Categories.Clear();

            // Movie
            AddCategoryMapping("70", TorznabCatType.MoviesUHD, "Movie/UHD/Blu-Ray");
            AddCategoryMapping("1", TorznabCatType.MoviesHD, "Movie/Blu-Ray");
            AddCategoryMapping("71", TorznabCatType.MoviesUHD, "Movie/UHD/Remux");
            AddCategoryMapping("2", TorznabCatType.MoviesHD, "Movie/Remux");
            AddCategoryMapping("5", TorznabCatType.MoviesHD, "Movie/1080p/i");
            AddCategoryMapping("3", TorznabCatType.MoviesHD, "Movie/720p");
            AddCategoryMapping("64", TorznabCatType.MoviesUHD, "Movie/2160p");
            AddCategoryMapping("63", TorznabCatType.Audio, "Movie/Audio Track");
            // TV Show

            AddCategoryMapping("72", TorznabCatType.TVUHD, "TV Show/UHD/Blu-ray");
            AddCategoryMapping("59", TorznabCatType.TVHD, "TV Show/Blu-ray");
            AddCategoryMapping("73", TorznabCatType.TVUHD, "TV Show/UHD/Remux");
            AddCategoryMapping("60", TorznabCatType.TVHD, "TV Show/Remux");
            AddCategoryMapping("30", TorznabCatType.TVHD, "TV Show/1080p/i");
            AddCategoryMapping("38", TorznabCatType.TVHD, "TV Show/720p");
            AddCategoryMapping("65", TorznabCatType.TVUHD, "TV Show/2160p");
            // Music
            AddCategoryMapping("44", TorznabCatType.Audio, "Music/Album");
            AddCategoryMapping("61", TorznabCatType.AudioVideo, "Music/Blu-Ray");
            AddCategoryMapping("62", TorznabCatType.AudioVideo, "Music/Remux");
            AddCategoryMapping("57", TorznabCatType.AudioVideo, "Music/1080p/i");
            AddCategoryMapping("45", TorznabCatType.AudioVideo, "Music/720p");
            AddCategoryMapping("66", TorznabCatType.AudioVideo, "Music/2160p");
            // XXX
            AddCategoryMapping("58", TorznabCatType.XXX, "XXX/Blu-ray");
            AddCategoryMapping("74", TorznabCatType.XXX, "XXX/UHD/Blu-ray");
            AddCategoryMapping("48", TorznabCatType.XXX, "XXX/1080p/i");
            AddCategoryMapping("47", TorznabCatType.XXX, "XXX/720p");
            AddCategoryMapping("67", TorznabCatType.XXX, "XXX/2160p");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var loginPage = await RequestStringWithCookies(LoginUrl, string.Empty);

            var pairs = new Dictionary<string, string> {
                { "uid", configData.Username.Value },
                { "pwd", configData.Password.Value }
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, loginPage.Cookies, true, null, LoginUrl);

            await ConfigureIfOK(
                result.Cookies, result.Content?.Contains("If your browser doesn't have javascript enabled") == true,
                () => throw new ExceptionWithConfigData("Couldn't login", configData));
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchUrl = SearchUrl;// string.Format(SearchUrl, HttpUtility.UrlEncode()));
            searchUrl += string.Join(string.Empty, MapTorznabCapsToTrackers(query).Select(cat => $"category[]={cat}&"));
            var queryCollection = new NameValueCollection
            {
                {"search", query.ImdbID ?? query.GetQueryString()},
                {"active", "0"},
                {"options", "0"}
            };

            // manually url encode parenthesis to prevent "hacking" detection
            searchUrl += queryCollection.GetQueryString().Replace("(", "%28").Replace(")", "%29");

            var results = await RequestStringWithCookiesAndRetry(searchUrl);
            try
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(results.Content);

                var userInfo = dom.QuerySelector("table.navus tr");
                var rank = userInfo.Children[1].TextContent.Replace("Rank:", string.Empty).Trim();

                var freeleechRanks = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "VIP",
                    "Uploader",
                    "HD Internal",
                    "Moderator",
                    "Administrator",
                    "Owner"
                };
                var hasFreeleech = freeleechRanks.Contains(rank);

                var rows = dom.QuerySelectorAll("table.mainblockcontenttt tr:has(td.mainblockcontent)").Skip(1);
                foreach (var row in rows)
                {
                    var release = new ReleaseInfo();

                    var mainLink = row.Children[2].QuerySelector("a");

                    release.Title = row.Children[2].QuerySelector("a").TextContent;
                    release.Description = row.Children[2].QuerySelector("span").TextContent;

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800; // 48 hours

                    var tdIndex = 0;

                    //Maybe replace with something like this in the future? Can't test this
                    //var editRow = row.Children.FirstOrDefault(node => string.Equals(
                    //                                              node.TextContent, "Edit",
                    //                                              StringComparison.OrdinalIgnoreCase));
                    //var index = row.Children.Index(editRow);
                    if (row.QuerySelector("td:nth-last-child(1)").TextContent == "Edit")
                        tdIndex = 1;
                    // moderators get additional delete, recommend and like links
                    if (row.QuerySelector("td:nth-last-child(4)").TextContent == "Edit")
                        tdIndex = 4;

                    // Sometimes the uploader column is missing
                    if (ParseUtil.TryCoerceInt(row.QuerySelector($"td:nth-last-child({tdIndex + 3})").TextContent, out var seeders))
                    {
                        release.Seeders = seeders;
                        if (ParseUtil.TryCoerceInt(row.QuerySelector($"td:nth-last-child({tdIndex + 2})").TextContent, out var peers))
                            release.Peers = peers + release.Seeders;
                    }

                    // Sometimes the grabs column is missing
                    if (ParseUtil.TryCoerceLong(row.QuerySelector($"td:nth-last-child({tdIndex + 1})").TextContent, out var grabs))
                        release.Grabs = grabs;

                    var fullSize = row.Children[7].TextContent;
                    release.Size = ReleaseInfo.GetBytes(fullSize);

                    release.Guid = new Uri(SiteLink + mainLink.GetAttribute("href"));
                    release.Link = new Uri(SiteLink + row.Children[4].FirstElementChild.GetAttribute("href"));
                    release.Comments = new Uri(SiteLink + mainLink.GetAttribute("href"));

                    var dateTag = row.Children[6].FirstElementChild;
                    var dateString = string.Join(" ", dateTag.Attributes.Select(attr => attr.Name));
                    release.PublishDate = DateTime.ParseExact(dateString, "dd MMM yyyy HH:mm:ss zz00", CultureInfo.InvariantCulture).ToLocalTime();

                    //var catLink = new Uri(row.Children[0].FirstElementChild.GetAttribute("href"));
                    //var catQuery = HttpUtility.ParseQueryString(catLink.Query);
                    //release.Category = MapTrackerCatToNewznab(catQuery["category"]);
                    var category = row.FirstElementChild.FirstElementChild.GetAttribute("href").Split('=')[1];
                    release.Category = MapTrackerCatToNewznab(category);

                    release.UploadVolumeFactor = 1;

                    if (row.QuerySelector("img[alt=\"Free Torrent\"]") != null)
                    {
                        release.DownloadVolumeFactor = 0;
                        release.UploadVolumeFactor = 0;
                    }
                    else if (hasFreeleech || row.QuerySelector("img[alt=\"Golden Torrent\"]") != null)
                        release.DownloadVolumeFactor = 0;
                    else if (row.QuerySelector("img[alt=\"Silver Torrent\"]") != null)
                        release.DownloadVolumeFactor = 0.5;
                    else if (row.QuerySelector("img[alt=\"Bronze Torrent\"]") != null)
                        release.DownloadVolumeFactor = 0.75;
                    else if (row.QuerySelector("img[alt=\"Blue Torrent\"]") != null)
                        release.DownloadVolumeFactor = 0.25;
                    else
                        release.DownloadVolumeFactor = 1;

                    var imdbLink = row.QuerySelector("a[href*=\"www.imdb.com/title/\"]")?.GetAttribute("href");
                    if (!string.IsNullOrWhiteSpace(imdbLink))
                        release.Imdb = ParseUtil.GetLongFromString(imdbLink);

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
