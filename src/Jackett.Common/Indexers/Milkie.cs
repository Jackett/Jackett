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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class Milkie : BaseWebIndexer
    {
        private string TorrentsEndpoint => SiteLink + "api/v1/torrents";

        private new ConfigurationDataAPIKey configData => (ConfigurationDataAPIKey)base.configData;

        public Milkie(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(id: "milkie",
                   name: "Milkie",
                   description: "Milkie.cc (ME) is private torrent tracker for 0day / general",
                   link: "https://milkie.cc/",
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
                   configData: new ConfigurationDataAPIKey())
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";

            AddCategoryMapping("1", TorznabCatType.Movies, "Movies");
            AddCategoryMapping("2", TorznabCatType.TV, "TV");
            AddCategoryMapping("3", TorznabCatType.Audio, "Music");
            AddCategoryMapping("4", TorznabCatType.PCGames, "Games");
            AddCategoryMapping("5", TorznabCatType.Books, "Ebook");
            AddCategoryMapping("6", TorznabCatType.PC, "Apps");
            AddCategoryMapping("7", TorznabCatType.XXX, "Adult");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            try
            {
                var results = await PerformQuery(new TorznabQuery());
                if (!results.Any())
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
            var qc = new NameValueCollection
            {
                { "ps", "100" }
            };

            if (!string.IsNullOrWhiteSpace(query.GetQueryString()))
                qc.Add("query", query.GetQueryString());

            if (query.HasSpecifiedCategories)
                qc.Add("categories", string.Join(",", MapTorznabCapsToTrackers(query)));

            var endpoint = TorrentsEndpoint + "?" + qc.GetQueryString();
            var headers = new Dictionary<string, string>
            {
                { "x-milkie-auth", configData.Key.Value }
            };
            var jsonResponse = await RequestWithCookiesAsync(endpoint, headers: headers);

            var releases = new List<ReleaseInfo>();

            try
            {
                var response = JsonConvert.DeserializeObject<MilkieResponse>(jsonResponse.ContentString);

                var dlQueryParams = new NameValueCollection
                {
                    { "key", configData.Key.Value }
                };

                foreach (var torrent in response.Torrents)
                {
                    var link = new Uri($"{TorrentsEndpoint}/{torrent.Id}/torrent?{dlQueryParams.GetQueryString()}");
                    var details = new Uri($"{SiteLink}browse/{torrent.Id}");
                    var publishDate = DateTimeUtil.FromUnknown(torrent.CreatedAt);

                    var release = new ReleaseInfo
                    {
                        Title = torrent.ReleaseName,
                        Link = link,
                        Details = details,
                        Guid = details,
                        PublishDate = publishDate,
                        Category = MapTrackerCatToNewznab(torrent.Category.ToString()),
                        Size = torrent.Size,
                        Seeders = torrent.Seeders,
                        Peers = torrent.Seeders + torrent.PartialSeeders + torrent.Leechers,
                        Grabs = torrent.Downloaded,
                        UploadVolumeFactor = 1,
                        DownloadVolumeFactor = 0,
                        MinimumRatio = 1,
                        MinimumSeedTime = 172800 // 48 hours
                    };

                    releases.Add(release);
                }
            }
            catch(Exception ex)
            {
                OnParseError(jsonResponse.ContentString, ex);
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
