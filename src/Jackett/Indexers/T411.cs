using Jackett.Models;
using Jackett.Utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Jackett.Indexers
{
    public class T411 : IIndexer
    {

        public event Action<IIndexer, JToken> OnSaveConfigurationRequested;

        public event Action<IIndexer, string, Exception> OnResultParsingError;

        public string DisplayName
        {
            get { return "T411"; }
        }

        public string DisplayDescription
        {
            get { return "French Torrent Tracker"; }
        }

        public Uri SiteLink
        {
            get { return new Uri(BaseUrl); }
        }

        public bool IsConfigured { get; private set; }

        const string BaseUrl = "http://www.t411.io";
        const string CommentsUrl = BaseUrl + "/torrents/{0}";

        const string ApiUrl = "http://api.t411.io";
        const string AuthUrl = ApiUrl + "/auth";
        const string SearchUrl = ApiUrl + "/torrents/search/{0}";
        const string DownloadUrl = ApiUrl + "/torrents/download/{0}";

        HttpClientHandler handler;
        HttpClient client;

        string username = string.Empty;
        string password = string.Empty;
        string token = string.Empty;
        DateTime lastTokenFetch = DateTime.MinValue;

        public T411()
        {
            IsConfigured = false;
            handler = new HttpClientHandler
            {
                AllowAutoRedirect = true
            };
            client = new HttpClient(handler);
        }

        public Task<ConfigurationData> GetConfigurationForSetup()
        {
            var config = new ConfigurationDataBasicLogin();
            return Task.FromResult<ConfigurationData>(config);
        }

        async Task<string> GetAuthToken(bool forceFetch = false)
        {
            if (!forceFetch && lastTokenFetch > DateTime.Now - TimeSpan.FromHours(48))
            {
                return token;
            }

            var pairs = new Dictionary<string, string> {
				{ "username", username },
				{ "password", password }
			};

            var content = new FormUrlEncodedContent(pairs);

            var response = await client.PostAsync(AuthUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonResponse = JObject.Parse(responseContent);
            if (jsonResponse["error"] != null)
            {
                throw new ApplicationException((string)jsonResponse["error"]);
            }
            token = (string)jsonResponse["token"];
            lastTokenFetch = DateTime.Now;
            return token;
        }

        public async Task ApplyConfiguration(JToken configJson)
        {
            var config = new ConfigurationDataBasicLogin();
            config.LoadValuesFromJson(configJson);

            username = config.Username.Value;
            password = config.Password.Value;

            try
            {
                await GetAuthToken(true);
            }
            catch (Exception ex)
            {
                throw new ExceptionWithConfigData(ex.Message, (ConfigurationData)config);
            }

            var configSaveData = new JObject();
            configSaveData["username"] = username;
            configSaveData["password"] = password;
            configSaveData["token"] = token;
            configSaveData["last_token_fetch"] = lastTokenFetch;

            if (OnSaveConfigurationRequested != null)
                OnSaveConfigurationRequested(this, configSaveData);

            IsConfigured = true;
        }

        public void LoadFromSavedConfiguration(JToken jsonConfig)
        {
            username = (string)jsonConfig["username"];
            password = (string)jsonConfig["password"];
            token = (string)jsonConfig["token"];
            lastTokenFetch = (DateTime)jsonConfig["last_token_fetch"];
            IsConfigured = true;
        }

        public async Task<ReleaseInfo[]> PerformQuery(TorznabQuery query)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            foreach (var title in query.ShowTitles ?? new string[] { "%20" })
            {
                var searchString = title + " " + query.GetEpisodeSearchString();
                var episodeSearchUrl = string.Format(SearchUrl, HttpUtility.UrlEncode(searchString));

                var message = new HttpRequestMessage();
                message.Method = HttpMethod.Get;
                message.RequestUri = new Uri(episodeSearchUrl);
                message.Headers.TryAddWithoutValidation("Authorization", await GetAuthToken());

                var response = await client.SendAsync(message);
                var results = await response.Content.ReadAsStringAsync();

                var jsonResult = JObject.Parse(results);
                try
                {
                    var items = (JArray)jsonResult["torrents"];
                    foreach (var item in items)
                    {
                        var release = new ReleaseInfo();

                        release.MinimumRatio = 1;
                        release.MinimumSeedTime = 172800;
                        var torrentId = (string)item["id"];
                        release.Link = new Uri(string.Format(DownloadUrl, torrentId));
                        release.Title = (string)item["name"];
                        release.Description = release.Title;
                        release.Comments = new Uri(string.Format(CommentsUrl, (string)item["rewritename"]));
                        release.Guid = release.Comments;

                        var dateUtc = DateTime.ParseExact((string)item["added"], "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                        release.PublishDate = DateTime.SpecifyKind(dateUtc, DateTimeKind.Utc).ToLocalTime();

                        release.Seeders = ParseUtil.CoerceInt((string)item["seeders"]);
                        release.Peers = ParseUtil.CoerceInt((string)item["leechers"]) + release.Seeders;

                        release.Size = ParseUtil.CoerceLong((string)item["size"]);

                        releases.Add(release);
                    }
                }
                catch (Exception ex)
                {
                    OnResultParsingError(this, results, ex);
                    throw ex;
                }
            }
            return releases.ToArray();
        }

        public async Task<byte[]> Download(Uri link)
        {
            var message = new HttpRequestMessage();
            message.Method = HttpMethod.Get;
            message.RequestUri = link;
            message.Headers.TryAddWithoutValidation("Authorization", await GetAuthToken());

            var response = await client.SendAsync(message);
            return await response.Content.ReadAsByteArrayAsync();
        }
    }
}
