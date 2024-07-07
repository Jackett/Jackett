using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jackett.Common.Extensions;
using Jackett.Common.Helpers;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers.Definitions
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

        public SubsPlease(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps, ICacheService cs)
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

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new SubsPleaseRequestGenerator(SiteLink);
        }

        public override IParseIndexerResponse GetParser()
        {
            return new SubsPleaseParser(SiteLink);
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var releases = await PerformQuery(new TorznabQuery());

            await ConfigureIfOK(string.Empty, releases.Any(), () =>
                throw new Exception("Could not find releases from this URL"));

            return IndexerConfigurationStatus.Completed;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = await base.PerformQuery(query);

            if (query.SearchTerm.IsNotNullOrWhiteSpace())
            {
                releases = releases.Where(release => query.MatchQueryStringAND(release.Title));

                // If we detected a resolution in the search terms earlier, filter by it
                var resolutionMatch = Regex.Match(query.SearchTerm, @"\d{3,4}p", RegexOptions.IgnoreCase);

                if (resolutionMatch.Success)
                {
                    releases = releases.Where(release => release.Title.IndexOf(resolutionMatch.Value, StringComparison.OrdinalIgnoreCase) >= 0);
                }
            }

            return releases;
        }
    }

    public class SubsPleaseRequestGenerator : IIndexerRequestGenerator
    {
        private readonly string _siteLink;

        private static readonly Regex _ResolutionRegex = new Regex(@"\d{3,4}p", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public SubsPleaseRequestGenerator(string siteLink)
        {
            _siteLink = siteLink;
        }

        public IndexerPageableRequestChain GetSearchRequests(TorznabQuery query)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            var queryParameters = new NameValueCollection
            {
                { "tz", "UTC" }
            };

            if (query.SearchTerm.IsNullOrWhiteSpace())
            {
                queryParameters.Set("f", "latest");
            }
            else
            {
                // If the search terms contain [SubsPlease] or SubsPlease, remove them from the query sent to the API
                var searchTerm = Regex.Replace(query.SearchTerm, "\\[?SubsPlease\\]?\\s*", string.Empty, RegexOptions.IgnoreCase).Trim();

                // If the search terms contain a resolution, remove it from the query sent to the API
                var resolutionMatch = _ResolutionRegex.Match(searchTerm);

                if (resolutionMatch.Success)
                {
                    searchTerm = searchTerm.Replace(resolutionMatch.Value, string.Empty);
                }

                // Only include season > 1 in searchTerm, format as S2 rather than S02
                if (query.Season is > 1)
                {
                    searchTerm += $" S{query.Season}";
                    query.Season = 0;
                }

                queryParameters.Set("f", "search");
                queryParameters.Set("s", searchTerm);
            }

            pageableRequests.Add(GetRequest(queryParameters));

            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetRequest(NameValueCollection queryParameters)
        {
            var searchUrl = $"{_siteLink}api/?{queryParameters.GetQueryString()}";

            var webRequest = new WebRequest
            {
                Url = searchUrl,
                Headers = new Dictionary<string, string>
                {
                    { "Accept", "application/json" },
                }
            };

            yield return new IndexerRequest(webRequest);
        }
    }

    public class SubsPleaseParser : IParseIndexerResponse
    {
        private readonly string _siteLink;

        private static readonly Regex _RegexSize = new Regex(@"\&xl=(?<size>\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public SubsPleaseParser(string siteLink)
        {
            _siteLink = siteLink;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var releases = new List<ReleaseInfo>();

            // When there are no results, the API returns an empty array or empty response instead of an object
            if (indexerResponse.Content.IsNullOrWhiteSpace() || indexerResponse.Content == "[]")
            {
                return releases;
            }

            var jsonResponse = JsonConvert.DeserializeObject<Dictionary<string, SubsPleaseRelease>>(indexerResponse.Content);

            foreach (var r in jsonResponse.Values)
            {
                foreach (var d in r.Downloads)
                {
                    var release = new ReleaseInfo
                    {
                        Details = new Uri($"{_siteLink}shows/{r.Page}/"),
                        PublishDate = r.ReleaseDate.LocalDateTime,
                        Files = 1,
                        Category = new List<int> { TorznabCatType.TVAnime.ID },
                        Seeders = 1,
                        Peers = 2,
                        MinimumRatio = 1,
                        MinimumSeedTime = 172800, // 48 hours
                        DownloadVolumeFactor = 0,
                        UploadVolumeFactor = 1
                    };

                    if (r.ImageUrl.IsNotNullOrWhiteSpace())
                    {
                        release.Poster = new Uri(_siteLink + r.ImageUrl.TrimStart('/'));
                    }

                    if (r.Episode.ToLowerInvariant() == "movie")
                    {
                        release.Category.Add(TorznabCatType.MoviesOther.ID);
                    }

                    // Ex: [SubsPlease] Shingeki no Kyojin (The Final Season) - 64 (1080p)
                    release.Title = $"[SubsPlease] {r.Show} - {r.Episode} ({d.Resolution}p)";
                    release.MagnetUri = new Uri(d.Magnet);
                    release.Link = null;
                    release.Guid = new Uri(d.Magnet);
                    release.Size = GetReleaseSize(d);

                    releases.Add(release);
                }
            }

            return releases;
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

        [JsonProperty("image_url")]
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
