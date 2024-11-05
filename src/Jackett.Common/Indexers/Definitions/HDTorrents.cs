using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class HDTorrents : IndexerBase
    {
        public override string Id => "hdtorrents";
        public override string Name => "HD-Torrents";
        public override string Description => "HD-Torrents is a private torrent website with HD torrents and strict rules on their content.";
        public override string SiteLink { get; protected set; } = "https://hdts.ru/"; // Domain https://hdts.ru/ seems more reliable
        public override string[] AlternativeSiteLinks => new[]
        {
            "https://hdts.ru/",
            "https://hd-torrents.org/",
            "https://hd-torrents.net/",
            "https://hd-torrents.me/"
        };
        public override string Language => "en-US";
        public override string Type => "private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private string SearchUrl => SiteLink + "torrents.php?";
        private string LoginUrl => SiteLink + "login.php";
        private readonly Regex _posterRegex = new Regex(@"src=\\'./([^']+)\\'", RegexOptions.IgnoreCase);
        private readonly HashSet<string> _freeleechRanks = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "VIP",
            "Uploader",
            "HD Internal",
            "Moderator",
            "Administrator",
            "Owner"
        };

        private new ConfigurationDataBasicLogin configData => (ConfigurationDataBasicLogin)base.configData;

        public HDTorrents(IIndexerConfigurationService configService, WebClient w, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(configService: configService,
                   client: w,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataBasicLogin("For best results, change the <b>Torrents per page:</b> setting to <b>100</b> on your account profile."))
        {
            configData.AddDynamic("freeleech", new BoolConfigurationItem("Search freeleech only") { Value = false });
            configData.AddDynamic("flaresolverr", new DisplayInfoConfigurationItem("FlareSolverr", "This site may use Cloudflare DDoS Protection, therefore Jackett requires <a href=\"https://github.com/Jackett/Jackett#configuring-flaresolverr\" target=\"_blank\">FlareSolverr</a> to access it."));
            configData.AddDynamic("accountinactivity", new DisplayInfoConfigurationItem("Account Inactivity", "If you do not log in for 50 days, your account will be disabled for inactivity. If you are VIP you won't be disabled until the VIP period is over."));
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId
                },
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q, MovieSearchParam.ImdbId
                },
                MusicSearchParams = new List<MusicSearchParam>
                {
                    MusicSearchParam.Q
                }
            };

            // Movie
            caps.Categories.AddCategoryMapping("70", TorznabCatType.MoviesBluRay, "Movie/UHD/Blu-Ray");
            caps.Categories.AddCategoryMapping("1", TorznabCatType.MoviesBluRay, "Movie/Blu-Ray");
            caps.Categories.AddCategoryMapping("71", TorznabCatType.MoviesUHD, "Movie/UHD/Remux");
            caps.Categories.AddCategoryMapping("2", TorznabCatType.MoviesHD, "Movie/Remux");
            caps.Categories.AddCategoryMapping("5", TorznabCatType.MoviesHD, "Movie/1080p/i");
            caps.Categories.AddCategoryMapping("3", TorznabCatType.MoviesHD, "Movie/720p");
            caps.Categories.AddCategoryMapping("64", TorznabCatType.MoviesUHD, "Movie/2160p");
            caps.Categories.AddCategoryMapping("63", TorznabCatType.Audio, "Movie/Audio Track");

            // TV Show
            caps.Categories.AddCategoryMapping("72", TorznabCatType.TVUHD, "TV Show/UHD/Blu-ray");
            caps.Categories.AddCategoryMapping("59", TorznabCatType.TVHD, "TV Show/Blu-ray");
            caps.Categories.AddCategoryMapping("73", TorznabCatType.TVUHD, "TV Show/UHD/Remux");
            caps.Categories.AddCategoryMapping("60", TorznabCatType.TVHD, "TV Show/Remux");
            caps.Categories.AddCategoryMapping("30", TorznabCatType.TVHD, "TV Show/1080p/i");
            caps.Categories.AddCategoryMapping("38", TorznabCatType.TVHD, "TV Show/720p");
            caps.Categories.AddCategoryMapping("65", TorznabCatType.TVUHD, "TV Show/2160p");

            // Music
            caps.Categories.AddCategoryMapping("44", TorznabCatType.Audio, "Music/Album");
            caps.Categories.AddCategoryMapping("61", TorznabCatType.AudioVideo, "Music/Blu-Ray");
            caps.Categories.AddCategoryMapping("62", TorznabCatType.AudioVideo, "Music/Remux");
            caps.Categories.AddCategoryMapping("57", TorznabCatType.AudioVideo, "Music/1080p/i");
            caps.Categories.AddCategoryMapping("45", TorznabCatType.AudioVideo, "Music/720p");
            caps.Categories.AddCategoryMapping("66", TorznabCatType.AudioVideo, "Music/2160p");

            // XXX
            caps.Categories.AddCategoryMapping("58", TorznabCatType.XXX, "XXX/Blu-ray");
            caps.Categories.AddCategoryMapping("78", TorznabCatType.XXX, "XXX/Remux");
            caps.Categories.AddCategoryMapping("74", TorznabCatType.XXX, "XXX/UHD/Blu-ray");
            caps.Categories.AddCategoryMapping("48", TorznabCatType.XXX, "XXX/1080p/i");
            caps.Categories.AddCategoryMapping("47", TorznabCatType.XXX, "XXX/720p");
            caps.Categories.AddCategoryMapping("67", TorznabCatType.XXX, "XXX/2160p");

            return caps;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var loginPage = await RequestWithCookiesAsync(LoginUrl, string.Empty);

            var pairs = new Dictionary<string, string> {
                { "uid", configData.Username.Value },
                { "pwd", configData.Password.Value }
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, loginPage.Cookies, true, null, LoginUrl);

            await ConfigureIfOK(
                result.Cookies, result.ContentString?.Contains("If your browser doesn't have javascript enabled") == true, () =>
                {
                    var parser = new HtmlParser();
                    using var dom = parser.ParseDocument(result.ContentString);

                    var errorMessage = dom.QuerySelector("div > font[color=\"#FF0000\"]")?.TextContent.Trim();

                    throw new ExceptionWithConfigData(errorMessage ?? "Couldn't login", configData);
                });

            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            var searchUrl = SearchUrl + string.Join(string.Empty, MapTorznabCapsToTrackers(query).Select(cat => $"category[]={cat}&"));

            var queryCollection = new NameValueCollection
            {
                { "search", query.ImdbID ?? query.GetQueryString() },
                { "active", ((BoolConfigurationItem)configData.GetDynamic("freeleech")).Value ? "5" : "0" },
                { "options", "0" }
            };

            // manually url encode parenthesis to prevent "hacking" detection, remove . as not used in titles
            searchUrl += queryCollection.GetQueryString().Replace("(", "%28").Replace(")", "%29").Replace(".", " ");

            var results = await RequestWithCookiesAndRetryAsync(searchUrl);

            // Occasionally the cookies become invalid, login again if that happens
            if (results.ContentString.Contains("Error:You're not authorized"))
            {
                await ApplyConfiguration(null);
                results = await RequestWithCookiesAndRetryAsync(searchUrl);
            }

            try
            {
                var parser = new HtmlParser();
                using var dom = parser.ParseDocument(results.ContentString);

                var userInfo = dom.QuerySelector("table.navus tr");
                var userRank = userInfo.Children[1].TextContent.Replace("Rank:", string.Empty).Trim();
                var hasFreeleech = _freeleechRanks.Contains(userRank);

                var rows = dom.QuerySelectorAll("table.mainblockcontenttt tr:has(td.mainblockcontent)");
                foreach (var row in rows.Skip(1))
                {
                    if (row.Children.Length == 2)
                    {
                        // fix bug with search: cohen
                        continue;
                    }

                    var mainLink = row.Children[2].QuerySelector("a");
                    var title = mainLink.TextContent;
                    var details = new Uri(SiteLink + mainLink.GetAttribute("href"));

                    var posterMatch = _posterRegex.Match(mainLink.GetAttribute("onmouseover"));
                    var poster = posterMatch.Success ? new Uri(SiteLink + posterMatch.Groups[1].Value.Replace("\\", "/")) : null;

                    var link = new Uri(SiteLink + row.Children[4].FirstElementChild.GetAttribute("href"));
                    var description = row.Children[2].QuerySelector("span")?.TextContent.Trim();
                    var size = ParseUtil.GetBytes(row.Children[7].TextContent);

                    var dateAdded = string.Join(" ", row.Children[6].FirstElementChild.Attributes.Select(a => a.Name).Take(4));
                    var publishDate = DateTime.ParseExact(dateAdded, "dd MMM yyyy HH:mm:ss", CultureInfo.InvariantCulture);

                    var categoryLink = row.FirstElementChild.FirstElementChild.GetAttribute("href");
                    var cat = ParseUtil.GetArgumentFromQueryString(categoryLink, "category");

                    // Sometimes the uploader column is missing, so seeders, leechers, and grabs may be at a different index.
                    // There's room for improvement, but this works for now.
                    var endIndex = row.Children.Length;

                    //Maybe use row.Children.Index(Node) after searching for an element instead?
                    if (row.Children[endIndex - 1].TextContent == "Edit")
                        endIndex -= 1;
                    // moderators get additional delete, recommend and like links
                    else if (row.Children[endIndex - 4].TextContent == "Edit")
                        endIndex -= 4;

                    int? seeders = null;
                    int? peers = null;
                    if (ParseUtil.TryCoerceInt(row.Children[endIndex - 3].TextContent, out var rSeeders))
                    {
                        seeders = rSeeders;
                        if (ParseUtil.TryCoerceInt(row.Children[endIndex - 2].TextContent, out var rLeechers))
                            peers = rLeechers + rSeeders;
                    }

                    var grabs = ParseUtil.TryCoerceLong(row.Children[endIndex - 1].TextContent, out var rGrabs)
                        ? (long?)rGrabs
                        : null;

                    var dlVolumeFactor = 1.0;
                    var upVolumeFactor = 1.0;
                    if (row.QuerySelector("img[src$=\"no_ratio.png\"]") != null)
                    {
                        dlVolumeFactor = 0;
                        upVolumeFactor = 0;
                    }
                    else if (hasFreeleech || row.QuerySelector("img[src$=\"free.png\"]") != null)
                        dlVolumeFactor = 0;
                    else if (row.QuerySelector("img[src$=\"50.png\"]") != null)
                        dlVolumeFactor = 0.5;
                    else if (row.QuerySelector("img[src$=\"25.png\"]") != null)
                        dlVolumeFactor = 0.75;
                    else if (row.QuerySelector("img[src$=\"75.png\"]") != null)
                        dlVolumeFactor = 0.25;

                    var imdb = ParseUtil.GetImdbId(row.QuerySelector("a[href*=\"www.imdb.com/title/\"]")?.GetAttribute("href")?.TrimEnd('/')?.Split('/')?.LastOrDefault());

                    var release = new ReleaseInfo
                    {
                        Title = title,
                        Details = details,
                        Guid = details,
                        Link = link,
                        PublishDate = publishDate,
                        Category = MapTrackerCatToNewznab(cat),
                        Description = description,
                        Poster = poster,
                        Imdb = imdb,
                        Size = size,
                        Grabs = grabs,
                        Seeders = seeders,
                        Peers = peers,
                        DownloadVolumeFactor = dlVolumeFactor,
                        UploadVolumeFactor = upVolumeFactor,
                        MinimumRatio = 1,
                        MinimumSeedTime = 172800 // 48 hours
                    };

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(results.ContentString, ex);
            }

            return releases;
        }
    }
}
