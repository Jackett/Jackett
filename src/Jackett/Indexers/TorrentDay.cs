using CsQuery;
using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
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
    public class TorrentDay : BaseIndexer, IIndexer
    {
        private readonly string StartPageUrl = "";
        private readonly string LoginUrl = "";
        private readonly string SearchUrl = "";

        CookieContainer cookies;
        HttpClientHandler handler;
        HttpClient client;

        public TorrentDay(IIndexerManagerService i, Logger l)
            : base(name: "TorrentDay",
                description: "TorrentDay",
                link: new Uri("https://torrentday.eu"),
                caps: TorznabCapsUtil.CreateDefaultTorznabTVCaps(),
                manager: i,
                logger: l)
        {
            StartPageUrl = SiteLink + "login.php";
            LoginUrl = SiteLink + "tak3login.php";
            SearchUrl = SiteLink + "browse.php?search={0}&cata=yes&c2=1&c7=1&c14=1&c24=1&c26=1&c31=1&c32=1&c33=1";

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

        HttpRequestMessage CreateHttpRequest(string uri)
        {
            var message = new HttpRequestMessage();
            message.Method = HttpMethod.Get;
            message.RequestUri = new Uri(uri);
            message.Headers.UserAgent.ParseAdd(BrowserUtil.ChromeUserAgent);
            return message;
        }

        public async Task ApplyConfiguration(JToken configJson)
        {
            var config = new ConfigurationDataBasicLogin();
            config.LoadValuesFromJson(configJson);

            var startMessage = CreateHttpRequest(StartPageUrl);
            var results = await (await client.SendAsync(startMessage)).Content.ReadAsStringAsync();


            var pairs = new Dictionary<string, string> {
				{ "username", config.Username.Value },
				{ "password", config.Password.Value }
			};
            var content = new FormUrlEncodedContent(pairs);
            var loginRequest = CreateHttpRequest(LoginUrl);
            loginRequest.Method = HttpMethod.Post;
            loginRequest.Content = content;
            loginRequest.Headers.Referrer = new Uri(StartPageUrl);

            var response = await client.SendAsync(loginRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!responseContent.Contains("logout.php"))
            {
                CQ dom = responseContent;
                var messageEl = dom["#login"];
                messageEl.Children("form").Remove();
                var errorMessage = messageEl.Text().Trim();
                throw new ExceptionWithConfigData(errorMessage, (ConfigurationData)config);
            }
            else
            {
                var configSaveData = new JObject();
                cookies.DumpToJson(SiteLink, configSaveData);
                SaveConfig(configSaveData);
                IsConfigured = true;
            }
        }

        public void LoadFromSavedConfiguration(JToken jsonConfig)
        {
            cookies.FillFromJson(SiteLink, jsonConfig, logger);
            IsConfigured = true;
        }

        public async Task<ReleaseInfo[]> PerformQuery(TorznabQuery query)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            var searchString = query.SanitizedSearchTerm + " " + query.GetEpisodeSearchString();
            var episodeSearchUrl = string.Format(SearchUrl, HttpUtility.UrlEncode(searchString));
            var results = await client.GetStringAsync(episodeSearchUrl);
            try
            {
                CQ dom = results;
                var rows = dom["#torrentTable > tbody > tr.browse"];
                foreach (var row in rows)
                {
                    CQ qRow = row.Cq();
                    var release = new ReleaseInfo();

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;
                    release.Title = qRow.Find(".torrentName").Text();
                    release.Description = release.Title;
                    release.Guid = new Uri(SiteLink + "/" + qRow.Find(".torrentName").Attr("href"));
                    release.Comments = release.Guid;
                    release.Link = new Uri(SiteLink + "/" + qRow.Find(".dlLinksInfo > a").Attr("href"));

                    var sizeStr = qRow.Find(".sizeInfo").Text().Trim();
                    var sizeParts = sizeStr.Split(' ');
                    release.Size = ReleaseInfo.GetBytes(sizeParts[1], ParseUtil.CoerceFloat(sizeParts[0]));

                    var dateStr = qRow.Find(".ulInfo").Text().Split('|').Last().Trim();
                    release.PublishDate = DateTimeUtil.FromTimeAgo(dateStr);

                    release.Seeders = ParseUtil.CoerceInt(qRow.Find(".seedersInfo").Text());
                    release.Peers = ParseUtil.CoerceInt(qRow.Find(".leechersInfo").Text()) + release.Seeders;

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
            return client.GetByteArrayAsync(link);
        }
    }
}
