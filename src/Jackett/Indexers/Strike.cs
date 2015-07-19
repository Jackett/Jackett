using Jackett.Models;
using Jackett.Services;
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

namespace Jackett.Indexers
{
    public class Strike :  BaseIndexer, IIndexer
    {
        private readonly string DownloadUrl = "/torrents/api/download/{0}.torrent";
        private readonly string SearchUrl = "/api/v2/torrents/search/?category=TV&phrase={0}";
        private string BaseUrl;

        private CookieContainer cookies;
        private HttpClientHandler handler;
        private HttpClient client;

         public Strike(IIndexerManagerService i, Logger l) :
            base(name: "Strike",
          description: "Torrent search engine",
          link: new Uri("https://getstrike.net"),
          rageid: true,
          manager: i,
          logger: l)
        {
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
            var config = new ConfigurationDataUrl(SiteLink);
            return Task.FromResult<ConfigurationData>(config);
        }

        public async Task ApplyConfiguration(JToken configJson)
        {
            var config = new ConfigurationDataUrl(SiteLink);
            config.LoadValuesFromJson(configJson);

            var formattedUrl = config.GetFormattedHostUrl();
            var releases = await PerformQuery(new TorznabQuery(), formattedUrl);
            if (releases.Length == 0)
                throw new Exception("Could not find releases from this URL");

            BaseUrl = formattedUrl;

            var configSaveData = new JObject();
            configSaveData["base_url"] = BaseUrl;
            SaveConfig(configSaveData);
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
                OnParseError(results, ex);
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
