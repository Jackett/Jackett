using CsQuery;
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
using System.Web.UI.WebControls;

namespace Jackett
{
    public class Freshon : IndexerInterface
    {

        static string BaseUrl = "https://freshon.tv";
        static string LoginUrl = BaseUrl + "/login.php";
        static string LoginPostUrl = BaseUrl + "/login.php?action=makelogin";
        static string SearchUrl = BaseUrl + "/browse.php";

        static string chromeUserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2272.118 Safari/537.36";

        CookieContainer cookies;
        HttpClientHandler handler;
        HttpClient client;

        public bool IsConfigured { get; private set; }

        public string DisplayName { get { return "FreshOnTV"; } }

        public string DisplayDescription { get { return "Our goal is to provide the latest stuff in the TV show domain"; } }

        public Uri SiteLink { get { return new Uri(BaseUrl); } }

        public event Action<IndexerInterface, JToken> OnSaveConfigurationRequested;

        public Freshon()
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

        public async Task<ConfigurationData> GetConfigurationForSetup()
        {
            var request = CreateHttpRequest(new Uri(LoginUrl));
            var response = await client.SendAsync(request);
            await response.Content.ReadAsStreamAsync();
            var config = new ConfigurationDataBasicLogin();
            return config;
        }

        public async Task ApplyConfiguration(JToken configJson)
        {
            var config = new ConfigurationDataBasicLogin();
            config.LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string>
                {
                    { "username", config.Username.Value},
                    { "password", config.Password.Value}
                };

            var content = new FormUrlEncodedContent(pairs);
            var message = CreateHttpRequest(new Uri(LoginPostUrl));
            message.Method = HttpMethod.Post;
            message.Content = content;
            message.Headers.Referrer = new Uri(LoginUrl);

            var response = await client.SendAsync(message);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!responseContent.Contains("/logout.php"))
            {
                CQ dom = responseContent;
                var messageEl = dom[".error_text"];
                var errorMessage = messageEl.Text().Trim();
                throw new ExceptionWithConfigData(errorMessage, (ConfigurationData)config);
            }
            else
            {
                var configSaveData = new JObject();
                configSaveData["cookies"] = new JArray((
                    from cookie in cookies.GetCookies(new Uri(BaseUrl)).Cast<Cookie>()
                    select cookie.Name + ":" + cookie.Value
                ).ToArray());

                if (OnSaveConfigurationRequested != null)
                    OnSaveConfigurationRequested(this, configSaveData);

                IsConfigured = true;
            }
        }

        public void LoadFromSavedConfiguration(JToken jsonConfig)
        {
            cookies.FillFromJson(new Uri(BaseUrl), (JArray)jsonConfig["cookies"]);
            IsConfigured = true;
        }

        HttpRequestMessage CreateHttpRequest(Uri uri)
        {
            var message = new HttpRequestMessage();
            message.Method = HttpMethod.Get;
            message.RequestUri = uri;
            message.Headers.UserAgent.ParseAdd(chromeUserAgent);
            return message;
        }

        public async Task<ReleaseInfo[]> PerformQuery(TorznabQuery query)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            foreach (var title in query.ShowTitles ?? new string[] { string.Empty })
            {
                string episodeSearchUrl;

                if (string.IsNullOrEmpty(title))
                    episodeSearchUrl = SearchUrl;
                else
                {
                    var searchString = title + " " + query.GetEpisodeSearchString();
                    episodeSearchUrl = string.Format("{0}?search={1}&cat=0", SearchUrl, HttpUtility.UrlEncode(searchString));
                }

                var request = CreateHttpRequest(new Uri(episodeSearchUrl));
                var response = await client.SendAsync(request);
                var results = await response.Content.ReadAsStringAsync();

                CQ dom = results;

                var rows = dom["#highlight > tbody > tr"];

                foreach (var row in rows.Skip(1))
                {
                    var release = new ReleaseInfo();

                    var qRow = row.Cq();
                    var qLink = qRow.Find("a.torrent_name_link").First();

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;
                    release.Title = qLink.Attr("title");
                    release.Description = release.Title;
                    release.Guid = new Uri(BaseUrl + qLink.Attr("href"));
                    release.Comments = release.Guid;
                    release.Link = new Uri(BaseUrl + qRow.Find("td.table_links > a").First().Attr("href"));

                    DateTime pubDate;
                    var dateString = qRow.Find("td.table_added").Text().Trim();
                    if (dateString.StartsWith("Today "))
                        pubDate = (DateTime.UtcNow + TimeSpan.Parse(dateString.Split(' ')[1])).ToLocalTime();
                    else if (dateString.StartsWith("Yesterday "))
                        pubDate = (DateTime.UtcNow + TimeSpan.Parse(dateString.Split(' ')[1]) - TimeSpan.FromDays(1)).ToLocalTime();
                    else
                        pubDate = DateTime.ParseExact(dateString, "d-MMM-yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToLocalTime();
                    release.PublishDate = pubDate;

                    release.Seeders = int.Parse(qRow.Find("td.table_seeders").Text().Trim());
                    release.Peers = int.Parse(qRow.Find("td.table_leechers").Text().Trim()) + release.Seeders;

                    var sizeCol = qRow.Find("td.table_size")[0];
                    var sizeVal = float.Parse(sizeCol.ChildNodes[0].NodeValue.Trim());
                    var sizeUnit = sizeCol.ChildNodes[2].NodeValue.Trim();
                    release.Size = ReleaseInfo.GetBytes(sizeUnit, sizeVal);

                    releases.Add(release);
                }
            }

            return releases.ToArray();
        }

        public async Task<byte[]> Download(Uri link)
        {
            var request = CreateHttpRequest(link);
            var response = await client.SendAsync(request);
            var bytes = await response.Content.ReadAsByteArrayAsync();
            return bytes;
        }
    }
}
