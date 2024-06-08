using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jackett.Common.Extensions;
using Jackett.Common.Helpers;
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
    public class SubsPlease : IndexerBase
    {
        public override string Id => "subsplease";
        public override string Name => "SubsPlease";
        public override string Description => "SubsPlease - A better HorribleSubs/Erai replacement";
        public override string SiteLink { get; protected set; } = "https://subsplease.org/";
        public override string[] LegacySiteLinks => new[]
        {
            "https://subsplease.mrunblock.bond/",
            "https://subsplease.nocensor.cloud/"
        };
        public override string Language => "en-US";
        public override string Type => "public";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private string ApiEndpoint => SiteLink + "api/?";

        private static readonly Regex _RegexSize = new Regex(@"\&xl=(?<size>\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public SubsPlease(IIndexerConfigurationService configService, Utils.Clients.WebClient wc, Logger l, IProtectionService ps, ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationData())
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

            caps.Categories.AddCategoryMapping(1, TorznabCatType.TVAnime);
            caps.Categories.AddCategoryMapping(2, TorznabCatType.MoviesOther);

            return caps;
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
            var searchTerm = Regex.Replace(query.SearchTerm, "\\[?SubsPlease\\]?\\s*", string.Empty, RegexOptions.IgnoreCase).Trim();

            // If the search terms contain a resolution, remove it from the query sent to the API
            var resMatch = Regex.Match(searchTerm, "\\d{3,4}[p|P]");
            if (resMatch.Success)
            {
                searchTerm = searchTerm.Replace(resMatch.Value, string.Empty);
            }

            // Only include season > 1 in searchTerm, format as S2 rather than S02
            if (query.Season != 0)
            {
                searchTerm = query.Season == 1 ? searchTerm : searchTerm + $" S{query.Season}";
                query.Season = 0;
            }

            var queryParameters = new NameValueCollection
            {
                { "f", "search" },
                { "tz", "America/New_York" },
                { "s", searchTerm }
            };
            var response = await RequestWithCookiesAndRetryAsync(ApiEndpoint + queryParameters.GetQueryString());
            if (response.Status != HttpStatusCode.OK)
            {
                throw new WebException($"SubsPlease search returned unexpected result. Expected 200 OK but got {response.Status}.", WebExceptionStatus.ProtocolError);
            }

            var results = ParseApiResults(response.ContentString);
            var filteredResults = results.Where(release => query.MatchQueryStringAND(release.Title));

            // If we detected a resolution in the search terms earlier, filter by it
            if (resMatch.Success)
            {
                filteredResults = filteredResults.Where(release => release.Title.IndexOf(resMatch.Value, StringComparison.OrdinalIgnoreCase) >= 0);
            }

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
            {
                throw new WebException($"SubsPlease search returned unexpected result. Expected 200 OK but got {response.Status}.", WebExceptionStatus.ProtocolError);
            }

            return ParseApiResults(response.ContentString);
        }

        private List<ReleaseInfo> ParseApiResults(string json)
        {
            var releaseInfo = new List<ReleaseInfo>();

            // When there are no results, the API returns an empty array or empty response instead of an object
            if (string.IsNullOrWhiteSpace(json) || json == "[]")
            {
                return releaseInfo;
            }

            var releases = JsonConvert.DeserializeObject<Dictionary<string, SubsPleaseRelease>>(json);

            foreach (var keyValue in releases)
            {
                var r = keyValue.Value;

                var baseRelease = new ReleaseInfo
                {
                    Details = new Uri(SiteLink + $"shows/{r.Page}/"),
                    PublishDate = r.ReleaseDate.DateTime,
                    Files = 1,
                    Category = new List<int> { TorznabCatType.TVAnime.ID },
                    Seeders = 1,
                    Peers = 2,
                    MinimumRatio = 1,
                    MinimumSeedTime = 172800, // 48 hours
                    DownloadVolumeFactor = 0,
                    UploadVolumeFactor = 1
                };

                if (r.Episode.ToLowerInvariant() == "movie")
                {
                    baseRelease.Category.Add(TorznabCatType.MoviesOther.ID);
                }

                foreach (var d in r.Downloads)
                {
                    var release = (ReleaseInfo)baseRelease.Clone();
                    // Ex: [SubsPlease] Shingeki no Kyojin (The Final Season) - 64 (1080p)
                    release.Title += $"[SubsPlease] {r.Show} - {r.Episode} ({d.Resolution}p)";
                    release.MagnetUri = new Uri(d.Magnet);
                    release.Link = null;
                    release.Guid = new Uri(d.Magnet);
                    release.Size = GetReleaseSize(d);

                    releaseInfo.Add(release);
                }
            }

            return releaseInfo;
        }

        private static long GetReleaseSize(SubsPleaseDownloadInfo info)
        {
            if (info.Magnet.IsNotNullOrWhiteSpace())
            {
                var sizeMatch = _RegexSize.Match(info.Magnet);

                if (sizeMatch.Success &&
                    long.TryParse(sizeMatch.Groups["size"].Value, out var releaseSize)
                    && releaseSize > 0)
                {
                    return releaseSize;
                }
            }

            // The API doesn't tell us file size, so give an estimate based on resolution
            return info.Resolution switch
            {
                "1080" => 1.3.Gigabytes(),
                "720" => 700.Megabytes(),
                "480" => 350.Megabytes(),
                _ => 1.Gigabytes()
            };
        }

        public class SubsPleaseRelease
        {
            public string Time { get; set; }

            [JsonProperty("release_date")]
            public DateTimeOffset ReleaseDate { get; set; }
            public string Show { get; set; }
            public string Episode { get; set; }
            public SubsPleaseDownloadInfo[] Downloads { get; set; }
            public string Xdcc { get; set; }
            public string ImageUrl { get; set; }
            public string Page { get; set; }
        }

        public class SubsPleaseDownloadInfo
        {
            [JsonProperty("res")]
            public string Resolution { get; set; }
            public string Magnet { get; set; }
        }
    }
}
