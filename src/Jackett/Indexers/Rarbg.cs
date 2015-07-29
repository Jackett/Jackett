using CsQuery;
using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Indexers
{
    public class Rarbg : BaseIndexer, IIndexer
    {
        private const string DefaultUrl = "http://torrentapi.org/";
        private const string TokenUrl = "pubapi.php?get_token=get_token&format=json";
        private const string SearchTVRageUrl = "pubapi.php?mode=search&search_tvrage={0}&token={1}&format=json&min_seeders=1";
        private const string SearchQueryUrl = "pubapi.php?mode=search&search_string={0}&token={1}&format=json&min_seeders=1";
        private string BaseUrl;

        public Rarbg(IIndexerManagerService i, Logger l, IWebClient wc)
            : base(name: "RARBG",
                description: "RARBG",
                link: "https://rarbg.com/",
                caps: TorznabCapsUtil.CreateDefaultTorznabTVCaps(),
                manager: i,
                client: wc,
                logger: l)
        {
        }

        public Task<ConfigurationData> GetConfigurationForSetup()
        {
            return Task.FromResult<ConfigurationData>(new ConfigurationDataUrl(DefaultUrl));
        }

        public async Task ApplyConfiguration(JToken configJson)
        {
            var config = new ConfigurationDataUrl(DefaultUrl);
            config.LoadValuesFromJson(configJson);

            var formattedUrl = config.GetFormattedHostUrl();
            var token = await GetToken(formattedUrl);
            /*var releases = await PerformQuery(new TorznabQuery(), formattedUrl);
            if (releases.Length == 0)
                throw new Exception("Could not find releases from this URL");*/

            BaseUrl = formattedUrl;

            var configSaveData = new JObject();
            configSaveData["base_url"] = BaseUrl;
            SaveConfig(configSaveData);
            IsConfigured = true;
        }

        public override void LoadFromSavedConfiguration(JToken jsonConfig)
        {
            BaseUrl = (string)jsonConfig["base_url"];
            IsConfigured = !string.IsNullOrEmpty(BaseUrl);
        }

        async Task<string> GetToken(string url)
        {
            var response = await RequestStringWithCookiesAndRetry(url + TokenUrl);
            JObject obj = JObject.Parse(response.Content);
            return (string)obj["token"];
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            return await PerformQuery(query, BaseUrl);
        }

        async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query, string baseUrl)
        {
            var releases = new List<ReleaseInfo>();
            string token = await GetToken(baseUrl);
            string searchUrl;
            if (query.RageID != 0)
                searchUrl = string.Format(baseUrl + SearchTVRageUrl, query.RageID, token);
            else
                searchUrl = string.Format(baseUrl + SearchQueryUrl, query.SanitizedSearchTerm, token);

            var results = await RequestStringWithCookiesAndRetry(searchUrl);
            try
            {
                var jItems = JArray.Parse(results.Content);
                foreach (JObject item in jItems)
                {
                    var release = new ReleaseInfo();
                    release.Title = (string)item["f"];
                    release.MagnetUri = new Uri((string)item["d"]);
                    release.Guid = release.MagnetUri;
                    release.PublishDate = new DateTime(1970, 1, 1);
                    release.Size = 0;
                    release.Seeders = 1;
                    release.Peers = 1;
                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;
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
