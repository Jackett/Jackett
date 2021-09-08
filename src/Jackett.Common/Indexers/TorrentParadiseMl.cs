using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    public class TorrentParadiseMl : BaseWebIndexer
    {
        private string ApiUrl => SiteLink + "api/";

        public TorrentParadiseMl(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "torrent-paradise-ml",
                   name: "Torrent Paradise (ML)",
                   description: "The most innovative torrent site",
                   link: "https://torrent-paradise.ml/",
                   caps: new TorznabCapabilities
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

            AddCategoryMapping(8000, TorznabCatType.Other);
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var query = new TorznabQuery { IsTest = true };
            var releases = await PerformQuery(query);

            await ConfigureIfOK(string.Empty, releases.Any(),
                                () => throw new Exception("Could not find releases from this URL"));

            return IndexerConfigurationStatus.Completed;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var searchUrl = CreateSearchUrl(query);
            if (string.IsNullOrWhiteSpace(searchUrl))
                return new List<ReleaseInfo>();

            var response = await RequestWithCookiesAndRetryAsync(searchUrl);

            try
            {
                var results = JsonConvert.DeserializeObject<IEnumerable<TorrentParadiseResult>>(response.ContentString);
                return results == null ? new List<ReleaseInfo>() : ConvertResultsIntoReleaseInfos(results);
            }
            catch (Exception ex)
            {
                throw new Exception("Error while parsing json: " + response.ContentString, ex);
            }
        }

        private string CreateSearchUrl(TorznabQuery query)
        {
            var searchTerm = query.GetQueryString();
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = DateTime.Now.Year.ToString();
            }

            var qc = new NameValueCollection
            {
                {"q", searchTerm}
            };

            return ApiUrl + "search?" + qc.GetQueryString();
        }

        private IEnumerable<ReleaseInfo> ConvertResultsIntoReleaseInfos(IEnumerable<TorrentParadiseResult> results)
        {
            foreach (var result in results)
            {
                yield return ConvertResultIntoReleaseInfo(result);
            }
        }

        private ReleaseInfo ConvertResultIntoReleaseInfo(TorrentParadiseResult result)
        {
            return new ReleaseInfo
            {
                Title = result.Text,
                Size = result.Size,
                Seeders = result.Seeders,
                Peers = result.Seeders + result.Leechers,
                InfoHash = result.Id,
                PublishDate = DateTime.Now,
                Details = new Uri(SiteLink),
                DownloadVolumeFactor = 0,
                UploadVolumeFactor = 1,
                Category = new List<int> { TorznabCatType.Other.ID }
            };
        }

        private class TorrentParadiseResult
        {
            [JsonConstructor]
            public TorrentParadiseResult(string id, string text, string len, string s, string l)
            {
                Id = id;
                Text = text;
                Size = ToLong(len);
                Seeders = ToLong(s);
                Leechers = ToLong(l);
            }

            public string Id { get; }

            public string Text { get; }

            public long? Size { get; }

            public long? Seeders { get; }

            public long? Leechers { get; }

            private long? ToLong(string str) => Convert.ToInt64(str);
        }
    }
}
