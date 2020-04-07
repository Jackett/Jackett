using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Jackett.Common.Helpers;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers
{
    public class Cinecalidad : BaseWebIndexer
    {
        private const int MaxItemsPerPage = 15;
        private const int MaxSearchPageLimit = 6; // 15 items per page * 6 pages = 90
        private string _language;

        public override string[] AlternativeSiteLinks { get; protected set; } = {
            "https://www.cinecalidad.is/",
            "https://www.cinecalidad.to/",
            "https://www.cinecalidad.eu/"
        };

        public Cinecalidad(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base("Cinecalidad",
                   description: "Películas Full HD en Castellano y Latino Dual.",
                   link: "https://www.cinecalidad.is/",
                   caps: new TorznabCapabilities(),
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   configData: new ConfigurationData())
        {
            Encoding = Encoding.UTF8;
            Language = "es-es";
            Type = "public";

            var language = new SelectItem(new Dictionary<string, string>
                {
                    {"castellano", "Spanish Castellano"},
                    {"latino", "Spanish Latino"}
                })
                {
                    Name = "Select language",
                    Value = "castellano"
                };
            configData.AddDynamic("language", language);

            AddCategoryMapping(1, TorznabCatType.MoviesHD);
        }

        public override void LoadValuesFromJson(JToken jsonConfig, bool useProtectionService = false)
        {
            base.LoadValuesFromJson(jsonConfig, useProtectionService);
            var language = (SelectItem)configData.GetDynamic("language");
            _language = language?.Value ?? "castellano";
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var releases = await PerformQuery(new TorznabQuery());

            await ConfigureIfOK(string.Empty, releases.Any(), () =>
                                    throw new Exception("Could not find release from this URL."));

            return IndexerConfigurationStatus.Completed;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            var templateUrl = SiteLink;
            if (_language.Equals("castellano"))
                templateUrl += "espana/";
            templateUrl += "{0}"; // placeholder for page

            var maxPages = 2; // we scrape only 2 pages for recent torrents
            if (!string.IsNullOrWhiteSpace(query.GetQueryString()))
            {
                templateUrl += "?s=" + WebUtilityHelpers.UrlEncode(query.GetQueryString(), Encoding.UTF8);
                maxPages = MaxSearchPageLimit;
            }

            var lastPublishDate = DateTime.Now;
            for (var page = 1; page <= maxPages; page++)
            {
                var pageParam = page > 1 ? $"page/{page}/" : "";
                var searchUrl = string.Format(templateUrl, pageParam);
                var response = await RequestStringWithCookiesAndRetry(searchUrl);
                var pageReleases = ParseReleases(response, query);

                // publish date is not available in the torrent list, but we add a relative date so we can sort
                foreach(var release in pageReleases)
                {
                    release.PublishDate = lastPublishDate;
                    lastPublishDate = lastPublishDate.AddMinutes(-1);
                }
                releases.AddRange(pageReleases);

                if (pageReleases.Count < MaxItemsPerPage)
                    break; // this is the last page
            }

            return releases;
        }

        public override async Task<byte[]> Download(Uri link)
        {
            var results = await RequestStringWithCookies(link.ToString());

            try
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(results.Content);
                var preotectedLink = dom.QuerySelector("a[service=BitTorrent]").GetAttribute("href");
                preotectedLink = SiteLink + preotectedLink.TrimStart('/');

                results = await RequestStringWithCookies(preotectedLink);
                dom = parser.ParseDocument(results.Content);
                var magnetUrl = dom.QuerySelector("a[href^=magnet]").GetAttribute("href");
                return await base.Download(new Uri(magnetUrl));
            }
            catch (Exception ex)
            {
                OnParseError(results.Content, ex);
            }

            return null;
        }

        private List<ReleaseInfo> ParseReleases(WebClientStringResult response, TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            try
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(response.Content);

                var rows = dom.QuerySelectorAll("div.home_post_cont");
                foreach (var row in rows)
                {
                    var qImg = row.QuerySelector("img");
                    var title = qImg.GetAttribute("title");
                    if (!CheckTitleMatchWords(query.GetQueryString(), title))
                        continue; // skip if it doesn't contain all words
                    title += _language.Equals("castellano") ? " SPANiSH" : " LATiN-SPANiSH";
                    title += " DUAL 1080p BDRip x264";

                    var banner = new Uri(qImg.GetAttribute("src"));
                    var link = new Uri(row.QuerySelector("a").GetAttribute("href"));

                    var release = new ReleaseInfo
                    {
                        Title = title,
                        Link = link,
                        Comments = link,
                        Guid = link,
                        Category = new List<int> {TorznabCatType.MoviesHD.ID},
                        BannerUrl = banner,
                        Size = 2147483648, // 2 GB
                        Files = 1,
                        Seeders = 1,
                        Peers = 2,
                        MinimumRatio = 1,
                        MinimumSeedTime = 172800, // 48 hours
                        DownloadVolumeFactor = 0,
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

        // TODO: merge this method with query.MatchQueryStringAND
        private static bool CheckTitleMatchWords(string queryStr, string title)
        {
            // this code split the words, remove words with 2 letters or less, remove accents and lowercase
            var queryMatches = Regex.Matches(queryStr, @"\b[\w']*\b");
            var queryWords = from m in queryMatches.Cast<Match>()
                             where !string.IsNullOrEmpty(m.Value) && m.Value.Length > 2
                             select Encoding.UTF8.GetString(Encoding.GetEncoding("ISO-8859-8").GetBytes(m.Value.ToLower()));

            var titleMatches = Regex.Matches(title, @"\b[\w']*\b");
            var titleWords = from m in titleMatches.Cast<Match>()
                             where !string.IsNullOrEmpty(m.Value) && m.Value.Length > 2
                             select Encoding.UTF8.GetString(Encoding.GetEncoding("ISO-8859-8").GetBytes(m.Value.ToLower()));
            titleWords = titleWords.ToArray();

            return queryWords.All(word => titleWords.Contains(word));
        }
    }

}
