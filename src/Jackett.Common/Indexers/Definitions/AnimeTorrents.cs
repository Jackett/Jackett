using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Jackett.Common.Extensions;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class AnimeTorrents : IndexerBase
    {
        public override string Id => "animetorrents";
        public override string Name => "AnimeTorrents";
        public override string Description => "Definitive source for anime and manga";
        public override string SiteLink { get; protected set; } = "https://animetorrents.me/";
        public override string Language => "en-US";
        public override string Type => "private";

        public override bool SupportsPagination => true;

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private string LoginUrl => SiteLink + "login.php";
        private string SearchUrl => SiteLink + "ajax/torrents_data.php";
        private string SearchUrlReferer => SiteLink + "torrents.php?cat=0&searchin=filename&search=";

        private new ConfigurationDataAnimeTorrents configData => (ConfigurationDataAnimeTorrents)base.configData;

        public AnimeTorrents(IIndexerConfigurationService configService, WebClient c, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(configService: configService,
                   client: c,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataAnimeTorrents())
        {
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                },
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q
                }
            };

            caps.Categories.AddCategoryMapping(1, TorznabCatType.MoviesSD, "Anime Movie");
            caps.Categories.AddCategoryMapping(6, TorznabCatType.MoviesHD, "Anime Movie HD");
            caps.Categories.AddCategoryMapping(2, TorznabCatType.TVAnime, "Anime Series");
            caps.Categories.AddCategoryMapping(7, TorznabCatType.TVAnime, "Anime Series HD");
            caps.Categories.AddCategoryMapping(5, TorznabCatType.XXXDVD, "Hentai (censored)");
            caps.Categories.AddCategoryMapping(9, TorznabCatType.XXXDVD, "Hentai (censored) HD");
            caps.Categories.AddCategoryMapping(4, TorznabCatType.XXXDVD, "Hentai (un-censored)");
            caps.Categories.AddCategoryMapping(8, TorznabCatType.XXXDVD, "Hentai (un-censored) HD");
            caps.Categories.AddCategoryMapping(13, TorznabCatType.BooksForeign, "Light Novel");
            caps.Categories.AddCategoryMapping(3, TorznabCatType.BooksComics, "Manga");
            caps.Categories.AddCategoryMapping(10, TorznabCatType.BooksComics, "Manga 18+");
            caps.Categories.AddCategoryMapping(11, TorznabCatType.TVAnime, "OVA");
            caps.Categories.AddCategoryMapping(12, TorznabCatType.TVAnime, "OVA HD");
            caps.Categories.AddCategoryMapping(14, TorznabCatType.BooksComics, "Doujin Anime");
            caps.Categories.AddCategoryMapping(15, TorznabCatType.XXXDVD, "Doujin Anime 18+");
            caps.Categories.AddCategoryMapping(16, TorznabCatType.AudioForeign, "Doujin Music");
            caps.Categories.AddCategoryMapping(17, TorznabCatType.BooksComics, "Doujinshi");
            caps.Categories.AddCategoryMapping(18, TorznabCatType.BooksComics, "Doujinshi 18+");
            caps.Categories.AddCategoryMapping(19, TorznabCatType.Audio, "OST");

            return caps;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "form", "login" },
                { "rememberme[]", "1" }
            };

            var loginPage = await RequestWithCookiesAndRetryAsync(LoginUrl, "", RequestType.GET, LoginUrl);

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, loginPage.Cookies, true);
            await ConfigureIfOK(result.Cookies, result.ContentString != null && result.ContentString.Contains("logout.php"), () =>
            {
                var parser = new HtmlParser();
                using var dom = parser.ParseDocument(result.ContentString);
                var errorMessage = dom.QuerySelector(".ui-state-error").Text().Trim();
                throw new ExceptionWithConfigData(errorMessage, configData);
            });

            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            var searchString = query.GetQueryString().Trim();

            // replace non-word characters with % (wildcard)
            searchString = Regex.Replace(searchString, @"[\W]+", "%");

            var searchUrl = SearchUrl;

            var page = query.Limit > 0 && query.Offset > 0 ? (int)(query.Offset / query.Limit) + 1 : 1;

            var queryCollection = new NameValueCollection
            {
                { "total", "146" }, // Assuming the total number of pages
                { "cat", MapTorznabCapsToTrackers(query).FirstIfSingleOrDefault("0") },
                { "page", page.ToString() },
                { "searchin", "filename" },
                { "search", searchString }
            };

            if (configData.DownloadableOnly.Value)
            {
                queryCollection.Set("dlable", "1");
            }

            searchUrl += "?" + queryCollection.GetQueryString();

            var extraHeaders = new Dictionary<string, string>
            {
                { "X-Requested-With", "XMLHttpRequest" }
            };

            var response = await RequestWithCookiesAndRetryAsync(searchUrl, referer: SearchUrlReferer, headers: extraHeaders);

            var results = response.ContentString;

            try
            {
                var parser = new HtmlParser();
                using var dom = parser.ParseDocument(results);

                var rows = dom.QuerySelectorAll("table tr");
                foreach (var (row, index) in rows.Skip(1).Select((v, i) => (v, i)))
                {
                    var downloadVolumeFactor = row.QuerySelector("img[alt=\"Gold Torrent\"]") != null ? 0 : row.QuerySelector("img[alt=\"Silver Torrent\"]") != null ? 0.5 : 1;

                    // skip non-freeleech results when freeleech only is set
                    if (configData.FreeleechOnly.Value && downloadVolumeFactor != 0)
                    {
                        continue;
                    }

                    var qTitleLink = row.QuerySelector("td:nth-of-type(2) a:nth-of-type(1)");
                    var title = qTitleLink?.TextContent.Trim();

                    // If we search and get no results, we still get a table just with no info.
                    if (title.IsNullOrWhiteSpace())
                    {
                        break;
                    }

                    var infoUrl = qTitleLink?.GetAttribute("href");

                    // newbie users don't see DL links
                    // use details link as placeholder
                    // skipping the release prevents newbie users from adding the tracker (empty result)
                    var downloadUrl = row.QuerySelector("td:nth-of-type(3) a")?.GetAttribute("href") ?? infoUrl;

                    var connections = row.QuerySelector("td:nth-of-type(8)").TextContent.Trim().Split("/".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    var seeders = ParseUtil.CoerceInt(connections[0]);

                    var categoryLink = row.QuerySelector("td:nth-of-type(1) a")?.GetAttribute("href") ?? string.Empty;
                    var categoryId = ParseUtil.GetArgumentFromQueryString(categoryLink, "cat");

                    var publishedDate = DateTime.ParseExact(row.QuerySelector("td:nth-of-type(5)").TextContent, "dd MMM yy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

                    if (publishedDate.Date == DateTime.Today)
                    {
                        publishedDate = publishedDate.Date + DateTime.Now.TimeOfDay - TimeSpan.FromMinutes(index);
                    }

                    var release = new ReleaseInfo
                    {
                        Guid = new Uri(infoUrl),
                        Details = new Uri(infoUrl),
                        Link = new Uri(downloadUrl),
                        Title = title,
                        Category = MapTrackerCatToNewznab(categoryId),
                        PublishDate = publishedDate,
                        Size = ParseUtil.GetBytes(row.QuerySelector("td:nth-of-type(6)").TextContent.Trim()),
                        Seeders = seeders,
                        Peers = ParseUtil.CoerceInt(connections[1]) + seeders,
                        Grabs = ParseUtil.CoerceInt(connections[2]),
                        DownloadVolumeFactor = downloadVolumeFactor,
                        UploadVolumeFactor = 1,
                        Genres = row.QuerySelectorAll("td:nth-of-type(2) a.tortags").Select(t => t.TextContent.Trim()).ToList()
                    };

                    var uLFactorImg = row.QuerySelector("img[alt*=\"x Multiplier Torrent\"]");
                    if (uLFactorImg != null)
                    {
                        release.UploadVolumeFactor = ParseUtil.CoerceDouble(uLFactorImg.GetAttribute("alt").Split('x')[0]);
                    }

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
