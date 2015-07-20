using CsQuery;
using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
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
        const string DefaultUrl = "http://torrentapi.org";
        const string TokenUrl = "/pubapi.php?get_token=get_token&format=json";
        const string SearchTVRageUrl = "/pubapi.php?mode=search&search_tvrage={0}&token={1}&format=json&min_seeders=1";
        const string SearchQueryUrl = "/pubapi.php?mode=search&search_string={0}&token={1}&format=json&min_seeders=1";
        private string BaseUrl;

        CookieContainer cookies;
        HttpClientHandler handler;
        HttpClient client;


        public Rarbg(IIndexerManagerService i, Logger l)
            : base(name: "RARBG",
                description: "RARBG",
                link: new Uri("https://rarbg.com"),
                caps: TorznabCapsUtil.CreateDefaultTorznabTVCaps(),
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
            var config = new ConfigurationDataUrl(DefaultUrl);
            return Task.FromResult<ConfigurationData>(config);
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

        public void LoadFromSavedConfiguration(JToken jsonConfig)
        {
            BaseUrl = (string)jsonConfig["base_url"];
            IsConfigured = true;
        }

        HttpRequestMessage CreateHttpRequest(string uri)
        {
            var message = new HttpRequestMessage();
            message.Method = HttpMethod.Get;
            message.RequestUri = new Uri(uri);
            message.Headers.UserAgent.ParseAdd(BrowserUtil.ChromeUserAgent);
            return message;
        }

        async Task<string> GetToken(string url)
        {
            var request = CreateHttpRequest(url + TokenUrl);
            var response = await client.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();
            JObject obj = JObject.Parse(result);
            return (string)obj["token"];
        }

        public async Task<ReleaseInfo[]> PerformQuery(TorznabQuery query)
        {
            return await PerformQuery(query, BaseUrl);
        }

        async Task<ReleaseInfo[]> PerformQuery(TorznabQuery query, string baseUrl)
        {

            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            string token = await GetToken(baseUrl);
            string searchUrl;
            if (query.RageID != 0)
                searchUrl = string.Format(baseUrl + SearchTVRageUrl, query.RageID, token);
            else
                searchUrl = string.Format(baseUrl + SearchQueryUrl, query.SanitizedSearchTerm, token);

            var request = CreateHttpRequest(searchUrl);
            var response = await client.SendAsync(request);
            var results = await response.Content.ReadAsStringAsync();
            try
            {
                var jItems = JArray.Parse(results);
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
                OnParseError(results, ex);
            }
            return releases.ToArray();
        }

        public Task<byte[]> Download(Uri link)
        {
            throw new NotImplementedException();
        }
    }
}
