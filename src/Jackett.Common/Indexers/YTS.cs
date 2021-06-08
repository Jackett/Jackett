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
            "https://yts.unblockit.li/",
            "https://yts.unblockninja.com/",
            "https://yts.nocensor.space/"
        };

        public override string[] LegacySiteLinks { get; protected set; } = {
            "https://yts.ag/",
            "https://yts.am/",
            "https://yts.lt/",
            "https://yts.unblockit.dev/",
            "https://yts.root.yt/",
            "https://yts.unblockit.ltd/",
            "https://yts.unblockit.buzz/",
            "https://yts.unblockit.club/",
            "https://yts.unblockit.link/",
            "https://yts.unblockit.onl/"
        };

        private string ApiEndpoint => SiteLink + "api/v2/list_movies.json";

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

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query) => await PerformQueryAsync(query);

        private async Task<IEnumerable<ReleaseInfo>> PerformQueryAsync(TorznabQuery query)
        {
            var searchUrl = CreateSearchUrl(query);
            var response = await RequestWithCookiesAndRetryAsync(searchUrl);
            var releases = ParseWebResult(response);
            return releases;
        }

        private string CreateSearchUrl(TorznabQuery query)
        {
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
            return searchUrl;
        }

        private IEnumerable<ReleaseInfo> ParseWebResult(WebResult webResult)
        {
            var releases = new List<ReleaseInfo>();
            var contentString = webResult.ContentString;
            try
            {
                // returned content might start with an html error message, remove it first
                var jsonStart = contentString.IndexOf('{');
                var jsonContentStr = contentString.Remove(0, jsonStart);

                var jsonContent = JObject.Parse(jsonContentStr);

                var result = jsonContent.Value<string>("status");
                if (result != "ok") // query was not successful
                {
                    return new List<ReleaseInfo>();
                }

                var dataItems = jsonContent.Value<JToken>("data");
                var movieCount = dataItems.Value<int>("movie_count");
                if (movieCount < 1) // no results found in query
                {
                    return new List<ReleaseInfo>();
                }

                var movies = dataItems.Value<JToken>("movies");
                if (movies == null)
                {
                    return new List<ReleaseInfo>();
                }

                foreach (var movie in movies)
                {
                    var torrents = movie.Value<JArray>("torrents");
                    if (torrents == null)
                        continue;
                    foreach (var torrent in torrents)
                    {
                        var release = ParseJsonIntoReleaseInfo(movie, torrent);
                        if (release == null)
                            continue;
                        releases.Add(release);
                    }
                }
            }
            catch (Exception ex)
            {
                OnParseError(contentString, ex);
            }

            return releases;
        }

        private ReleaseInfo ParseJsonIntoReleaseInfo(JToken movie, JToken torrent)
        {
            //TODO change to initializer
            var release = new ReleaseInfo();

            // append type: BRRip or WEBRip, resolves #3558 via #4577
            var type = torrent.Value<string>("type");
            switch (type)
            {
                case "web":
                    type = "WEBRip";
                    break;
                default:
                    type = "BRRip";
                    break;
            }
            var quality = torrent.Value<string>("quality");
            var title = movie.Value<string>("title").Replace(":", "").Replace(' ', '.');
            var year = movie.Value<int>("year");
            release.Title = $"{title}.{year}.{quality}.{type}-YTS";

            var imdb = movie.Value<string>("imdb_code");
            release.Imdb = ParseUtil.GetImdbID(imdb);

            release.InfoHash = torrent.Value<string>("hash"); // magnet link is auto generated from infohash

            // ex: 2015-08-16 21:25:08 +0000
            var dateStr = torrent.Value<string>("date_uploaded");
            var dateTime = DateTime.ParseExact(dateStr, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            release.PublishDate = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc).ToLocalTime();

            release.Link = new Uri(torrent.Value<string>("url"));
            release.Seeders = torrent.Value<int>("seeds");
            release.Peers = torrent.Value<int>("peers") + release.Seeders;
            release.Size = torrent.Value<long>("size_bytes");
            release.DownloadVolumeFactor = 0;
            release.UploadVolumeFactor = 1;

            release.Details = new Uri(movie.Value<string>("url"));
            release.Poster = new Uri(movie.Value<string>("large_cover_image"));
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

            return release;
        }
    }
}
