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
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class TorrentsCSV : BaseWebIndexer
    {
        private string ApiEndpoint => SiteLink + "service/search";

        private new ConfigurationData configData => base.configData;

        public TorrentsCSV(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base("Torrents.csv",
                   description: "Torrents.csv is a self-hostable, open source torrent search engine and database",
                   link: "https://torrents-csv.ml/",
                   caps: new TorznabCapabilities
                   {
                       SupportsImdbMovieSearch = true
                   },
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   configData: new ConfigurationData())
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "public";

            // torrents.csv doesn't return categories
            AddCategoryMapping(1, TorznabCatType.Other);
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);
            var releases = await PerformQuery(new TorznabQuery());

            await ConfigureIfOK(string.Empty, releases.Any(),
                                () => throw new Exception("Error: 0 results found!"));

            return IndexerConfigurationStatus.Completed;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query) {
            var releases = new List<ReleaseInfo>();

            var searchString = query.GetQueryString();
            if (!string.IsNullOrWhiteSpace(searchString) && searchString.Length < 3)
                return releases; // search needs at least 3 characters
            if (string.IsNullOrEmpty(searchString))
                searchString = DateTime.Now.Year.ToString();

            var qc = new NameValueCollection
            {
                { "q", searchString },
                { "size", "100" },
                { "type_", "torrent" }
            };

            var searchUrl = ApiEndpoint + "?" + qc.GetQueryString();
            var response = await RequestStringWithCookiesAndRetry(searchUrl);

            try
            {
                var jsonStart = response.Content;
                var jsonContent = JArray.Parse(jsonStart);

                foreach (var torrent in jsonContent)
                {
                    if (torrent == null)
                        throw new Exception("Error: No data returned!");

                    var title = torrent.Value<string>("name");
                    var size = torrent.Value<long>("size_bytes");
                    var seeders = torrent.Value<int>("seeders");
                    var leechers = torrent.Value<int>("leechers");
                    var grabs = ParseUtil.CoerceInt(torrent.Value<string>("completed") ?? "0");
                    var infohash = torrent.Value<JToken>("infohash").ToString();

                    // convert unix timestamp to human readable date
                    var publishDate = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                    publishDate = publishDate.AddSeconds(torrent.Value<long>("created_unix"));

                    // construct magnet link from infohash with public trackers
                    // TODO move trackers to List for reuse elsewhere
                    // TODO dynamically generate list periodically from online tracker repositories like
                    // https://github.com/ngosang/trackerslist
                    var magnet = new Uri("magnet:?xt=urn:btih:" + infohash +
                        "&tr=udp://tracker.coppersurfer.tk:6969/announce" +
                        "&tr=udp://tracker.leechers-paradise.org:6969/announce" +
                        "&tr=udp://tracker.internetwarriors.net:1337/announce" +
                        "&tr=udp://tracker.opentrackr.org:1337/announce" +
                        "&tr=udp://9.rarbg.to:2710/announce" +
                        "&tr=udp://exodus.desync.com:6969/announce" +
                        "&tr=udp://explodie.org:6969/announce" +
                        "&tr=udp://tracker2.itzmx.com:6961/announce" +
                        "&tr=udp://tracker1.itzmx.com:8080/announce" +
                        "&tr=udp://tracker.torrent.eu.org:451/announce" +
                        "&tr=udp://tracker.tiny-vps.com:6969/announce" +
                        "&tr=udp://tracker.port443.xyz:6969/announce" +
                        "&tr=udp://thetracker.org:80/announce" +
                        "&tr=udp://open.stealth.si:80/announce" +
                        "&tr=udp://open.demonii.si:1337/announce" +
                        "&tr=udp://ipv4.tracker.harry.lu:80/announce" +
                        "&tr=udp://denis.stalker.upeer.me:6969/announce" +
                        "&tr=udp://tracker1.wasabii.com.tw:6969/announce" +
                        "&tr=udp://tracker.dler.org:6969/announce" +
                        "&tr=udp://tracker.cyberia.is:6969/announce" +
                        "&tr=udp://tracker4.itzmx.com:2710/announce" +
                        "&tr=udp://tracker.uw0.xyz:6969/announce" +
                        "&tr=udp://tracker.moeking.me:6969/announce" +
                        "&tr=udp://retracker.lanta-net.ru:2710/announce" +
                        "&tr=udp://tracker.nyaa.uk:6969/announce" +
                        "&tr=udp://tracker.novg.net:6969/announce" +
                        "&tr=udp://tracker.iamhansen.xyz:2000/announce" +
                        "&tr=udp://tracker.filepit.to:6969/announce" +
                        "&tr=udp://tracker.dyn.im:6969/announce" +
                        "&tr=udp://torrentclub.tech:6969/announce" +
                        "&tr=udp://tracker.tvunderground.org.ru:3218/announce" +
                        "&tr=udp://tracker.open-tracker.org:1337/announce" +
                        "&tr=udp://tracker.justseed.it:1337/announce");

                    var release = new ReleaseInfo
                    {
                        Title = title,
                        Comments = new Uri(SiteLink), // there is no comments or details link
                        Guid = magnet,
                        MagnetUri = magnet,
                        InfoHash = infohash,
                        Category = new List<int> { TorznabCatType.Other.ID },
                        PublishDate = publishDate,
                        Size = size,
                        Grabs = grabs,
                        Seeders = seeders,
                        Peers = leechers + seeders,
                        DownloadVolumeFactor = 0,
                        UploadVolumeFactor = 1,
                        MinimumRatio = 1,
                        MinimumSeedTime = 172800, // 48 hours
                    };

                    releases.Add(release);
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
