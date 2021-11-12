using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class SubsPlease : BaseWebIndexer
    {
        public override string[] AlternativeSiteLinks { get; protected set; } = {
            "https://subsplease.org/",
            "https://subsplease.nocensor.biz/"
        };

        public override string[] LegacySiteLinks { get; protected set; } = {
            "https://subsplease.nocensor.space/",
            "https://subsplease.nocensor.work/"
        };

        private string ApiEndpoint => SiteLink + "/api/?";

        public SubsPlease(IIndexerConfigurationService configService, Utils.Clients.WebClient wc, Logger l, IProtectionService ps, ICacheService cs)
            : base(id: "subsplease",
                   name: "SubsPlease",
                   description: "SubsPlease - A better HorribleSubs/Erai replacement",
                   link: "https://subsplease.org/",
                   caps: new TorznabCapabilities
                   {
                       TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                       }
                   },
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationData())
        {
            Encoding = Encoding.UTF8;
            Language = "en-US";
            Type = "public";

            // Configure the category mappings
            AddCategoryMapping(1, TorznabCatType.TVAnime, "Anime");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var releases = await PerformQuery(new TorznabQuery());

            await ConfigureIfOK(string.Empty, releases.Any(), () =>
                throw new Exception("Could not find releases from this URL"));

            return IndexerConfigurationStatus.Completed;
        }

        // If the search string is empty use the latest releases
        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
            => query.IsTest || string.IsNullOrWhiteSpace(query.SearchTerm)
            ? await FetchNewReleases()
            : await PerformSearch(query);

        private async Task<IEnumerable<ReleaseInfo>> PerformSearch(TorznabQuery query)
        {
            // If the search terms contain [SubsPlease] or SubsPlease, remove them from the query sent to the API
            string searchTerm = Regex.Replace(query.SearchTerm, "\\[?SubsPlease\\]?\\s*", string.Empty, RegexOptions.IgnoreCase).Trim();

            // If the search terms contain a resolution, remove it from the query sent to the API
            Match resMatch = Regex.Match(searchTerm, "\\d{3,4}[p|P]");
            if (resMatch.Success)
                searchTerm = searchTerm.Replace(resMatch.Value, string.Empty);

            var queryParameters = new NameValueCollection
            {
                { "f", "search" },
                { "tz", "America/New_York" },
                { "s", searchTerm }
            };
            var response = await RequestWithCookiesAndRetryAsync(ApiEndpoint + queryParameters.GetQueryString());
            if (response.Status != HttpStatusCode.OK)
                throw new WebException($"SubsPlease search returned unexpected result. Expected 200 OK but got {response.Status}.", WebExceptionStatus.ProtocolError);

            var results = ParseApiResults(response.ContentString);
            var filteredResults = results.Where(release => query.MatchQueryStringAND(release.Title));

            // If we detected a resolution in the search terms earlier, filter by it
            if (resMatch.Success)
                filteredResults = filteredResults.Where(release => release.Title.IndexOf(resMatch.Value, StringComparison.OrdinalIgnoreCase) >= 0);

            return filteredResults;
        }

        private async Task<IEnumerable<ReleaseInfo>> FetchNewReleases()
        {
            var queryParameters = new NameValueCollection
            {
                { "f", "latest" },
                { "tz", "America/New_York" }
            };
            var response = await RequestWithCookiesAndRetryAsync(ApiEndpoint + queryParameters.GetQueryString());
            if (response.Status != HttpStatusCode.OK)
                throw new WebException($"SubsPlease search returned unexpected result. Expected 200 OK but got {response.Status}.", WebExceptionStatus.ProtocolError);

            return ParseApiResults(response.ContentString);
        }

        private List<ReleaseInfo> ParseApiResults(string json)
        {
            var releaseInfo = new List<ReleaseInfo>();

            // When there are no results, the API returns an empty array or empty response instead of an object
            if (string.IsNullOrWhiteSpace(json) || json == "[]")
                return releaseInfo;

            var releases = JsonConvert.DeserializeObject<Dictionary<string, Release>>(json);
            foreach (var keyValue in releases)
            {
                Release r = keyValue.Value;
                var baseRelease = new ReleaseInfo
                {
                    Details = new Uri(SiteLink + $"shows/{r.Page}/"),
                    PublishDate = r.Release_Date.DateTime,
                    Files = 1,
                    Category = new List<int> { TorznabCatType.TVAnime.ID },
                    Seeders = 1,
                    Peers = 2,
                    MinimumRatio = 1,
                    MinimumSeedTime = 172800, // 48 hours
                    DownloadVolumeFactor = 0,
                    UploadVolumeFactor = 1
                };
                foreach (var d in r.Downloads)
                {
                    var release = (ReleaseInfo)baseRelease.Clone();
                    // Ex: [SubsPlease] Shingeki no Kyojin (The Final Season) - 64 (1080p)
                    release.Title += $"[SubsPlease] {r.Show} - {r.Episode} ({d.Res}p)";
                    release.MagnetUri = new Uri(d.Magnet);
                    release.Link = null;
                    release.Guid = new Uri(d.Magnet);

                    // The API doesn't tell us file size, so give an estimate based on resolution
                    if (string.Equals(d.Res, "1080"))
                        release.Size = 1395864371; // 1.3GB
                    else if (string.Equals(d.Res, "720"))
                        release.Size = 734003200; // 700MB
                    else if (string.Equals(d.Res, "480"))
                        release.Size = 367001600; // 350MB
                    else
                        release.Size = 1073741824; // 1GB

                    releaseInfo.Add(release);
                }
            }

            return releaseInfo;
        }

        public class Release
        {
            public string Time { get; set; }
            public DateTimeOffset Release_Date { get; set; }
            public string Show { get; set; }
            public string Episode { get; set; }
            public DownloadInfo[] Downloads { get; set; }
            public string Xdcc { get; set; }
            public string ImageUrl { get; set; }
            public string Page { get; set; }
        }

        public class DownloadInfo
        {
            public string Res { get; set; }
            public string Magnet { get; set; }
        }
    }
}
