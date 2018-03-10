using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
    public class Yify : BaseWebIndexer
    {
        public override string[] LegacySiteLinks { get; protected set; } = new string[] {
            "https://yts.me/",
            "https://yts.pe/",
        };

        private string ApiEndpoint { get { return SiteLink + "api/v2/list_movies.json"; } }

        private new ConfigurationData configData
        {
            get { return (ConfigurationData)base.configData; }
            set { base.configData = value; }
        }

        public Yify(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(name: "YIFY",
                description: "YIFY is a Public torrent site specialising in HD movies of small size",
                link: "https://yifymovie.co/",
                caps: new TorznabCapabilities(),
                configService: configService,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationData())
        {
            Encoding = Encoding.GetEncoding("windows-1252");
            Language = "en-us";
            Type = "public";

            TorznabCaps.SupportsImdbSearch = true;

            webclient.requestDelay = 2.5; // 0.5 requests per second (2 causes problems)

            AddCategoryMapping(1, TorznabCatType.MoviesHD, "Movies/x264/720");
            AddCategoryMapping(2, TorznabCatType.MoviesHD, "Movies/x264/1080");
            AddCategoryMapping(3, TorznabCatType.MoviesUHD, "Movies/x264/2160");
            AddCategoryMapping(4, TorznabCatType.Movies3D, "Movies/x264/3D");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);
            var releases = await PerformQuery(new TorznabQuery());

            await ConfigureIfOK(string.Empty, releases.Count() > 0, () =>
            {
                throw new Exception("Could not find releases from this URL");
            });

            return IndexerConfigurationStatus.Completed;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            return await PerformQuery(query, 0);
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query, int attempts)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();

            var queryCollection = new NameValueCollection();

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
            var response = await RequestStringWithCookiesAndRetry(searchUrl, string.Empty);

            try
            {
                var jsonContent = JObject.Parse(response.Content);

                string result = jsonContent.Value<string>("status");
                if (result != "ok") // query was not successful
                {
                    return releases.ToArray();
                }

                var data_items = jsonContent.Value<JToken>("data");
                int movie_count = data_items.Value<int>("movie_count");
                if (movie_count < 1) // no results found in query
                {
                    return releases.ToArray();
                }

                foreach (var movie_item in data_items.Value<JToken>("movies"))
                {
                    var torrents = movie_item.Value<JArray>("torrents");
                    if (torrents == null)
                        continue;
                    foreach (var torrent_info in torrents)
                    {
                        var release = new ReleaseInfo();

                        // Append the quality to the title because thats how radarr seems to be determining the quality?
                        // All releases are BRRips, see issue #2200
                        release.Title = movie_item.Value<string>("title_long") + " " + torrent_info.Value<string>("quality") + " BRRip";
                        var imdb = movie_item.Value<string>("imdb_code");
                        release.Imdb = ParseUtil.GetImdbID(imdb);

                        // API does not provide magnet link, so, construct it
                        string magnet_uri = "magnet:?xt=urn:btih:" + torrent_info.Value<string>("hash") +
                        "&dn=" + movie_item.Value<string>("slug") +
                        "&tr=udp://open.demonii.com:1337/announce" +
                        "&tr=udp://tracker.openbittorrent.com:80" +
                        "&tr=udp://tracker.coppersurfer.tk:6969" +
                        "&tr=udp://glotorrents.pw:6969/announce" +
                        "&tr=udp://tracker.opentrackr.org:1337/announce" +
                        "&tr=udp://torrent.gresille.org:80/announce" +
                        "&tr=udp://p4p.arenabg.com:1337" +
                        "&tr=udp://tracker.leechers-paradise.org:6969";

                        release.MagnetUri = new Uri(magnet_uri);
                        release.InfoHash = torrent_info.Value<string>("hash");

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

                        release.Comments = new Uri(movie_item.Value<string>("url"));
                        release.BannerUrl = new Uri(movie_item.Value<string>("medium_cover_image"));
                        release.Guid = release.Link;

                        // API doesn't provide Categories, resolve via quality
                        release.Category = MapTrackerCatToNewznab("1"); // default
                        if (torrent_info.Value<string>("quality") == "720p")
                        {
                            release.Category = MapTrackerCatToNewznab("1");
                        }
                        if (torrent_info.Value<string>("quality") == "1080p")
                        {
                            release.Category = MapTrackerCatToNewznab("2");
                        }
                        if (torrent_info.Value<string>("quality") == "2160p")
                        {
                            release.Category = MapTrackerCatToNewznab("3");
                        }
                        if (torrent_info.Value<string>("quality") == "3D")
                        {
                            release.Category = MapTrackerCatToNewznab("4");
                        }
                        releases.Add(release);
                    }
                }
            }
            catch (Exception ex)
            {
                OnParseError(response.Content, ex);
            }

            return releases;
        }
    }
}
