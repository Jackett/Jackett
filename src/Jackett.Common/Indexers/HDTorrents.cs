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
            var queryCollection = new NameValueCollection();
            var searchString = query.GetQueryString();

            foreach (var cat in MapTorznabCapsToTrackers(query))
                searchUrl += "category%5B%5D=" + cat + "&";

            if (query.ImdbID != null)
                queryCollection.Add("search", query.ImdbID);
            else if (!string.IsNullOrWhiteSpace(searchString))
                queryCollection.Add("search", searchString);

            queryCollection.Add("active", "0");
            queryCollection.Add("options", "0");

            searchUrl += queryCollection.GetQueryString().Replace("(", "%28").Replace(")", "%29"); // maually url encode brackets to prevent "hacking" detection

            var results = await RequestStringWithCookiesAndRetry(searchUrl);
            try
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(results.Content);

                var userInfo = dom.QuerySelector(".mainmenu > table > tbody > tr:has(td[title=\"Active-Torrents\"])");
                var rank = userInfo.QuerySelector("td:nth-child(2)").TextContent.Substring(6);

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

                var rows = dom.QuerySelectorAll(".mainblockcontenttt > tbody > tr:has(a[href^=\"details.php?id=\"])");
                foreach (var row in rows)
                {
                    var release = new ReleaseInfo();

                    release.Title = row.QuerySelector("td.mainblockcontent b a").TextContent;
                    release.Description = row.QuerySelector("td:nth-child(3) > span").TextContent;

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800; // 48 hours

                    var tdIndex = 0;
                    if (row.QuerySelector("td:nth-last-child(1)").TextContent == "Edit")
                        tdIndex = 1;
                    // moderators get additional delete, recomend and like links
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

                    var fullSize = row.QuerySelectorAll("td.mainblockcontent")[6].TextContent;
                    release.Size = ReleaseInfo.GetBytes(fullSize);

                    release.Guid = new Uri(SiteLink + row.QuerySelector("td.mainblockcontent b a").GetAttribute("href"));
                    release.Link = new Uri(SiteLink + row.QuerySelectorAll("td.mainblockcontent")[3].FirstElementChild.GetAttribute("href"));
                    release.Comments = new Uri(SiteLink + row.QuerySelector("td.mainblockcontent b a").GetAttribute("href"));

                    var dateSplit = row.QuerySelectorAll("td.mainblockcontent")[5].InnerHtml.Split(',');
                    var dateString = dateSplit[1].Substring(0, dateSplit[1].IndexOf('>')).Replace("=\"\"", "").Trim();
                    release.PublishDate = DateTime.ParseExact(dateString, "dd MMM yyyy HH:mm:ss zz00", CultureInfo.InvariantCulture).ToLocalTime();

                    var category = row.QuerySelector("td:nth-of-type(1) a").GetAttribute("href").Replace("torrents.php?category=", "");
                    release.Category = MapTrackerCatToNewznab(category);

                    release.UploadVolumeFactor = 1;

                    if (row.QuerySelector("img[alt=\"Free Torrent\"]") != null)
                    {
                        release.DownloadVolumeFactor = 0;
                        release.UploadVolumeFactor = 0;
                    }
                    else if (hasFreeleech)
                        release.DownloadVolumeFactor = 0;
                    else if (row.QuerySelector("img[alt=\"Silver Torrent\"]") != null)
                        release.DownloadVolumeFactor = 0.5;
                    else if (row.QuerySelector("img[alt=\"Bronze Torrent\"]") != null)
                        release.DownloadVolumeFactor = 0.75;
                    else if (row.QuerySelector("img[alt=\"Blue Torrent\"]") != null)
                        release.DownloadVolumeFactor = 0.25;
                    else
                        release.DownloadVolumeFactor = 1;

                    var imdblink = row.QuerySelector("a[href*=\"www.imdb.com/title/\"]")?.GetAttribute("href");
                    if (!string.IsNullOrWhiteSpace(imdblink))
                        release.Imdb = ParseUtil.GetLongFromString(imdblink);

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
