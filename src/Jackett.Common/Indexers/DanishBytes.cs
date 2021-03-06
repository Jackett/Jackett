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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class DanishBytes : BaseWebIndexer
    {
        private string SearchURl => SiteLink + "api/torrents/v2/filter";

        private new ConfigurationDataAPIKey configData => (ConfigurationDataAPIKey)base.configData;

        public DanishBytes(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "danishbytes",
                   name: "DanishBytes",
                   description: "DanishBytes is a Private Danish Tracker",
                   link: "https://danishbytes.org/",
                   caps: new TorznabCapabilities
                   {
                       LimitsDefault = 25,
                       LimitsMax = 25,
                       TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.ImdbId, TvSearchParam.TvdbId
                       },
                       MovieSearchParams = new List<MovieSearchParam>
                       {
                           MovieSearchParam.Q, MovieSearchParam.ImdbId, MovieSearchParam.TmdbId
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
                   configData: new ConfigurationDataAPIKey())

        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";

            AddCategoryMapping("1", TorznabCatType.Movies, "Movies");
            AddCategoryMapping("2", TorznabCatType.TV, "TV");
            AddCategoryMapping("3", TorznabCatType.Audio, "Music");
            AddCategoryMapping("4", TorznabCatType.PCGames, "Games");
            AddCategoryMapping("5", TorznabCatType.PC0day, "Appz");
            AddCategoryMapping("6", TorznabCatType.Books, "Bookz");
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
                {"search", query.GetQueryString()},
                {"api_token", configData.Key.Value},
            };

            if (query.IsImdbQuery)
                qc.Add("imdb", query.ImdbID);
            if (query.IsTvdbSearch)
                qc.Add("tvdb", query.TvdbID.ToString());
            if (query.IsTmdbQuery)
                qc.Add("tmdb", query.TmdbID.ToString());

            var requestUrl = SearchURl;
            var searchUrl = requestUrl + "?" + qc.GetQueryString();

            foreach (var cat in MapTorznabCapsToTrackers(query))
                searchUrl += $"&categories[]={cat}";

            var results = await RequestWithCookiesAsync(searchUrl);
            if (results.Status != HttpStatusCode.OK)
                throw new Exception($"Error code: {results.Status}");

            try
            {
                var dbResponse = JsonConvert.DeserializeObject<DBResponse>(results.ContentString);
                foreach (var attr in dbResponse.torrents)
                {
                    var release = new ReleaseInfo
                    {
                        Title = attr.name,
                        Details = new Uri($"{SiteLink}torrents/{attr.id}"),
                        Link = new Uri($"{SiteLink}torrent/download/{attr.id}.{dbResponse.rsskey}"),
                        PublishDate = attr.created_at,
                        Category = MapTrackerCatToNewznab(attr.category_id),
                        Size = attr.size,
                        Seeders = attr.seeders,
                        Peers = attr.leechers + attr.seeders,
                        Grabs = attr.times_completed,
                        DownloadVolumeFactor = attr.free ? 0 : 1,
                        UploadVolumeFactor = attr.doubleup ? 2 : 1
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

        private class Torrent
        {
            public int id { get; set; }
            public string name { get; set; }
            public string info_hash { get; set; }
            public long size { get; set; }
            public int leechers { get; set; }
            public int seeders { get; set; }
            public int times_completed { get; set; }
            public string category_id { get; set; }
            public string tmdb { get; set; }
            public string igdb { get; set; }
            public string mal { get; set; }
            public string tvdb { get; set; }
            public string imdb { get; set; }
            public int stream { get; set; }
            public bool free { get; set; }
            public bool on_fire { get; set; }
            public bool doubleup { get; set; }
            public bool highspeed { get; set; }
            public bool featured { get; set; }
            public bool webstream { get; set; }
            public bool anon { get; set; }
            public bool sticky { get; set; }
            public bool sd { get; set; }
            public DateTime created_at { get; set; }
            public DateTime bumped_at { get; set; }
            public int type_id { get; set; }
            public int resolution_id { get; set; }
            public string poster_image { get; set; }
            public string video { get; set; }
            public int thanks_count { get; set; }
            public int comments_count { get; set; }
            public string getSize { get; set; }
            public string created_at_human { get; set; }
            public bool bookmarked { get; set; }
            public bool liked { get; set; }
            public bool show_last_torrents { get; set; }
        }

        private class PageLinks
        {
            public int to { get; set; }
            public string qty { get; set; }
            public int current_page { get; set; }
        }

        private class DBResponse
        {
            public Torrent[] torrents { get; set; }
            public int resultsCount { get; set; }
            public PageLinks links { get; set; }
            public string currentCount { get; set; }
            public int torrentCountTotal { get; set; }
            public string rsskey { get; set; }
        }

    }
}
