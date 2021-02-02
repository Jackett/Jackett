using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    public class TorrentParadise : BaseWebIndexer
    {
        public class TorrentParadiseResult
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

        private string ApiUrl => SiteLink + "api/";

        public TorrentParadise(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "torrent-paradise",
                   name: "Torrent Paradise",
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
            Encoding = Encoding.GetEncoding("windows-1252");
            Language = "en-us";
            Type = "public";

            AddCategoryMapping(8000, TorznabCatType.Other);
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var query = new TorznabQuery { SearchTerm = DateTime.Now.Year.ToString(), IsTest = true };
            var releases = await PerformQuery(query);

            await ConfigureIfOK(string.Empty, releases.Any(),
                                () => throw new Exception("Could not find releases from this URL"));

            return IndexerConfigurationStatus.Completed;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var searchUrl = CreateSearchUrl(query);
            if (string.IsNullOrWhiteSpace(searchUrl)) return new List<ReleaseInfo>();

            var response = await RequestWithCookiesAndRetryAsync(searchUrl);

            try
            {
                var results = JsonConvert.DeserializeObject<IEnumerable<TorrentParadiseResult>>(response.ContentString);
                if (results == null) return new List<ReleaseInfo>();
                return ConvertResultsIntoReleaseInfos(results);
            }
            catch (Exception ex)
            {
                throw new Exception("Error while parsing json: " + response.ContentString, ex);
            }
        }

        private string CreateSearchUrl(TorznabQuery query)
        {
            var queryString = query.GetQueryString();
            if (string.IsNullOrWhiteSpace(queryString))
            {
                if (!query.IsTest)
                {
                    return null;
                }
                query.SearchTerm = DateTime.Now.Year.ToString();
                queryString = query.GetQueryString();
            }

            return ApiUrl + "search?q=" + queryString;
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
                MagnetUri = CreateMagnetUri(result),
                PublishDate = DateTime.Now,
                Details = new Uri(SiteLink),
                DownloadVolumeFactor = 1,
                UploadVolumeFactor = 1
            };
        }

        private Uri CreateMagnetUri(TorrentParadiseResult result)
        {
            var uriString = "magnet:?xt=urn:btih:" + result.Id +
                "&tr=udp%3A%2F%2Ftracker.coppersurfer.tk%3A6969%2Fannounce" +
                "&tr=udp%3A%2F%2Ftracker.opentrackr.org%3A1337%2Fannounce" +
                "&tr=udp%3A%2F%2Ftracker.internetwarriors.net%3A1337";

            return new Uri(uriString);
        }
    }
}
