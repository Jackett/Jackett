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

namespace Jackett.Common.Indexers {
    public class Torrentscsv : BaseWebIndexer {
        public override string[] LegacySiteLinks { get; protected set; } = new string[] {
            "https://torrents-csv.ml/",
        };

        private string ApiEndpoint { get { return SiteLink + "service/search"; } }

        private new ConfigurationData configData {
            get { return (ConfigurationData) base.configData; }
            set { base.configData = value; }
        }

        public Torrentscsv (IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps) : base (
            name: "Torrents.csv",
            description: "Torrents.csv is a self-hostable, open source torrent search engine and database",
            link: "https://torrents-csv.ml/",
            caps : new TorznabCapabilities (),
            configService : configService,
            client : wc,
            logger : l,
            p : ps,
            configData : new ConfigurationData ()) {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "public";

            // Dummy mappings for sonarr, radarr, etc
            AddCategoryMapping (1, TorznabCatType.TV);
            AddCategoryMapping (2, TorznabCatType.Movies);
            AddCategoryMapping (3, TorznabCatType.Console);
            AddCategoryMapping (4, TorznabCatType.Audio);
            AddCategoryMapping (5, TorznabCatType.PC);
            AddCategoryMapping (6, TorznabCatType.XXX);
            AddCategoryMapping (7, TorznabCatType.Other);
            AddCategoryMapping (8, TorznabCatType.Books);

            TorznabCaps.SupportsImdbSearch = false;

            webclient.requestDelay = 1;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration (JToken configJson) {
            configData.LoadValuesFromJson (configJson);
            var releases = await PerformQuery (new TorznabQuery ());

            await ConfigureIfOK (string.Empty, releases.Count () > 0, () => {
                throw new Exception ("Error: No data returned!");
            });

            return IndexerConfigurationStatus.Completed;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery (TorznabQuery query) {
            return await PerformQuery (query, 0);
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery (TorznabQuery query, int attempts) {
            var releases = new List<ReleaseInfo> ();
            var searchString = query.GetQueryString ();

            var queryCollection = new NameValueCollection ();

            queryCollection.Add ("q", searchString);
            queryCollection.Add ("size", "100");
            queryCollection.Add ("type_", "torrent");

            var searchUrl = ApiEndpoint + "?" + queryCollection.GetQueryString ();
            var response = await RequestStringWithCookiesAndRetry (searchUrl, string.Empty);

            try {
                var jsonStart = response.Content;
                var jsonContent = JArray.Parse (jsonStart);

                foreach (var torrent in jsonContent) {
		    
		    if (torrent == null)
                        throw new Exception ("Error: No data returned!");

                    var release = new ReleaseInfo ();
                    release.Title = torrent.Value<string> ("name");

                    // construct magnet link from infohash with all public trackers known to man
                    string magnet_uri = "magnet:?xt=urn:btih:" + torrent.Value<JToken> ("infohash") +
                        "&tr=udp://tracker.opentrackr.org:1337/announce" +
                        "&tr=udp://tracker.leechers-paradise.org:6969" +
                        "&tr=udp://tracker.coppersurfer.tk:6969/announce" +
                        "&tr=udp://tracker1.itzmx.com:8080/announce" +
                        "&tr=udp://explodie.org:6969/announce" +
                        "&tr=udp://9.rarbg.to:2710/announce" +
                        "&tr=udp://exodus.desync.com:6969/announce" +
                        "&tr=udp://tracker.openbittorrent.com:80" +
                        "&tr=udp://torrent.gresille.org:80/announce" +
                        "&tr=udp://glotorrents.pw:6969/announce" +
                        "&tr=http://tracker3.itzmx.com:6961/announce" +
                        "&tr=udp://tracker.internetwarriors.net:1337/announce" +
                        "&tr=udp://open.demonii.com:1337/announce" +
                        "&tr=udp://p4p.arenabg.com:1337";

                    release.MagnetUri = new Uri (magnet_uri);
                    release.InfoHash = torrent.Value<JToken> ("infohash").ToString ();

                    // convert unix timestamp to human readable date
                    double createdunix = torrent.Value<int> ("created_unix");
                    System.DateTime dateTime = new System.DateTime (1970, 1, 1, 0, 0, 0, 0);
                    dateTime = dateTime.AddSeconds (createdunix);

                    release.PublishDate = dateTime;
                    release.Seeders = torrent.Value<int> ("seeders");
                    release.Peers = torrent.Value<int> ("leechers") + release.Seeders;
                    release.Size = torrent.Value<long> ("size_bytes");
                    release.DownloadVolumeFactor = 0;
                    release.UploadVolumeFactor = 1;

                    releases.Add (release);
                }

            } catch (Exception ex) {
                OnParseError (response.Content, ex);
            }

            return releases;
        }
    }
}
