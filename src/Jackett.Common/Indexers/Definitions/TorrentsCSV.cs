using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Jackett.Common.Extensions;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Serializer;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class TorrentsCSV : IndexerBase
    {
        public override string Id => "torrentscsv";
        public override string Name => "Torrents.csv";
        public override string Description => "Torrents.csv is a self-hostable, open source torrent search engine and database";
        public override string SiteLink { get; protected set; } = "https://torrents-csv.com/";
        public override string[] LegacySiteLinks => new[]
        {
            "https://torrents-csv.ml/",
        };
        public override string Language => "en-US";
        public override string Type => "public";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private string SearchEndpoint => SiteLink + "service/search";

        private new ConfigurationData configData => base.configData;

        public TorrentsCSV(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps, ICacheService cs)
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

            // torrents.csv doesn't return categories
            caps.Categories.AddCategoryMapping(1, TorznabCatType.Other);

            return caps;
        }


        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var releases = await PerformQuery(new TorznabQuery());

            await ConfigureIfOK(string.Empty, releases.Any(),
                                () => throw new Exception("Error: 0 results found!"));

            return IndexerConfigurationStatus.Completed;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();

            // not supported
            if (searchString.IsNullOrWhiteSpace())
            {
                releases.Add(new ReleaseInfo
                {
                    Title = "[NOT IMPLEMENTED] Empty search is unsupported in this indexer",
                    Guid = new Uri(SiteLink),
                    Details = new Uri(SiteLink),
                    MagnetUri = new Uri("magnet:?xt=urn:btih:3333333333333333333333333333333333333333"), // unknown torrent
                    Category = new List<int> { TorznabCatType.Other.ID },
                    PublishDate = new DateTime(),
                    Size = 0,
                    Grabs = 0,
                    Seeders = 0,
                    Peers = 0,
                    DownloadVolumeFactor = 0,
                    UploadVolumeFactor = 1
                });

                return releases;
            }

            // search needs at least 3 characters
            if (searchString.IsNotNullOrWhiteSpace() && searchString.Length < 3)
            {
                return releases;
            }

            var qc = new NameValueCollection
            {
                { "size", "100" },
                { "q", searchString }
            };

            var searchUrl = SearchEndpoint + "?" + qc.GetQueryString();
            var response = await RequestWithCookiesAndRetryAsync(searchUrl);

            try
            {
                var jsonResponse = STJson.Deserialize<TorrentsCSVResponse>(response.ContentString);

                foreach (var torrent in jsonResponse.Torrents)
                {
                    if (torrent == null)
                    {
                        throw new Exception("Error: No data returned!");
                    }

                    var infoHash = torrent.InfoHash;
                    var title = torrent.Name;
                    var seeders = torrent.Seeders ?? 0;
                    var leechers = torrent.Leechers ?? 0;
                    var grabs = torrent.Completed ?? 0;
                    var publishDate = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(torrent.Created);

                    var release = new ReleaseInfo
                    {
                        Guid = new Uri($"magnet:?xt=urn:btih:{infoHash}"),
                        Details = new Uri($"{SiteLink}search?q={title}"), // there is no details link
                        Title = title,
                        InfoHash = infoHash, // magnet link is auto generated from infohash
                        Category = new List<int> { TorznabCatType.Other.ID },
                        PublishDate = publishDate,
                        Size = torrent.Size,
                        Grabs = grabs,
                        Seeders = seeders,
                        Peers = leechers + seeders,
                        DownloadVolumeFactor = 0,
                        UploadVolumeFactor = 1
                    };

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(response.ContentString, ex);
            }

            return releases
                   .OrderByDescending(o => o.PublishDate)
                   .ToArray();
        }
    }

    public class TorrentsCSVResponse
    {
        public IReadOnlyCollection<TorrentsCSVTorrent> Torrents { get; set; }
    }

    public class TorrentsCSVTorrent
    {
        [JsonPropertyName("infohash")]
        public string InfoHash { get; set; }

        public string Name { get; set; }

        [JsonPropertyName("size_bytes")]
        public long Size { get; set; }

        [JsonPropertyName("created_unix")]
        public long Created { get; set; }

        public int? Leechers { get; set; }

        public int? Seeders { get; set; }

        public int? Completed { get; set; }
    }
}
