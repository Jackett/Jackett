using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Newtonsoft.Json.Linq;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class HDSpace : BaseWebIndexer
    {
        private string LoginUrl => SiteLink + "index.php?page=login";
        private string SearchUrl => SiteLink + "index.php?page=torrents";

        private new ConfigurationDataBasicLogin configData => (ConfigurationDataBasicLogin)base.configData;

        public HDSpace(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "hdspace",
                   name: "HD-Space",
                   description: "Sharing The Universe",
                   link: "https://hd-space.org/",
                   caps: new TorznabCapabilities
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
                   },
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataBasicLogin())
        {
            Encoding = Encoding.UTF8;
            Language = "en-US";
            Type = "private";

            configData.AddDynamic("flaresolverr", new DisplayInfoConfigurationItem("FlareSolverr", "This site may use Cloudflare DDoS Protection, therefore Jackett requires <a href=\"https://github.com/Jackett/Jackett#configuring-flaresolverr\" target=\"_blank\">FlareSolverr</a> to access it."));

            AddCategoryMapping(15, TorznabCatType.MoviesBluRay, "Movie / Blu-ray");
            AddCategoryMapping(40, TorznabCatType.MoviesHD, "Movie / Remux");
            AddCategoryMapping(18, TorznabCatType.MoviesHD, "Movie / 720p");
            AddCategoryMapping(19, TorznabCatType.MoviesHD, "Movie / 1080p");
            AddCategoryMapping(46, TorznabCatType.MoviesUHD, "Movie / 2160p");
            AddCategoryMapping(21, TorznabCatType.TVHD, "TV Show / 720p HDTV");
            AddCategoryMapping(22, TorznabCatType.TVHD, "TV Show / 1080p HDTV");
            AddCategoryMapping(45, TorznabCatType.TVUHD, "TV Show / 2160p HDTV");
            AddCategoryMapping(24, TorznabCatType.TVDocumentary, "Documentary / 720p");
            AddCategoryMapping(25, TorznabCatType.TVDocumentary, "Documentary / 1080p");
            AddCategoryMapping(47, TorznabCatType.TVDocumentary, "Documentary / 2160p");
            AddCategoryMapping(27, TorznabCatType.TVAnime, "Animation / 720p");
            AddCategoryMapping(28, TorznabCatType.TVAnime, "Animation / 1080p");
            AddCategoryMapping(48, TorznabCatType.TVAnime, "Animation / 2160p");
            AddCategoryMapping(30, TorznabCatType.AudioLossless, "Music / HQ Audio");
            AddCategoryMapping(31, TorznabCatType.AudioVideo, "Music / Videos");
            AddCategoryMapping(33, TorznabCatType.XXX, "XXX / 720p");
            AddCategoryMapping(34, TorznabCatType.XXX, "XXX / 1080p");
            AddCategoryMapping(49, TorznabCatType.XXX, "XXX / 2160p");
            AddCategoryMapping(36, TorznabCatType.MoviesOther, "Trailers");
            AddCategoryMapping(37, TorznabCatType.PC, "Software");
            AddCategoryMapping(38, TorznabCatType.Other, "Others");
            AddCategoryMapping(41, TorznabCatType.MoviesUHD, "Movie / 4K UHD");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var loginPage = await RequestWithCookiesAsync(LoginUrl, string.Empty);
            var pairs = new Dictionary<string, string>
            {
                {"uid", configData.Username.Value},
                {"pwd", configData.Password.Value}
            };

            // Send Post
            var response = await RequestLoginAndFollowRedirect(LoginUrl, pairs, loginPage.Cookies, true, referer: LoginUrl);

            await ConfigureIfOK(response.Cookies, response.ContentString?.Contains("logout.php") == true, () =>
            {
                var errorStr = "You have {0} remaining login attempts";
                var remainingAttemptSpan = new Regex(string.Format(errorStr, "(.*?)"))
                                           .Match(loginPage.ContentString).Groups[1].ToString();
                var attempts = Regex.Replace(remainingAttemptSpan, "<.*?>", string.Empty);
                var errorMessage = string.Format(errorStr, attempts);
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var queryCollection = new NameValueCollection
            {
                {"active", "0"},
                {"category", string.Join(";", MapTorznabCapsToTrackers(query))}
            };

            if (query.IsImdbQuery)
            {
                queryCollection.Set("options", "2");
                queryCollection.Set("search", query.ImdbIDShort);
            }
            else
            {
                queryCollection.Set("options", "0");
                queryCollection.Set("search", query.GetQueryString().Replace(".", " "));
            }

            var response = await RequestWithCookiesAndRetryAsync($"{SearchUrl}&{queryCollection.GetQueryString()}");

            try
            {
                var resultParser = new HtmlParser();
                var searchResultDocument = resultParser.ParseDocument(response.ContentString);
                var rows = searchResultDocument.QuerySelectorAll("table.lista > tbody > tr");

                foreach (var row in rows)
                {
                    // this tracker has horrible markup, find the result rows by looking for the style tag before each one
                    var prev = row.PreviousElementSibling;
                    if (prev == null || !string.Equals(prev.NodeName, "style", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var release = new ReleaseInfo
                    {
                        MinimumRatio = 1,
                        MinimumSeedTime = 86400 // 24 hours
                    };

                    var qLink = row.Children[1].FirstElementChild;
                    release.Title = qLink.TextContent.Trim();
                    release.Details = new Uri(SiteLink + qLink.GetAttribute("href"));
                    release.Guid = release.Details;
                    release.Link = new Uri(SiteLink + row.Children[3].FirstElementChild.GetAttribute("href"));

                    var torrentTitle = ParseUtil.GetArgumentFromQueryString(release.Link.ToString(), "f")?.Replace(".torrent", "").Trim();
                    if (!string.IsNullOrWhiteSpace(torrentTitle))
                        release.Title = WebUtility.HtmlDecode(torrentTitle);

                    var qGenres = row.QuerySelector("span[style=\"color: #000000 \"]");
                    var description = "";
                    if (qGenres != null)
                        description = qGenres.TextContent.Split('\xA0').Last().Replace(" ", "");

                    var imdbLink = row.Children[1].QuerySelector("a[href*=imdb]");
                    if (imdbLink != null)
                        release.Imdb = ParseUtil.GetImdbID(imdbLink.GetAttribute("href").Split('/').Last());

                    var dateStr = row.Children[4].TextContent.Trim();
                    //"July 11, 2015, 13:34:09", "Today|Yesterday at 20:04:23"
                    release.PublishDate = DateTimeUtil.FromUnknown(dateStr);
                    var sizeStr = row.Children[5].TextContent;
                    release.Size = ReleaseInfo.GetBytes(sizeStr);
                    release.Seeders = ParseUtil.CoerceInt(row.Children[7].TextContent);
                    release.Peers = ParseUtil.CoerceInt(row.Children[8].TextContent) + release.Seeders;
                    var grabs = row.QuerySelector("td:nth-child(10)").TextContent;
                    grabs = grabs.Replace("---", "0");
                    release.Grabs = ParseUtil.CoerceInt(grabs);
                    if (row.QuerySelector("img[title=\"FreeLeech\"]") != null)
                        release.DownloadVolumeFactor = 0;
                    else if (row.QuerySelector("img[src=\"images/sf.png\"]") != null) // side freeleech
                        release.DownloadVolumeFactor = 0;
                    else if (row.QuerySelector("img[title=\"Half FreeLeech\"]") != null)
                        release.DownloadVolumeFactor = 0.5;
                    else
                        release.DownloadVolumeFactor = 1;
                    release.UploadVolumeFactor = 1;

                    var categoryLink = row.QuerySelector("a[href^=\"index.php?page=torrents&category=\"]").GetAttribute("href");
                    var cat = ParseUtil.GetArgumentFromQueryString(categoryLink, "category");
                    release.Category = MapTrackerCatToNewznab(cat);

                    release.Description = description;
                    if (release.Genres == null)
                        release.Genres = new List<string>();
                    release.Genres = release.Genres.Union(description.Split(',')).ToList();

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(response.ContentString, ex);
            }

            return releases;
        }
    }
}
