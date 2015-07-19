using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Jackett.Indexers
{
    public class Strike : IndexerInterface
    {

        public event Action<IndexerInterface, JToken> OnSaveConfigurationRequested;
        public event Action<IndexerInterface, string, Exception> OnResultParsingError;

        public string DisplayName
        {
            get { return "Strike"; }
        }

        public string DisplayDescription
        {
            get { return "Torrent search engine"; }
        }

        public Uri SiteLink
        {
            get { return new Uri(DefaultUrl); }
        }

        public bool RequiresRageIDLookupDisabled { get { return true; } }

        public bool IsConfigured { get; private set; }

        const string DefaultUrl = "https://getstrike.net";


        //const string DownloadUrl = "/api/v2/torrents/download/?hash={0}";
        const string DownloadUrl = "/torrents/api/download/{0}.torrent";

        const string SearchUrl = "/api/v2/torrents/search/?category=TV&phrase={0}";
        string BaseUrl;

        CookieContainer cookies;
        HttpClientHandler handler;
        HttpClient client;

        public Strike()
        {
            IsConfigured = false;
            cookies = new CookieContainer();
            handler = new HttpClientHandler
            {
                CookieContainer = cookies,
                AllowAutoRedirect = true,
                UseCookies = true,
            };
            client = new HttpClient(handler);
        }

        public Task<ConfigurationData> GetConfigurationForSetup()
        {
            var config = new ConfigurationDataUrl(DefaultUrl);
            return Task.FromResult<ConfigurationData>(config);
        }

        public async Task ApplyConfiguration(JToken configJson)
        {
            var config = new ConfigurationDataUrl(DefaultUrl);
            config.LoadValuesFromJson(configJson);

            var formattedUrl = config.GetFormattedHostUrl();
            var releases = await PerformQuery(new TorznabQuery(), formattedUrl);
            if (releases.Length == 0)
                throw new Exception("Could not find releases from this URL");

            BaseUrl = formattedUrl;

            var configSaveData = new JObject();
            configSaveData["base_url"] = BaseUrl;

            if (OnSaveConfigurationRequested != null)
                OnSaveConfigurationRequested(this, configSaveData);

            IsConfigured = true;

        }

        public void LoadFromSavedConfiguration(JToken jsonConfig)
        {
            BaseUrl = (string)jsonConfig["base_url"];
            IsConfigured = true;
        }

        public async Task<ReleaseInfo[]> PerformQuery(TorznabQuery query, string baseUrl)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            var searchTerm = string.IsNullOrEmpty(query.SanitizedSearchTerm) ? "2015" : query.SanitizedSearchTerm;

            var searchString = searchTerm + " " + query.GetEpisodeSearchString();
            var episodeSearchUrl = baseUrl + string.Format(SearchUrl, HttpUtility.UrlEncode(searchString.Trim()));
            var results = await client.GetStringAsync(episodeSearchUrl);
            try
            {
                var jResults = JObject.Parse(results);
                foreach (JObject result in (JArray)jResults["torrents"])
                {
                    var release = new ReleaseInfo();

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;

                    release.Title = (string)result["torrent_title"];
                    release.Description = release.Title;
                    release.Seeders = (int)result["seeds"];
                    release.Peers = (int)result["leeches"] + release.Seeders;
                    release.Size = (long)result["size"];

                    // "Apr  2, 2015", "Apr 12, 2015" (note the spacing)
                    var dateString = string.Join(" ", ((string)result["upload_date"]).Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
                    release.PublishDate = DateTime.ParseExact(dateString, "MMM d, yyyy", CultureInfo.InvariantCulture);

                    release.Guid = new Uri((string)result["page"]);
                    release.Comments = release.Guid;

                    release.InfoHash = (string)result["torrent_hash"];
                    release.MagnetUri = new Uri((string)result["magnet_uri"]);
                    release.Link = new Uri(string.Format("{0}{1}", baseUrl, string.Format(DownloadUrl, release.InfoHash)));

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnResultParsingError(this, results, ex);
                throw ex;
            }


            return releases.ToArray();
        }

        public async Task<ReleaseInfo[]> PerformQuery(TorznabQuery query)
        {
            return await PerformQuery(query, BaseUrl);
        }

        public Task<byte[]> Download(Uri link)
        {
            throw new NotImplementedException();
        }


    }
}
