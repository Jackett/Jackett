using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class Uniotaku : IndexerBase
    {
        public override string Id => "uniotaku";
        public override string Name => "UniOtaku";
        public override string Description => "UniOtaku is a BRAZILIAN Semi-Private Torrent Tracker for ANIME";
        public override string SiteLink { get; protected set; } = "https://tracker.uniotaku.com/";
        public override string Language => "pt-BR";
        public override string Type => "semi-private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        public Uniotaku(IIndexerConfigurationService configService, Utils.Clients.WebClient wc, Logger l, IProtectionService ps, ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataUniotaku())
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
                },
                MusicSearchParams = new List<MusicSearchParam>
                {
                    MusicSearchParam.Q
                },
                BookSearchParams = new List<BookSearchParam>
                {
                    BookSearchParam.Q
                }
            };

            caps.Categories.AddCategoryMapping(28, TorznabCatType.TVAnime, "Anime");
            caps.Categories.AddCategoryMapping(47, TorznabCatType.MoviesOther, "Filme");
            caps.Categories.AddCategoryMapping(48, TorznabCatType.TVAnime, "OVA");
            caps.Categories.AddCategoryMapping(49, TorznabCatType.BooksComics, "Mangá");
            caps.Categories.AddCategoryMapping(50, TorznabCatType.TVOther, "Dorama");
            caps.Categories.AddCategoryMapping(51, TorznabCatType.Audio, "OST");
            caps.Categories.AddCategoryMapping(52, TorznabCatType.TVAnime, "Anime Completo");
            caps.Categories.AddCategoryMapping(53, TorznabCatType.BooksComics, "Mangá Completo");
            caps.Categories.AddCategoryMapping(54, TorznabCatType.TVOther, "Dorama Completo");
            caps.Categories.AddCategoryMapping(55, TorznabCatType.XXX, "Hentai");
            caps.Categories.AddCategoryMapping(56, TorznabCatType.XXXOther, "H Doujinshi");
            caps.Categories.AddCategoryMapping(57, TorznabCatType.TVOther, "Tokusatsu");
            caps.Categories.AddCategoryMapping(58, TorznabCatType.TVOther, "Live Action");

            return caps;
        }

        private new ConfigurationDataUniotaku configData => (ConfigurationDataUniotaku)base.configData;

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var loginUrl = SiteLink + "account-login.php";

            var postData = new Dictionary<string, string>
            {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "manter", "1" }
            };

            var response = await RequestLoginAndFollowRedirect(loginUrl, postData, CookieHeader, true, null, SiteLink);

            await ConfigureIfOK(response.Cookies, response.Cookies != null && response.Cookies.Contains("uid=") && response.Cookies.Contains("pass="), () =>
            {
                var parser = new HtmlParser();
                using var dom = parser.ParseDocument(response.ContentString);
                var errorMessage = dom.QuerySelector(".login-content span.text-red")?.TextContent.Trim();

                throw new ExceptionWithConfigData(errorMessage ?? "Unknown error message, please report.", configData);
            });

            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var searchString = query.GetQueryString();

            if (!string.IsNullOrWhiteSpace(searchString))
                searchString = "%" + Regex.Replace(searchString, @"[ -._]+", "%").Trim() + "%";

            var categoryMapping = MapTorznabCapsToTrackers(query);

            var parameters = new NameValueCollection
            {
                { "categoria", categoryMapping.FirstIfSingleOrDefault("0") },
                { "grupo", "0" },
                { "status", configData.Freeleech.Value ? "1" : "0" },
                { "ordenar", configData.SortBy.Value },
                { "start", "0" },
                { "length", "100" },
                { "search[value]", searchString ?? string.Empty },
                { "search[regex]", "false" },
            };

            var searchUrl = $"{SiteLink}torrents_.php?{parameters.GetQueryString()}";
            var response = await RequestWithCookiesAsync(searchUrl);

            var releases = new List<ReleaseInfo>();
            var parser = new HtmlParser();

            try
            {
                var jsonContent = JObject.Parse(response.ContentString);

                var publishDate = DateTime.Now;
                foreach (var item in jsonContent.Value<JArray>("data"))
                {
                    using var detailsDom = parser.ParseDocument(item.SelectToken("[0]").Value<string>());
                    using var categoryDom = parser.ParseDocument(item.SelectToken("[1]").Value<string>());
                    using var groupDom = parser.ParseDocument(item.SelectToken("[7]").Value<string>());

                    var qTitleLink = detailsDom.QuerySelector("a[href^=\"torrents-details.php?id=\"]");
                    var title = qTitleLink?.TextContent.Trim();
                    var details = new Uri(SiteLink + qTitleLink?.GetAttribute("href"));

                    var category = categoryDom.QuerySelector("img[alt]")?.GetAttribute("alt")?.Trim() ?? "Anime";

                    var releaseGroup = groupDom.QuerySelector("a[href*=\"teams-view.php?id=\"]")?.TextContent.Trim();
                    if (!string.IsNullOrWhiteSpace(releaseGroup))
                        title += $" [{releaseGroup}]";

                    var seeders = item.SelectToken("[3]")?.Value<int>();
                    var leechers = item.SelectToken("[4]")?.Value<int>();

                    publishDate = publishDate.AddMinutes(-1);

                    var release = new ReleaseInfo
                    {
                        Guid = details,
                        Details = details,
                        Link = details,
                        Title = title,
                        Category = MapTrackerCatDescToNewznab(category),
                        Size = ParseUtil.GetBytes(item.SelectToken("[6]")?.Value<string>()),
                        Grabs = item.SelectToken("[5]")?.Value<int>(),
                        Seeders = seeders,
                        Peers = seeders + leechers,
                        PublishDate = publishDate,
                        DownloadVolumeFactor =
                            detailsDom.QuerySelector("img[src*=\"images/free.gif\"]") != null ? 0 :
                            detailsDom.QuerySelector("img[src*=\"images/silverdownload.gif\"]") != null ? 0.5 : 1,
                        UploadVolumeFactor = 1,
                        MinimumRatio = 0.7
                    };

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(response.ContentString, ex);
            }

            return releases;
        }

        public override async Task<byte[]> Download(Uri link)
        {
            var response = await RequestWithCookiesAsync(link.ToString());

            var parser = new HtmlParser();
            using var dom = parser.ParseDocument(response.ContentString);
            var downloadLink = dom.QuerySelector("a[href^=\"download.php?id=\"]")?.GetAttribute("href")?.Trim();

            if (downloadLink == null)
                throw new Exception($"Failed to fetch download link from {link}");

            return await base.Download(new Uri(SiteLink + downloadLink));
        }
    }
}
