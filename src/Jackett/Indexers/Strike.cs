using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Jackett.Models.IndexerConfig;
using System.Collections.Specialized;
using Jackett.Models.IndexerConfig.Bespoke;

namespace Jackett.Indexers
{
    public class Strike : BaseIndexer, IIndexer
    {
        readonly static string defaultSiteLink = "https://getstrike.net/";

        private Uri BaseUri
        {
            get { return new Uri(configData.Url.Value); }
            set { configData.Url.Value = value.ToString(); }
        }

        private string SearchUrl { get { return BaseUri + "api/v2/torrents/search/?phrase={0}"; } }
        private string DownloadUrl { get { return BaseUri + "torrents/api/download/{0}.torrent"; } }

        new ConfigurationDataStrike configData
        {
            get { return (ConfigurationDataStrike)base.configData; }
            set { base.configData = value; }
        }


        public Strike(IIndexerManagerService i, Logger l, IWebClient wc, IProtectionService ps)
            : base(name: "Strike",
                description: "Torrent search engine",
                link: defaultSiteLink,
                caps: new TorznabCapabilities(),
                manager: i,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationDataStrike(defaultSiteLink))
        {
            AddCategoryMapping("Anime", TorznabCatType.TVAnime);
            AddCategoryMapping("Applications", TorznabCatType.PC);
            AddCategoryMapping("Books", TorznabCatType.Books);
            AddCategoryMapping("Games", TorznabCatType.PCGames);
            AddCategoryMapping("Movies", TorznabCatType.Movies);
            AddCategoryMapping("TV", TorznabCatType.TV);
            AddCategoryMapping("XXX", TorznabCatType.XXX);
            AddCategoryMapping("Music", TorznabCatType.Audio);

            /*AddCategoryMapping("Movies:Highres Movies", TorznabCatType.MoviesHD);
            AddCategoryMapping("Movies:3D Movies", TorznabCatType.Movies3D);
            AddCategoryMapping("Books:Ebooks", TorznabCatType.BooksEbook);
            AddCategoryMapping("Books:Comics", TorznabCatType.BooksComics);
            AddCategoryMapping("Books:Audio Books", TorznabCatType.AudioAudiobook);
            AddCategoryMapping("Games:XBOX360", TorznabCatType.ConsoleXbox360);
            AddCategoryMapping("Games:Wii", TorznabCatType.ConsoleWii);
            AddCategoryMapping("Games:PSP", TorznabCatType.ConsolePSP);
            AddCategoryMapping("Games:PS3", TorznabCatType.ConsolePS3);
            AddCategoryMapping("Games:PC", TorznabCatType.PCGames);
            AddCategoryMapping("Games:Android", TorznabCatType.PCPhoneAndroid);
            AddCategoryMapping("Music:Mp3", TorznabCatType.AudioMP3);*/
        }

        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);
            var releases = await PerformQuery(new TorznabQuery());

            await ConfigureIfOK(string.Empty, releases.Count() > 0, () =>
            {
                throw new Exception("Could not find releases from this URL");
            });

            return IndexerConfigurationStatus.Completed;
        }

        // Override to load legacy config format
        public override void LoadFromSavedConfiguration(JToken jsonConfig)
        {
            if (jsonConfig is JObject)
            {
                BaseUri = new Uri(jsonConfig.Value<string>("base_url"));
                SaveConfig();
                IsConfigured = true;
                return;
            }

            base.LoadFromSavedConfiguration(jsonConfig);
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();
            var queryString = query.GetQueryString();
            var searchTerm = string.IsNullOrEmpty(queryString) ? DateTime.Now.Year.ToString() : queryString;
            var episodeSearchUrl = string.Format(SearchUrl, HttpUtility.UrlEncode(searchTerm));

            var trackerCategories = MapTorznabCapsToTrackers(query, mapChildrenCatsToParent: true);

            // This tracker can only search one cat at a time, otherwise search all and filter results
            if (trackerCategories.Count == 1)
            {
                episodeSearchUrl += "&category=" + trackerCategories[0];
            }

            var results = await RequestStringWithCookiesAndRetry(episodeSearchUrl, string.Empty);
            try
            {
                var jResults = JObject.Parse(results.Content);
                foreach (JObject result in (JArray)jResults["torrents"])
                {
                    var release = new ReleaseInfo();

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;

                    if (trackerCategories.Count > 0 && !trackerCategories.Contains((string)result["torrent_category"]))
                    {
                        continue;
                    }
                    release.Category = MapTrackerCatToNewznab((string)result["torrent_category"]);

                    release.Title = (string)result["torrent_title"];
                    release.Description = release.Title;
                    release.Seeders = (int)result["seeds"];
                    release.Peers = (int)result["leeches"] + release.Seeders;
                    release.Size = (long)result["size"];

                    // "Apr  2, 2015", "Apr 12, 2015" (note the spacing)
                    // some are unix timestamps, some are not.. :/
                    var dateString = string.Join(" ", ((string)result["upload_date"]).Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
                    float dateVal;
                    if (ParseUtil.TryCoerceFloat(dateString, out dateVal))
                        release.PublishDate = DateTimeUtil.UnixTimestampToDateTime(dateVal);
                    else
                        release.PublishDate = DateTime.ParseExact(dateString, "MMM d, yyyy", CultureInfo.InvariantCulture);

                    release.Guid = new Uri((string)result["page"]);
                    release.Comments = release.Guid;

                    release.InfoHash = (string)result["torrent_hash"];
                    release.MagnetUri = new Uri((string)result["magnet_uri"]);
                    release.Link = new Uri(string.Format(DownloadUrl, release.InfoHash));

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(results.Content, ex);
            }

            return releases;
        }

        public override Task<byte[]> Download(Uri link)
        {
            throw new NotImplementedException();
        }
    }
}
