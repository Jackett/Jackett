using CsQuery;
using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Indexers
{
    class SceneAccess : BaseIndexer, IIndexer
    {
        private readonly string LoginUrl = "";
        private readonly string SearchUrl = "";

        CookieContainer cookies;
        HttpClientHandler handler;
        HttpClient client;
        string cookieHeader;

        public SceneAccess(IIndexerManagerService i, Logger l)
            : base(name: "SceneAccess",
                description: "Your gateway to the scene",
                link: new Uri("https://sceneaccess.eu"),
                caps: TorznabCapsUtil.CreateDefaultTorznabTVCaps(),
                manager: i,
                logger: l)
        {
            LoginUrl = SiteLink + "/login";
            SearchUrl = SiteLink + "/{0}?method=1&c{1}=1&search={2}";

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
            var config = new ConfigurationDataBasicLogin();
            return Task.FromResult<ConfigurationData>(config);
        }

        public async Task ApplyConfiguration(JToken configJson)
        {
            var config = new ConfigurationDataBasicLogin();
            config.LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string> {
                { "username", config.Username.Value },
                { "password", config.Password.Value },
                { "submit", "come on in" }
            };

            var content = new FormUrlEncodedContent(pairs);

            string responseContent;
            var configSaveData = new JObject();

            if (Engine.IsWindows)
            {
                // If Windows use .net http
                var response = await client.PostAsync(LoginUrl, content);
                responseContent = await response.Content.ReadAsStringAsync();
                cookies.DumpToJson(SiteLink, configSaveData);
            }
            else
            {
                // If UNIX system use curl
                var response = await CurlHelper.PostAsync(LoginUrl, pairs);
                responseContent = Encoding.UTF8.GetString(response.Content);
                cookieHeader = response.CookieHeader;
                configSaveData["cookie_header"] = cookieHeader;
            }

            if (!responseContent.Contains("nav_profile"))
            {
                CQ dom = responseContent;
                var messageEl = dom["#login_box_desc"];
                var errorMessage = messageEl.Text().Trim();
                throw new ExceptionWithConfigData(errorMessage, (ConfigurationData)config);
            }
            else
            {
                SaveConfig(configSaveData);
                IsConfigured = true;
            }
        }

        public void LoadFromSavedConfiguration(JToken jsonConfig)
        {
            cookies.FillFromJson(SiteLink, jsonConfig, logger);
            cookieHeader = cookies.GetCookieHeader(SiteLink);
            IsConfigured = true;
        }

        public async Task<ReleaseInfo[]> PerformQuery(TorznabQuery query)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            var searchString = query.SanitizedSearchTerm + " " + query.GetEpisodeSearchString();
            var searchSection = string.IsNullOrEmpty(query.Episode) ? "archive" : "browse";
            var searchCategory = string.IsNullOrEmpty(query.Episode) ? "26" : "27";

            var searchUrl = string.Format(SearchUrl, searchSection, searchCategory, searchString);

            string results;
            if (Engine.IsWindows)
            {
                results = await client.GetStringAsync(searchUrl);
            }
            else
            {
                var response = await CurlHelper.GetAsync(searchUrl, cookieHeader);
                results = Encoding.UTF8.GetString(response.Content);
            }

            try
            {
                CQ dom = results;
                var rows = dom["#torrents-table > tbody > tr.tt_row"];
                foreach (var row in rows)
                {
                    CQ qRow = row.Cq();
                    var release = new ReleaseInfo();

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 129600;
                    release.Title = qRow.Find(".ttr_name > a").Text();
                    release.Description = release.Title;
                    release.Guid = new Uri(SiteLink + "/" + qRow.Find(".ttr_name > a").Attr("href"));
                    release.Comments = release.Guid;
                    release.Link = new Uri(SiteLink + "/" + qRow.Find(".td_dl > a").Attr("href"));

                    var sizeStr = qRow.Find(".ttr_size").Contents()[0].NodeValue;
                    var sizeParts = sizeStr.Split(' ');
                    release.Size = ReleaseInfo.GetBytes(sizeParts[1], ParseUtil.CoerceFloat(sizeParts[0]));

                    var timeStr = qRow.Find(".ttr_added").Text();
                    DateTime time;
                    if (DateTime.TryParseExact(timeStr, "yyyy-MM-ddHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out time))
                    {
                        release.PublishDate = time;
                    }

                    release.Seeders = ParseUtil.CoerceInt(qRow.Find(".ttr_seeders").Text());
                    release.Peers = ParseUtil.CoerceInt(qRow.Find(".ttr_leechers").Text()) + release.Seeders;

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(results, ex);
            }

            return releases.ToArray();
        }

        public async Task<byte[]> Download(Uri link)
        {
            if (Engine.IsWindows)
            {
                return await client.GetByteArrayAsync(link);
            }
            else
            {
                var response = await CurlHelper.GetAsync(link.ToString(), cookieHeader);
                return response.Content;
            }
        }
    }
}
