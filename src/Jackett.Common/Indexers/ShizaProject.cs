using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class ShizaProject : IndexerBase
    {
        public override string Id => "shizaroject";
        public override string Name => "ShizaProject";
        public override string Description => "ShizaProject Tracker is a Public RUSSIAN tracker and release group for ANIME";
        public override string SiteLink { get; protected set; } = "https://shiza-project.com/";
        public override string[] LegacySiteLinks => new[]
        {
            "http://shiza-project.com/" // site is forcing https
        };
        public override string Language => "ru-RU";
        public override string Type => "public";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        public ShizaProject(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
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

            caps.Categories.AddCategoryMapping(1, TorznabCatType.TVAnime, "TV");
            caps.Categories.AddCategoryMapping(2, TorznabCatType.TVAnime, "TV_SPECIAL");
            caps.Categories.AddCategoryMapping(3, TorznabCatType.TVAnime, "ONA");
            caps.Categories.AddCategoryMapping(4, TorznabCatType.TVAnime, "OVA");
            caps.Categories.AddCategoryMapping(5, TorznabCatType.Movies, "MOVIE");
            caps.Categories.AddCategoryMapping(6, TorznabCatType.Movies, "SHORT_MOVIE");

            return caps;
        }

        /// <summary>
        /// http://shiza-project.com/graphql
        /// </summary>
        private string GraphqlEndpointUrl => SiteLink + "graphql";

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            IsConfigured = true;
            SaveConfig();

            return await Task.FromResult(IndexerConfigurationStatus.Completed);
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releasesQuery = new
            {
                operationName = "fetchReleases",
                variables = new
                {
                    first = 50, //Number of fetched releases (required parameter) TODO: consider adding pagination
                    query = query.SearchTerm
                },
                query = @"
                query fetchReleases($first: Int, $query: String) {
                    releases(first: $first, query: $query) {
                        edges {
                            node {
                                name
                                type
                                originalName
                                alternativeNames
                                publishedAt
                                slug
                                posters {
                                    preview: resize(width: 360, height: 500) {
                                        url
                                    }
                                }
                                torrents {
                                    synopsis
                                    downloaded
                                    seeders
                                    leechers
                                    size
                                    magnetUri
                                    updatedAt
                                    file {
                                        url
                                    }
                                    videoQualities
                                }
                            }
                        }
                    }
                }"
            };
            var headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json; charset=utf-8" },
            };
            var response = await RequestWithCookiesAndRetryAsync(GraphqlEndpointUrl, method: RequestType.POST, rawbody: JsonConvert.SerializeObject(releasesQuery), headers: headers);
            var j = JsonConvert.DeserializeObject<ReleasesResponse>(response.ContentString);

            var releases = new List<ReleaseInfo>();

            foreach (var e in j.Data.Releases.Edges)
            {
                var n = e.Node;
                var baseRelease = new ReleaseInfo
                {
                    Poster = GetFirstPoster(n),
                    Details = new Uri(SiteLink + "releases/" + n.Slug),
                    DownloadVolumeFactor = 0,
                    UploadVolumeFactor = 1,
                    Category = MapTrackerCatDescToNewznab(n.Type)
                };

                foreach (var t in n.Torrents)
                {
                    var release = (ReleaseInfo)baseRelease.Clone();

                    release.Title = $"{ComposeTitle(n, t)}{GetTitleQualities(t)}";
                    release.Size = t.Size;
                    release.Seeders = t.Seeders;
                    release.Peers = t.Leechers + t.Seeders;
                    release.Grabs = t.Downloaded;
                    release.Link = t.File?.Url;
                    release.Guid = t.File?.Url;
                    release.MagnetUri = t.MagnetUri;
                    release.PublishDate = GetActualPublishDate(n, t);
                    releases.Add(release);
                }
            }

            return releases;
        }

        private string ComposeTitle(Node n, Torrent t)
        {
            var allNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                n.Name,
                n.OriginalName
            };
            allNames.UnionWith(n.AlternativeNames);

            return $"{string.Join(" / ", allNames)} {t.Synopsis}";
        }

        private DateTime GetActualPublishDate(Node n, Torrent t)
        {
            if (n.PublishedAt == null)
            {
                return t.UpdatedAt;
            }

            return t.UpdatedAt > n.PublishedAt ? t.UpdatedAt : n.PublishedAt.Value;
        }

        private string GetTitleQualities(Torrent t)
        {
            var s = " [";

            foreach (var q in t.VideoQualities)
            {
                s += " " + q;
            }

            return s + " ]";
        }

        private Uri GetFirstPoster(Node n) => n.Posters?.FirstOrDefault()?.Preview?.Url;

        public partial class ReleasesResponse
        {
            [JsonProperty("data")]
            public Data Data { get; set; }
        }

        public partial class Data
        {
            [JsonProperty("releases")]
            public Releases Releases { get; set; }
        }

        public partial class Releases
        {
            [JsonProperty("edges")]
            public Edge[] Edges { get; set; }
        }

        public partial class Edge
        {
            [JsonProperty("node")]
            public Node Node { get; set; }
        }

        public partial class Node
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("originalName")]
            public string OriginalName { get; set; }

            [JsonProperty("alternativeNames")]
            public string[] AlternativeNames { get; set; }

            [JsonProperty("publishedAt")]
            public DateTime? PublishedAt { get; set; }

            [JsonProperty("slug")]
            public string Slug { get; set; }

            [JsonProperty("posters")]
            public Poster[] Posters { get; set; }

            [JsonProperty("torrents")]
            public Torrent[] Torrents { get; set; }

            public string Type { get; set; }
        }

        public partial class Poster
        {
            [JsonProperty("preview")]
            public Preview Preview { get; set; }
        }

        public partial class Preview
        {
            [JsonProperty("url")]
            public Uri Url { get; set; }
        }

        public partial class Torrent
        {
            public string Synopsis { get; set; }

            [JsonProperty("downloaded")]
            public long Downloaded { get; set; }

            [JsonProperty("seeders")]
            public long Seeders { get; set; }

            [JsonProperty("leechers")]
            public long Leechers { get; set; }

            [JsonProperty("size")]
            public long Size { get; set; }

            [JsonProperty("magnetUri")]
            public Uri MagnetUri { get; set; }

            [JsonProperty("updatedAt")]
            public DateTime UpdatedAt { get; set; }

            [JsonProperty("file")]
            public Preview File { get; set; }

            [JsonProperty("videoQualities")]
            public string[] VideoQualities { get; set; }
        }
    }
}
