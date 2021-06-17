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
        private string SearchURl => SiteLink + "api/torrents/filter";
        private string TorrentsUrl => SiteLink + "api/torrents";

        private new ConfigurationDataAPIKey configData => (ConfigurationDataAPIKey)base.configData;

        public DanishBytes(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "danishbytes",
                   name: "DanishBytes",
                   description: "DanishBytes is a Private Danish Tracker",
                   link: "https://danishbytes.org/",
                   caps: new TorznabCapabilities
                   {
                       LimitsDefault = 15,
                       LimitsMax = 15,
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
                {"name", query.GetQueryString()},
                {"api_token", configData.Key.Value},
            };
            foreach (var cat in MapTorznabCapsToTrackers(query))
                qc.Add("categories[]", cat);

            if (query.IsImdbQuery)
                qc.Add("imdb", query.ImdbID);
            if (query.IsTvdbSearch)
                qc.Add("tvdb", query.TvdbID.ToString());
            if (query.IsTmdbQuery)
                qc.Add("tmdb", query.TmdbID.ToString());

            var requestUrl = UseFilterEndpoint(query) ? SearchURl : TorrentsUrl;
            var searchUrl = requestUrl + "?" + qc.GetQueryString();

            var results = await RequestWithCookiesAsync(searchUrl);
            if (results.Status != HttpStatusCode.OK)
                throw new Exception($"Error code: {results.Status}");

            try
            {
                var dbResponse = JsonConvert.DeserializeObject<DBResponse>(results.ContentString);
                foreach (var torrent in dbResponse.data)
                {
                    var attr = torrent.attributes;
                    var release = new ReleaseInfo
                    {
                        Title = attr.name,
                        Details = new Uri(attr.details_link),
                        Link = new Uri(attr.download_link),
                        PublishDate = attr.created_at,
                        Category = MapTrackerCatDescToNewznab(attr.category),
                        Size = attr.size,
                        Files = attr.num_file,
                        Seeders = attr.seeders,
                        Peers = attr.leechers + attr.seeders,
                        Grabs = attr.times_completed,
                        DownloadVolumeFactor = StringToBool(attr.freeleech) ? 0 : 1,
                        UploadVolumeFactor = StringToBool(attr.double_upload) ? 2 : 1
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

        private bool UseFilterEndpoint(TorznabQuery query) => !string.IsNullOrEmpty(query.GetQueryString()) ||
                                                              (query.Categories.Length > 0 || query.IsImdbQuery ||
                                                               query.IsTvdbSearch);

        private static bool StringToBool(string value) => value.Equals("yes", StringComparison.CurrentCultureIgnoreCase) ||
                                                          value.Equals(
                                                              bool.TrueString, StringComparison.CurrentCultureIgnoreCase) ||
                                                          value.Equals("1");

        private class Torrent
        {
            public int id { get; set; }
            public string type { get; set; }
            public TorrentAttribute attributes { get; set; }
        }

        private class TorrentAttribute
        {
            public string name { get; set; }
            public DateTime? release_year { get; set; }
            public string category { get; set; }
            public string type { get; set; }
            public string resolution { get; set; }
            public long size { get; set; }
            public int num_file { get; set; }
            public string freeleech { get; set; }
            public string double_upload { get; set; }
            public string uploader { get; set; }
            public int seeders { get; set; }
            public int leechers { get; set; }
            public int times_completed { get; set; }
            public string tmdb_id { get; set; }
            public string imdb_id { get; set; }
            public string tvdb_id { get; set; }
            public string mal_id { get; set; }
            public string igdb_id { get; set; }
            public DateTime created_at { get; set; }
            public string download_link { get; set; }
            public string details_link { get; set; }
        }

        private class DBResponse
        {
            public Torrent[] data { get; set; }
        }

    }
}
