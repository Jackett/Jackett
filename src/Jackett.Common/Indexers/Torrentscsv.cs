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
    public class Torrentscsv : BaseWebIndexer
    {

        private string ApiEndpoint => SiteLink + "service/search";

        private new ConfigurationData configData
        {
            get => base.configData;
            set => base.configData = value;
        }

        public Torrentscsv(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps) : base(
            name: "Torrents.csv",
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

            // dummy mappings for sonarr, radarr, etc since torrents.csv doesnt return categories
            AddCategoryMapping(1000, TorznabCatType.Console);
            AddCategoryMapping(2000, TorznabCatType.Movies);
            AddCategoryMapping(3000, TorznabCatType.Audio);
            AddCategoryMapping(4000, TorznabCatType.PC);
            AddCategoryMapping(5000, TorznabCatType.TV);
            AddCategoryMapping(6000, TorznabCatType.XXX);
            AddCategoryMapping(7000, TorznabCatType.Other);
            AddCategoryMapping(8000, TorznabCatType.Books);
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);
            var releases = await PerformQuery(new TorznabQuery());

            await ConfigureIfOK(string.Empty, releases.Count() > 0, () =>
            {
                throw new Exception("Error: No data returned!");
            });

            return IndexerConfigurationStatus.Completed;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query) => await PerformQuery(query, 0);

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query, int attempts)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();
            if (string.IsNullOrEmpty(searchString))
                searchString = "2020";

            var queryCollection = new NameValueCollection
            {
                { "q", searchString },
                { "size", "500" },
                { "type_", "torrent" }
            };

            var searchUrl = ApiEndpoint + "?" + queryCollection.GetQueryString();
            var response = await RequestStringWithCookiesAndRetry(searchUrl, string.Empty);

            try
            {
                var jsonStart = response.Content;
                var jsonContent = JArray.Parse(jsonStart);

                foreach (var torrent in jsonContent)
                {

                    if (torrent == null)
                        throw new Exception("Error: No data returned!");

                    // construct magnet link from infohash with all public trackers known to man
                    // TODO move trackers to List for reuse elsewhere
                    // TODO dynamically generate list periodically from online tracker repositories like
                    // https://torrents.io/tracker-list/
                    // https://github.com/ngosang/trackerslist
                    var magnet = new Uri("magnet:?xt=urn:btih:" + torrent.Value<JToken>("infohash") +
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

                    // convert unix timestamp to human readable date
                    double createdunix = torrent.Value<long>("created_unix");
                    var dateTime = new System.DateTime(1970, 1, 1, 0, 0, 0, 0);
                    dateTime = dateTime.AddSeconds(createdunix);
                    var seeders = torrent.Value<int>("seeders");
                    var grabs = ParseUtil.CoerceInt(torrent.Value<string>("completed") ?? "0");
                    var release = new ReleaseInfo
                    {
                        Title = torrent.Value<string>("name"),
                        MagnetUri = magnet,
                        // there is no comments or details link so we point to the web site instead
                        Comments = new Uri(SiteLink),
                        Guid = magnet,
                        Link = magnet,
                        InfoHash = torrent.Value<JToken>("infohash").ToString(),
                        PublishDate = dateTime,
                        Seeders = seeders,
                        Peers = torrent.Value<int>("leechers") + seeders,
                        Size = torrent.Value<long>("size_bytes"),
                        Grabs = grabs,
                        MinimumRatio = 1,
                        MinimumSeedTime = 172800, // 48 hours
                        DownloadVolumeFactor = 0,
                        UploadVolumeFactor = 1
                    };

                    //TODO isn't there already a function for this?
                    // dummy mappings for sonarr, radarr, etc
                    var categories = string.Join(";", MapTorznabCapsToTrackers(query));
                    if (!string.IsNullOrEmpty(categories))
                    {
                        if (categories.Contains("1000"))
                        {
                            release.Category = new List<int> { TorznabCatType.Console.ID };
                        }
                        if (categories.Contains("2000"))
                        {
                            release.Category = new List<int> { TorznabCatType.Movies.ID };
                        }
                        if (categories.Contains("3000"))
                        {
                            release.Category = new List<int> { TorznabCatType.Audio.ID };
                        }
                        if (categories.Contains("4000"))
                        {
                            release.Category = new List<int> { TorznabCatType.PC.ID };
                        }
                        if (categories.Contains("5000"))
                        {
                            release.Category = new List<int> { TorznabCatType.TV.ID };
                        }
                        if (categories.Contains("6000"))
                        {
                            release.Category = new List<int> { TorznabCatType.XXX.ID };
                        }
                        if (categories.Contains("7000"))
                        {
                            release.Category = new List<int> { TorznabCatType.Other.ID };
                        }
                        if (categories.Contains("8000"))
                        {
                            release.Category = new List<int> { TorznabCatType.Books.ID };
                        }
                    }
                    // for null category
                    else
                    {
                        release.Category = new List<int> { TorznabCatType.Other.ID };
                    }
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
