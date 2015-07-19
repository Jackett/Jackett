using CsQuery;
using Jackett.Indexers;
using Jackett.Models;
using Jackett.Utils;
using Newtonsoft.Json.Linq;
using NLog;
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

namespace Jackett.Indexers
{
    public class Freshon : IIndexer
    {
        public event Action<IIndexer, string, Exception> OnResultParsingError;

        static string BaseUrl = "https://freshon.tv";
        static string LoginUrl = BaseUrl + "/login.php";
        static string LoginPostUrl = BaseUrl + "/login.php?action=makelogin";
        static string SearchUrl = BaseUrl + "/browse.php";

        static string chromeUserAgent = BrowserUtil.ChromeUserAgent;

        CookieContainer cookies;
        HttpClientHandler handler;
        HttpClient client;

        public bool IsConfigured { get; private set; }

        public string DisplayName { get { return "FreshOnTV"; } }

        public string DisplayDescription { get { return "Our goal is to provide the latest stuff in the TV show domain"; } }

        public Uri SiteLink { get { return new Uri(BaseUrl); } }

        public event Action<IIndexer, JToken> OnSaveConfigurationRequested;
        Logger logger;

        public Freshon(Logger l)
        {
            logger = l;
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

            var pairs = new Dictionary<string, string> {
				{ "username", config.Username.Value },
				{ "password", config.Password.Value }
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
                cookies.DumpToJson(SiteLink, configSaveData);

                if (OnSaveConfigurationRequested != null)
                    OnSaveConfigurationRequested(this, configSaveData);

                IsConfigured = true;
            }
        }

        public void LoadFromSavedConfiguration(JToken jsonConfig)
        {
            cookies.FillFromJson(new Uri(BaseUrl), jsonConfig, logger);
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
                try
                {
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

                        release.Seeders = ParseUtil.CoerceInt(qRow.Find("td.table_seeders").Text().Trim());
                        release.Peers = ParseUtil.CoerceInt(qRow.Find("td.table_leechers").Text().Trim()) + release.Seeders;

                        var sizeCol = qRow.Find("td.table_size")[0];
                        var sizeVal = ParseUtil.CoerceFloat(sizeCol.ChildNodes[0].NodeValue.Trim());
                        var sizeUnit = sizeCol.ChildNodes[2].NodeValue.Trim();
                        release.Size = ReleaseInfo.GetBytes(sizeUnit, sizeVal);

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
            var request = CreateHttpRequest(link);
            var response = await client.SendAsync(request);
            var bytes = await response.Content.ReadAsByteArrayAsync();
            return bytes;
        }
    }
}
