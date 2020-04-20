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
    public class Milkie : BaseWebIndexer
    {
        private readonly string APIBase = "https://milkie.cc/api/v1";
        private string TorrentsEndpoint => APIBase + "/torrents";

        private new ConfigurationDataAPIKey configData
        {
            get => (ConfigurationDataAPIKey)base.configData;
            set => base.configData = value;
        }

        public Milkie(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(name: "Milkie",
                description: "Milkie.cc (ME) is private torrent tracker for 0day / general",
                link: "https://milkie.cc/",
                caps: new TorznabCapabilities(),
                configService: configService,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationDataAPIKey())
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";

            AddCategoryMapping("1", TorznabCatType.Movies, "Movies");
            AddCategoryMapping("2", TorznabCatType.TV, "TV");
            AddCategoryMapping("3", TorznabCatType.Audio, "Music");
            AddCategoryMapping("4", TorznabCatType.PCGames, "Game");
            AddCategoryMapping("4", TorznabCatType.Console, "Game");
            AddCategoryMapping("5", TorznabCatType.Books, "Ebook");
            AddCategoryMapping("6", TorznabCatType.PC0day, "App");
            AddCategoryMapping("6", TorznabCatType.PCISO, "App");
            AddCategoryMapping("6", TorznabCatType.PCMac, "App");
            AddCategoryMapping("6", TorznabCatType.PCPhoneAndroid, "App");
            AddCategoryMapping("6", TorznabCatType.PCPhoneIOS, "App");
            AddCategoryMapping("6", TorznabCatType.PCPhoneOther, "App");
            AddCategoryMapping("6", TorznabCatType.OtherMisc, "App");
            AddCategoryMapping("7", TorznabCatType.XXX, "Adult");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            try
            {
                var results = await PerformQuery(new TorznabQuery());

                if (results.Count() == 0)
                    throw new Exception("Testing returned no results!");

                IsConfigured = true;
                SaveConfig();
                return IndexerConfigurationStatus.Completed;
            }
            catch (Exception e)
            {
                IsConfigured = false;
                throw new ExceptionWithConfigData(e.Message, configData);
            }
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var queryParams = new NameValueCollection
            {
                { "ps", "100" }
            };

            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                queryParams.Add("query", query.SearchTerm);
            }

            var endpoint = TorrentsEndpoint + "?" + queryParams.GetQueryString();
            var jsonResponse = await RequestStringWithCookies(
                endpoint,
                null,
                null,
                new Dictionary<string, string>() { { "x-milkie-auth", configData.Key.Value } }
            );

            var releases = new List<ReleaseInfo>();

            try
            {
                var response = JsonConvert.DeserializeObject<MilkieResponse>(jsonResponse.Content);

                var dlQueryParams = new NameValueCollection
                {
                    { "key", configData.Key.Value }
                };

                foreach (var torrent in response.Torrents)
                {
                    var release = new ReleaseInfo()
                    {
                        Title = torrent.ReleaseName,
                        Link = new Uri($"{TorrentsEndpoint}/{torrent.Id}/torrent?{dlQueryParams.GetQueryString()}"),
                        Comments = new Uri($"{SiteLink}browse/{torrent.Id}"),
                        Guid = new Uri($"{SiteLink}browse/{torrent.Id}"),
                        Size = torrent.Size,
                        Category = MapTrackerCatToNewznab(torrent.Category.ToString()),
                        Seeders = torrent.Seeders,
                        Peers = torrent.Seeders + torrent.PartialSeeders + torrent.Leechers,
                        Grabs = torrent.Downloaded,
                        UploadVolumeFactor = 1,
                        DownloadVolumeFactor = 0,
                        MinimumRatio = 0,
                        MinimumSeedTime = 0,
                        PublishDate = DateTimeUtil.FromUnknown(torrent.CreatedAt)
                    };

                    releases.Add(release);
                }
            }
            catch(Exception ex)
            {
                OnParseError(jsonResponse.Content, ex);
            }

            return releases;
        }

        private class MilkieResponse
        {
            public int Hits { get; set; }
            public int Took { get; set; }
            public MilkieTorrent[] Torrents { get; set; }
        }

        private class MilkieTorrent
        {
            public string Id { get; set; }
            public string ReleaseName { get; set; }
            public int Category { get; set; }
            public int Downloaded { get; set; }
            public int Seeders { get; set; }
            public int PartialSeeders { get; set; }
            public int Leechers { get; set; }
            public long Size { get; set; }
            public string CreatedAt { get; set; }
        }
    }
}
