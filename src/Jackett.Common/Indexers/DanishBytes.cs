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
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId, TvSearchParam.TvdbId
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
            Language = "en-US";
            Type = "private";

            AddCategoryMapping("1", TorznabCatType.Movies, "Movies");
            AddCategoryMapping("2", TorznabCatType.TV, "TV");
            AddCategoryMapping("3", TorznabCatType.Audio, "Music");
            AddCategoryMapping("4", TorznabCatType.PCGames, "Games");
            AddCategoryMapping("5", TorznabCatType.PC0day, "Appz");
            AddCategoryMapping("8", TorznabCatType.Books, "Bookz");
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
                var jsonContent = JObject.Parse(results.ContentString);
                var rsskey = jsonContent.Value<string>("rsskey");
                foreach (var item in jsonContent.Value<JArray>("torrents"))
                {
                    var torrent = item.ToObject<Torrent>();
                    var release = new ReleaseInfo
                    {
                        Title = torrent.name,
                        Details = new Uri($"{SiteLink}torrents/{torrent.id}"),
                        Link = new Uri($"{SiteLink}torrent/download/{torrent.id}.{rsskey}"),
                        PublishDate = torrent.created_at,
                        Category = MapTrackerCatToNewznab(torrent.category_id),
                        Size = torrent.size,
                        Seeders = torrent.seeders,
                        Peers = torrent.leechers + torrent.seeders,
                        Grabs = torrent.times_completed,
                        DownloadVolumeFactor = torrent.free ? 0 : 1,
                        UploadVolumeFactor = torrent.doubleup ? 2 : 1
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
            public long size { get; set; }
            public int leechers { get; set; }
            public int seeders { get; set; }
            public int times_completed { get; set; }
            public string category_id { get; set; }
            public bool free { get; set; }
            public bool doubleup { get; set; }
            public DateTime created_at { get; set; }
        }

    }
}
