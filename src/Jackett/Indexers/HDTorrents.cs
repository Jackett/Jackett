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
    public class HDTorrents : IndexerInterface
    {
        public event Action<IndexerInterface, JToken> OnSaveConfigurationRequested;

        public event Action<IndexerInterface, string, Exception> OnResultParsingError;

        const string DefaultUrl = "https://hd-torrents.org";
        string BaseUrl = DefaultUrl;
        static string chromeUserAgent = BrowserUtil.ChromeUserAgent;
        private string SearchUrl = "https://hd-torrents.org/torrents.php?search={0}&active=1&options=0&category%5B%5D=59&category%5B%5D=60&category%5B%5D=30&category%5B%5D=38&page={1}";
        private static string LoginUrl = DefaultUrl + "/login.php";
        private static string LoginPostUrl = DefaultUrl + "/login.php?returnto=index.php";
        private const int MAXPAGES = 3;

        CookieContainer cookies;
        HttpClientHandler handler;
        HttpClient client;

        public HDTorrents()
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

        public string DisplayName
        {
            get { return "HD-Torrents"; }
        }

        public string DisplayDescription
        {
            get { return "HD-Torrents is a private torrent website with HD torrents and strict rules on their content."; }
        }

        public Uri SiteLink
        {
            get { return new Uri(DefaultUrl); }
        }

        public bool IsConfigured
        {
            get;
            private set;
        }

        public Task<ConfigurationData> GetConfigurationForSetup()
        {
            var config = new ConfigurationDataBasicLogin();
            return Task.FromResult<ConfigurationData>(config);
        }

        HttpRequestMessage CreateHttpRequest(string url)
        {
            var message = new HttpRequestMessage();
            message.Method = HttpMethod.Get;
            message.RequestUri = new Uri(url);
            message.Headers.UserAgent.ParseAdd(chromeUserAgent);
            return message;
        }

        public async Task ApplyConfiguration(JToken configJson)
        {
            var config = new ConfigurationDataBasicLogin();
            config.LoadValuesFromJson(configJson);

            var startMessage = CreateHttpRequest(LoginUrl);
            var results = await (await client.SendAsync(startMessage)).Content.ReadAsStringAsync();


            var pairs = new Dictionary<string, string> {
				{ "uid", config.Username.Value },
				{ "pwd", config.Password.Value }
			};

            var content = new FormUrlEncodedContent(pairs);

            var loginRequest = CreateHttpRequest(LoginUrl);
            loginRequest.Method = HttpMethod.Post;
            loginRequest.Content = content;
            loginRequest.Headers.Referrer = new Uri("https://hd-torrents.org/torrents.php");

            var response = await client.SendAsync(loginRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!responseContent.Contains("If your browser doesn't have javascript enabled"))
            {
                var errorMessage = "Couldn't login";
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
            cookies.FillFromJson(SiteLink, jsonConfig);
            IsConfigured = true;
        }

        async Task<ReleaseInfo[]> PerformQuery(TorznabQuery query, string baseUrl)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();
            List<string> searchurls = new List<string>();

            foreach (var title in query.ShowTitles ?? new string[] { string.Empty })
            {
                var searchString = title + " " + query.GetEpisodeSearchString();
                for (int page = 0; page < MAXPAGES; page++)
                    searchurls.Add(string.Format(SearchUrl, HttpUtility.UrlEncode(searchString.Trim()), page));
            }

            foreach (string SearchUrl in searchurls)
            {
                var results = await client.GetStringAsync(SearchUrl);
                try
                {
                    CQ dom = results;
                    ReleaseInfo release;

                    int rowCount = 0;
                    var rows = dom[".mainblockcontenttt > tbody > tr"];
                    foreach (var row in rows)
                    {
                        CQ qRow = row.Cq();
                        if (rowCount < 2 || qRow.Children().Count() != 12) //skip 2 rows because there's an empty row & a title/sort row
                        {
                            rowCount++;
                            continue;
                        }

                        release = new ReleaseInfo();
                        long? size;

                        release.Title = qRow.Find("td.mainblockcontent b a").Text();
                        release.Description = release.Title;

                        if (0 != qRow.Find("td.mainblockcontent u").Length)
                            release.Imdb = ParseUtil.TryCoerceLong(qRow.Find("td.mainblockcontent u").Parent().First().Attr("href").Replace("http://www.imdb.com/title/tt", "").Replace("/", ""));

                        release.MinimumRatio = 1;
                        release.MinimumSeedTime = 172800;

                        release.MagnetUri = new Uri(DefaultUrl + "/" + qRow.Find("td.mainblockcontent").Get(3).FirstChild.GetAttribute("href"));

                        release.Seeders = ParseUtil.TryCoerceInt(qRow.Find("td").Get(9).FirstChild.FirstChild.InnerText);
                        release.Peers = ParseUtil.TryCoerceInt(qRow.Find("td").Get(10).FirstChild.FirstChild.InnerText);

                        string fullSize = qRow.Find("td.mainblockcontent").Get(6).InnerText;
                        string[] sizeSplit = fullSize.Split(' ');
                        switch (sizeSplit[1].ToLower())
                        {
                            case "kb":
                                size = ReleaseInfo.BytesFromKB(ParseUtil.CoerceFloat(sizeSplit[0]));
                                break;
                            case "mb":
                                size = ReleaseInfo.BytesFromMB(ParseUtil.CoerceFloat(sizeSplit[0]));
                                break;
                            case "gb":
                                size = ReleaseInfo.BytesFromGB(ParseUtil.CoerceFloat(sizeSplit[0]));
                                break;
                            default:
                                size = null;
                                break;
                        }
                        release.Size = size;

                        release.Link = new Uri(DefaultUrl + "/" + qRow.Find("td.mainblockcontent b a").Attr("href"));
                        release.Guid = release.Link;

                        string[] dateSplit = qRow.Find("td.mainblockcontent").Get(5).InnerHTML.Split(',');
                        string dateString = dateSplit[1].Substring(0, dateSplit[1].IndexOf('>'));
                        release.PublishDate = DateTime.Parse(dateString, CultureInfo.InvariantCulture);

                        release.Comments = new Uri(DefaultUrl + "/" + qRow.Find("td.mainblockcontent").Get(2).FirstChild.GetAttribute("href"));

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
