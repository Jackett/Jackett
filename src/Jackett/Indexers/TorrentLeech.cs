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
    public class TorrentLeech : IndexerInterface
    {
        public event Action<IndexerInterface, string, Exception> OnResultParsingError;

        public event Action<IndexerInterface, JToken> OnSaveConfigurationRequested;

        public string DisplayName
        {
            get { return "TorrentLeech"; }
        }

        public string DisplayDescription
        {
            get { return "This is what happens when you seed"; }
        }

        public Uri SiteLink
        {
            get { return new Uri(BaseUrl); }
        }

        const string BaseUrl = "http://www.torrentleech.org";
        const string LoginUrl = BaseUrl + "/user/account/login/";
        const string SearchUrl = BaseUrl + "/torrents/browse/index/query/{0}/categories/2%2C26%2C27%2C32/orderby/added?";

        public bool IsConfigured { get; private set; }


        CookieContainer cookies;
        HttpClientHandler handler;
        HttpClient client;

        public TorrentLeech()
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

        public async Task ApplyConfiguration(JToken configJson)
        {
            var config = new ConfigurationDataBasicLogin();
            config.LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string> {
				{ "username", config.Username.Value },
				{ "password", config.Password.Value },
                { "remember_me", "on" },
                { "login", "submit" }
			};

            var content = new FormUrlEncodedContent(pairs);

            var response = await client.PostAsync(LoginUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!responseContent.Contains("/user/account/logout"))
            {
                CQ dom = responseContent;
                var messageEl = dom[".ui-state-error"].Last();
                var errorMessage = messageEl.Text().Trim();
                throw new ExceptionWithConfigData(errorMessage, (ConfigurationData)config);
            }
            else
            {
                var configSaveData = new JObject();
                configSaveData["cookies"] = cookies.ToJson(SiteLink);

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

                    CQ qRows = dom["#torrenttable > tbody > tr"];

                    foreach (var row in qRows)
                    {
                        var release = new ReleaseInfo();

                        var qRow = row.Cq();

                        var debug = qRow.Html();

                        release.MinimumRatio = 1;
                        release.MinimumSeedTime = 172800;

                        CQ qLink = qRow.Find(".title > a").First();
                        release.Guid = new Uri(BaseUrl + qLink.Attr("href"));
                        release.Comments = release.Guid;
                        release.Title = qLink.Text();
                        release.Description = release.Title;

                        release.Link = new Uri(BaseUrl + qRow.Find(".quickdownload > a").Attr("href"));

                        var dateString = qRow.Find(".name").First()[0].ChildNodes[4].NodeValue.Replace(" on", "").Trim();
                        //"2015-04-25 23:38:12"
                        //"yyyy-MMM-dd hh:mm:ss"
                        release.PublishDate = DateTime.ParseExact(dateString, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

                        var sizeStringParts = qRow.Children().ElementAt(4).InnerText.Split(' ');
                        release.Size = ReleaseInfo.GetBytes(sizeStringParts[1], float.Parse(sizeStringParts[0]));

                        release.Seeders = int.Parse(qRow.Find(".seeders").Text());
                        release.Peers = release.Seeders + int.Parse(qRow.Find(".leechers").Text());

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
