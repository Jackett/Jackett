using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Newtonsoft.Json.Linq;
using NLog;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class Magnetico : BaseWebIndexer
    {
        private string SearchURl => SiteLink + "api/v0.1/torrents";
        private string TorrentsUrl => SiteLink + "torrents";

        private new ConfigurationDataBasicLogin configData => (ConfigurationDataBasicLogin)base.configData;

        public Magnetico(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "magnetico",
                   name: "Magnetico (Local DHT)",
                   description: "Magnetico is a self-hosted BitTorrent DHT search engine",
                   link: "http://127.0.0.1:8080/",
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
                   configData: new ConfigurationDataBasicLogin("Configure the URL, username and password from your local magneticow.<br>" +
                               "Default credentials are: username=username, password=password.<br>" +
                               "If you have many torrents, it is recommended to use PostgreSQL database to make queries faster. With SQLite, timeouts may occur."))

        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "semi-private";

            var sort = new ConfigurationData.SelectItem(new Dictionary<string, string>
                {
                    {"DISCOVERED_ON", "discovered"},
                    {"TOTAL_SIZE", "size"},
                    {"N_FILES", "files"},
                    {"RELEVANCE", "relevance"}
                })
                { Name = "Sort requested from site", Value = "discovered" };
            configData.AddDynamic("sort", sort);

            var order = new ConfigurationData.SelectItem(new Dictionary<string, string>
                {
                    {"false", "desc"},
                    {"true", "asc"}
                })
                { Name = "Order requested from site", Value = "false" };
            configData.AddDynamic("order", order);

            AddCategoryMapping("1", TorznabCatType.Other);
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            await PerformQuery(new TorznabQuery()); // throws exception if there is an error

            IsConfigured = true;
            SaveConfig();
            return IndexerConfigurationStatus.Completed;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            var qc = new NameValueCollection
            {
                {"query", query.GetQueryString()},
                {"orderBy", ((ConfigurationData.SelectItem)configData.GetDynamic("sort")).Value},
                {"ascending", ((ConfigurationData.SelectItem)configData.GetDynamic("order")).Value},
                {"limit", "100"}
            };
            var searchUrl = SearchURl + "?" + qc.GetQueryString();

            var results = await RequestWithCookiesAsync(searchUrl, headers: GetAuthorizationHeaders());
            if (results.Status != HttpStatusCode.OK)
                throw new Exception($"Error code: {results.Status}");

            try
            {
                var torrents = JArray.Parse(results.ContentString).ToObject<List<Torrent>>();
                foreach (var torrent in torrents)
                {
                    var details = new Uri($"{TorrentsUrl}/{torrent.infoHash}");
                    var publishDate = DateTimeUtil.UnixTimestampToDateTime(torrent.discoveredOn);

                    var release = new ReleaseInfo
                    {
                        Title = torrent.name,
                        Details = new Uri($"{TorrentsUrl}/{torrent.infoHash}"),
                        Guid = details,
                        InfoHash = torrent.infoHash,
                        PublishDate = publishDate,
                        Category = new List<int>{ TorznabCatType.Other.ID },
                        Size = torrent.size,
                        Files = torrent.nFiles,
                        Seeders = 1,
                        Peers = 1,
                        DownloadVolumeFactor = 0,
                        UploadVolumeFactor = 1
                    };

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(results.ContentString, ex);
            }

            return releases;
        }

        private Dictionary<string, string> GetAuthorizationHeaders()
        {
            var username = configData.Username.Value;
            var password = configData.Password.Value;
            var encoded = Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(username + ":" + password));
            var headers = new Dictionary<string, string>
            {
                {"Authorization", "Basic " + encoded}
            };
            return headers;
        }

        private class Torrent
        {
            public string infoHash { get; set; }
            public long id { get; set; }
            public string name { get; set; }
            public long size { get; set; }
            public long discoveredOn { get; set; }
            public long nFiles { get; set; }
            public long relevance { get; set; }
        }
    }
}
