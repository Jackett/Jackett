using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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
    public class YTS : BaseWebIndexer
    {

        public override string[] AlternativeSiteLinks { get; protected set; } = {
            "https://yts.mx/",
            "https://yts.unblockit.dev/",
            "https://yts.unblockninja.com/",
            "https://yts.root.yt/"
        };

        public override string[] LegacySiteLinks { get; protected set; } = {
            "https://yts.ag/",
            "https://yts.am/",
            "https://yts.lt/"
        };

        private string ApiEndpoint => SiteLink + "api/v2/list_movies.json";

        private new ConfigurationData configData
        {
            get => base.configData;
            set => base.configData = value;
        }

        public YTS(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "yts",
                   name: "YTS",
                   description: "YTS is a Public torrent site specialising in HD movies of small size",
                   link: "https://yts.mx/",
                   caps: new TorznabCapabilities
                   {
                       MovieSearchParams = new List<MovieSearchParam> { MovieSearchParam.Q, MovieSearchParam.ImdbId }
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

            webclient.requestDelay = 2.5; // 0.5 requests per second (2 causes problems)

            // note: the API does not support searching with categories, so these are dummy ones for torznab compatibility
            // we map these newznab cats with the returned quality value in the releases routine.
            AddCategoryMapping(45, TorznabCatType.MoviesHD, "Movies/x264/720p");
            AddCategoryMapping(44, TorznabCatType.MoviesHD, "Movies/x264/1080p");
            AddCategoryMapping(46, TorznabCatType.MoviesUHD, "Movies/x264/2160p"); // added for #7010
            AddCategoryMapping(47, TorznabCatType.Movies3D, "Movies/x264/3D");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var releases = await PerformQuery(new TorznabQuery());

            await ConfigureIfOK(string.Empty, releases.Count() > 0,
                                () => throw new Exception("Could not find releases from this URL"));

            return IndexerConfigurationStatus.Completed;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query) => await PerformQuery(query, 0);

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query, int attempts)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();

            var queryCollection = new NameValueCollection
            {

                // without this the API sometimes returns nothing
                { "sort", "date_added" },
                { "limit", "50" }
            };

            if (query.ImdbID != null)
            {
                queryCollection.Add("query_term", query.ImdbID);
            }
            else if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Replace("'", ""); // ignore ' (e.g. search for america's Next Top Model)
                queryCollection.Add("query_term", searchString);
            }

            var searchUrl = ApiEndpoint + "?" + queryCollection.GetQueryString();
            var response = await RequestWithCookiesAndRetryAsync(searchUrl);

            try
            {
                // returned content might start with an html error message, remove it first
                var jsonStart = response.ContentString.IndexOf('{');
                var jsonContentStr = response.ContentString.Remove(0, jsonStart);

                var jsonContent = JObject.Parse(jsonContentStr);

                var result = jsonContent.Value<string>("status");
                if (result != "ok") // query was not successful
                {
                    return releases.ToArray();
                }

                var data_items = jsonContent.Value<JToken>("data");
                var movie_count = data_items.Value<int>("movie_count");
                if (movie_count < 1) // no results found in query
                {
                    return releases.ToArray();
                }

                var movies = data_items.Value<JToken>("movies");
                if (movies == null)
                    throw new Exception("API error, movies missing");

                foreach (var movie_item in movies)
                {
                    var torrents = movie_item.Value<JArray>("torrents");
                    if (torrents == null)
                        continue;
                    foreach (var torrent_info in torrents)
                    {
                        //TODO change to initializer
                        var release = new ReleaseInfo();

                        // append type: BRRip or WEBRip, resolves #3558 via #4577
                        var type = torrent_info.Value<string>("type");
                        switch (type)
                        {
                            case "web":
                                type = "WEBRip";
                                break;
                            default:
                                type = "BRRip";
                                break;
                        }
                        var quality = torrent_info.Value<string>("quality");
                        var title = movie_item.Value<string>("title").Replace(":","").Replace(' ', '.');
                        var year = movie_item.Value<int>("year");
                        release.Title = $"{title}.{year}.{quality}.{type}-YTS";
                        var imdb = movie_item.Value<string>("imdb_code");
                        release.Imdb = ParseUtil.GetImdbID(imdb);

                        release.InfoHash = torrent_info.Value<string>("hash"); // magnet link is auto generated from infohash

                        // ex: 2015-08-16 21:25:08 +0000
                        var dateStr = torrent_info.Value<string>("date_uploaded");
                        var dateTime = DateTime.ParseExact(dateStr, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                        release.PublishDate = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc).ToLocalTime();
                        release.Link = new Uri(torrent_info.Value<string>("url"));
                        release.Seeders = torrent_info.Value<int>("seeds");
                        release.Peers = torrent_info.Value<int>("peers") + release.Seeders;
                        release.Size = torrent_info.Value<long>("size_bytes");
                        release.DownloadVolumeFactor = 0;
                        release.UploadVolumeFactor = 1;

                        release.Details = new Uri(movie_item.Value<string>("url"));
                        release.Poster = new Uri(movie_item.Value<string>("large_cover_image"));
                        release.Guid = release.Link;

                        // map the quality to a newznab category for torznab compatibility (for Radarr, etc)
                        switch (quality)
                        {
                            case "720p":
                                release.Category = MapTrackerCatToNewznab("45");
                                break;
                            case "1080p":
                                release.Category = MapTrackerCatToNewznab("44");
                                break;
                            case "2160p":
                                release.Category = MapTrackerCatToNewznab("46");
                                break;
                            case "3D":
                                release.Category = MapTrackerCatToNewznab("47");
                                break;
                            default:
                                release.Category = MapTrackerCatToNewznab("45");
                                break;
                        }
                        releases.Add(release);
                    }
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
