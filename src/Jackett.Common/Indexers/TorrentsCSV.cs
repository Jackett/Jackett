using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class TorrentsCSV : BaseWebIndexer
    {
        private string SearchEndpoint => SiteLink + "service/search";

        private new ConfigurationData configData => base.configData;

        public TorrentsCSV(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "torrentscsv",
                   name: "Torrents.csv",
                   description: "Torrents.csv is a self-hostable, open source torrent search engine and database",
                   link: "https://torrents-csv.ml/",
                   caps: new TorznabCapabilities
                   {
                       TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                       },
                       MovieSearchParams = new List<MovieSearchParam>
                       {
                           MovieSearchParam.Q
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
            Language = "en-us";
            Type = "public";

            // torrents.csv doesn't return categories
            AddCategoryMapping(1, TorznabCatType.Other);
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var releases = await PerformQuery(new TorznabQuery());

            await ConfigureIfOK(string.Empty, releases.Any(),
                                () => throw new Exception("Error: 0 results found!"));

            return IndexerConfigurationStatus.Completed;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query) {
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();

            if (string.IsNullOrWhiteSpace(searchString)) // not supported
            {
                releases.Add(new ReleaseInfo
                {
                    Title = "[NOT IMPLEMENTED] Empty search is unsupported in this indexer",
                    Guid = new Uri(SiteLink),
                    Details = new Uri(SiteLink),
                    MagnetUri = new Uri("magnet:?xt=urn:btih:3333333333333333333333333333333333333333"), // unknown torrent
                    Category = new List<int> { TorznabCatType.Other.ID },
                    PublishDate = new DateTime(),
                    Grabs = 0,
                    Seeders = 0,
                    Peers = 0,
                    DownloadVolumeFactor = 0,
                    UploadVolumeFactor = 1
                });
                return releases;
            }

            if (!string.IsNullOrWhiteSpace(searchString) && searchString.Length < 3)
                return releases; // search needs at least 3 characters

            var qc = new NameValueCollection
            {
                { "size", "100" },
                { "q", searchString }
            };

            var searchUrl = SearchEndpoint + "?" + qc.GetQueryString();
            var response = await RequestWithCookiesAndRetryAsync(searchUrl);
            try
            {
                var jsonStart = response.ContentString;
                var jsonContent = JArray.Parse(jsonStart);

                foreach (var torrent in jsonContent)
                {
                    if (torrent == null)
                        throw new Exception("Error: No data returned!");

                    var title = torrent.Value<string>("name");
                    var size = torrent.Value<long>("size_bytes");
                    var seeders = torrent.Value<int>("seeders");
                    var leechers = torrent.Value<int>("leechers");
                    var grabs = ParseUtil.CoerceInt(torrent.Value<string>("completed") ?? "0");
                    var infoHash = torrent.Value<JToken>("infohash").ToString();

                    // convert unix timestamp to human readable date
                    var publishDate = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                    publishDate = publishDate.AddSeconds(torrent.Value<long>("created_unix"));

                    var release = new ReleaseInfo
                    {
                        Title = title,
                        Details = new Uri(SiteLink), // there is no details link
                        Guid = new Uri($"magnet:?xt=urn:btih:{infoHash}"),
                        InfoHash = infoHash, // magnet link is auto generated from infohash
                        Category = new List<int> { TorznabCatType.Other.ID },
                        PublishDate = publishDate,
                        Size = size,
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
            return releases;
        }
    }
}
