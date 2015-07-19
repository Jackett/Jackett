using CsQuery;
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
    public class TorrentDay : IndexerInterface
    {
        public event Action<IndexerInterface, JToken> OnSaveConfigurationRequested;

        public event Action<IndexerInterface, string, Exception> OnResultParsingError;

        public string DisplayName
        {
            get { return "TorrentDay"; }
        }

        public string DisplayDescription
        {
            get { return DisplayName; }
        }

        public Uri SiteLink
        {
            get { return new Uri(BaseUrl); }
        }

        public bool IsConfigured { get; private set; }

        const string BaseUrl = "https://torrentday.eu";
        const string StartPageUrl = BaseUrl + "/login.php";
        const string LoginUrl = BaseUrl + "/tak3login.php";
        const string SearchUrl = BaseUrl + "/browse.php?search={0}&cata=yes&c2=1&c7=1&c14=1&c24=1&c26=1&c31=1&c32=1&c33=1";

        static string chromeUserAgent = BrowserUtil.ChromeUserAgent;

        CookieContainer cookies;
        HttpClientHandler handler;
        HttpClient client;

        public TorrentDay()
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
            var config = new ConfigurationDataBasicLogin();
            return Task.FromResult<ConfigurationData>(config);
        }

        HttpRequestMessage CreateHttpRequest(string uri)
        {
            var message = new HttpRequestMessage();
            message.Method = HttpMethod.Get;
            message.RequestUri = new Uri(uri);
            message.Headers.UserAgent.ParseAdd(chromeUserAgent);
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

                if (OnSaveConfigurationRequested != null)
                    OnSaveConfigurationRequested(this, configSaveData);

                IsConfigured = true;
            }
        }

        public void LoadFromSavedConfiguration(JToken jsonConfig)
        {
            cookies.FillFromJson(new Uri(BaseUrl), jsonConfig);
            IsConfigured = true;
        }

        public async Task<ReleaseInfo[]> PerformQuery(TorznabQuery query)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            foreach (var title in query.ShowTitles ?? new string[] { string.Empty })
            {
                var searchString = title + " " + query.GetEpisodeSearchString();
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
                        release.Guid = new Uri(BaseUrl + "/" + qRow.Find(".torrentName").Attr("href"));
                        release.Comments = release.Guid;
                        release.Link = new Uri(BaseUrl + "/" + qRow.Find(".dlLinksInfo > a").Attr("href"));

                        var sizeStr = qRow.Find(".sizeInfo").Text().Trim();
                        var sizeParts = sizeStr.Split(' ');
                        release.Size = ReleaseInfo.GetBytes(sizeParts[1], ParseUtil.CoerceFloat(sizeParts[0]));

                        var dateStr = qRow.Find(".ulInfo").Text().Split('|').Last().Trim();
                        var dateParts = dateStr.Split(' ');
                        var dateValue = ParseUtil.CoerceInt(dateParts[0]);
                        TimeSpan ts = TimeSpan.Zero;
                        if (dateStr.Contains("sec"))
                            ts = TimeSpan.FromSeconds(dateValue);
                        else if (dateStr.Contains("min"))
                            ts = TimeSpan.FromMinutes(dateValue);
                        else if (dateStr.Contains("hour"))
                            ts = TimeSpan.FromHours(dateValue);
                        else if (dateStr.Contains("day"))
                            ts = TimeSpan.FromDays(dateValue);
                        else if (dateStr.Contains("week"))
                            ts = TimeSpan.FromDays(dateValue * 7);
                        else if (dateStr.Contains("month"))
                            ts = TimeSpan.FromDays(dateValue * 30);
                        else if (dateStr.Contains("year"))
                            ts = TimeSpan.FromDays(dateValue * 365);
                        release.PublishDate = DateTime.Now - ts;

                        release.Seeders = ParseUtil.CoerceInt(qRow.Find(".seedersInfo").Text());
                        release.Peers = ParseUtil.CoerceInt(qRow.Find(".leechersInfo").Text()) + release.Seeders;

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

        public Task<byte[]> Download(Uri link)
        {
            return client.GetByteArrayAsync(link);
        }
    }
}
