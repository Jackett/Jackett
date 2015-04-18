using CsQuery;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Jackett.Indexers
{
    public class IPTorrents : IndexerInterface
    {

        public event Action<IndexerInterface, Newtonsoft.Json.Linq.JToken> OnSaveConfigurationRequested;

        public string DisplayName { get { return "IPTorrents"; } }

        public string DisplayDescription { get { return "Always a step ahead"; } }

        public Uri SiteLink { get { return new Uri(BaseUrl); } }

        public bool IsConfigured { get; private set; }

        static string chromeUserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2272.118 Safari/537.36";


        static string BaseUrl = "https://iptorrents.com";

        string SearchUrl = BaseUrl + "/t?q=";


        CookieContainer cookies;
        HttpClientHandler handler;
        HttpClient client;

        public IPTorrents()
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
            await client.GetAsync(new Uri(BaseUrl));
            var config = new ConfigurationDataBasicLogin();
            return (ConfigurationData)config;
        }

        public async Task ApplyConfiguration(Newtonsoft.Json.Linq.JToken configJson)
        {

            var config = new ConfigurationDataBasicLogin();
            config.LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string>
                {
                    { "username", config.Username.Value},
                    { "password", config.Password.Value}
                };

            var content = new FormUrlEncodedContent(pairs);
            var message = new HttpRequestMessage();
            message.Method = HttpMethod.Post;
            message.Content = content;
            message.RequestUri = new Uri(BaseUrl);
            message.Headers.Referrer = new Uri(BaseUrl);
            message.Headers.UserAgent.ParseAdd(chromeUserAgent);

            var response = await client.SendAsync(message);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!responseContent.Contains("/my.php"))
            {
                CQ dom = responseContent;
                var messageEl = dom["body > div"].First();
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

        HttpRequestMessage CreateHttpRequest(Uri uri)
        {
            var message = new HttpRequestMessage();
            message.Method = HttpMethod.Get;
            message.RequestUri = uri;
            message.Headers.UserAgent.ParseAdd(chromeUserAgent);
            return message;
        }

        public Task VerifyConnection()
        {
            return Task.Run(async () =>
            {
                var message = CreateHttpRequest(new Uri(BaseUrl));

                var response = await client.SendAsync(message);
                var result = await response.Content.ReadAsStringAsync();
                if (!result.Contains("/my.php"))
                    throw new Exception("Detected as not logged in");
            });
        }

        public void LoadFromSavedConfiguration(Newtonsoft.Json.Linq.JToken jsonConfig)
        {
            cookies.FillFromJson(new Uri(BaseUrl), (JArray)jsonConfig["cookies"]);
            IsConfigured = true;
        }

        public async Task<ReleaseInfo[]> PerformQuery(TorznabQuery query)
        {

            List<ReleaseInfo> releases = new List<ReleaseInfo>();


            foreach (var title in query.ShowTitles ?? new string[] { string.Empty })
            {

                var searchString = title + " " + query.GetEpisodeSearchString();
                var episodeSearchUrl = SearchUrl + HttpUtility.UrlEncode(searchString);

                var request = CreateHttpRequest(new Uri(episodeSearchUrl));
                var response = await client.SendAsync(request);
                var results = await response.Content.ReadAsStringAsync();

                CQ dom = results;

                var rows = dom["table.torrents > tbody > tr"];
                foreach (var row in rows.Skip(1))
                {
                    var release = new ReleaseInfo();

                    var qRow = row.Cq();

                    var qTitleLink = qRow.Find("a.t_title").First();
                    release.Title = qTitleLink.Text().Trim();
                    release.Description = release.Title;
                    release.Guid = new Uri(BaseUrl + qTitleLink.Attr("href"));
                    release.Comments = release.Guid;

                    DateTime pubDate;
                    var descString = qRow.Find(".t_ctime").Text();
                    var dateString = descString.Split('|').Last().Trim();
                    dateString = dateString.Split(new string[] { " by " }, StringSplitOptions.None)[0];
                    var dateValue = float.Parse(dateString.Split(' ')[0]);
                    var dateUnit = dateString.Split(' ')[1];
                    if (dateUnit.Contains("minute"))
                        pubDate = DateTime.Now - TimeSpan.FromMinutes(dateValue);
                    else if (dateUnit.Contains("hour"))
                        pubDate = DateTime.Now - TimeSpan.FromHours(dateValue);
                    else if (dateUnit.Contains("day"))
                        pubDate = DateTime.Now - TimeSpan.FromDays(dateValue);
                    else if (dateUnit.Contains("week"))
                        pubDate = DateTime.Now - TimeSpan.FromDays(7 * dateValue);
                    else if (dateUnit.Contains("month"))
                        pubDate = DateTime.Now - TimeSpan.FromDays(30 * dateValue);
                    else if (dateUnit.Contains("year"))
                        pubDate = DateTime.Now - TimeSpan.FromDays(365 * dateValue);
                    else
                        pubDate = DateTime.MinValue;
                    release.PublishDate = pubDate;

                    var qLink = row.ChildElements.ElementAt(3).Cq().Children("a");
                    release.Link = new Uri(BaseUrl + qLink.Attr("href"));

                    var sizeStr = row.ChildElements.ElementAt(5).Cq().Text().Trim();
                    var sizeVal = float.Parse(sizeStr.Split(' ')[0]);
                    var sizeUnit = sizeStr.Split(' ')[1];
                    release.Size = ReleaseInfo.GetBytes(sizeUnit, sizeVal);

                    release.Seeders = int.Parse(qRow.Find(".t_seeders").Text().Trim());
                    release.Peers = int.Parse(qRow.Find(".t_leechers").Text().Trim()) + release.Seeders;

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
