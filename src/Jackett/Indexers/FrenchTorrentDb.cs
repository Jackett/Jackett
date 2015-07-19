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
using System.Threading.Tasks;
using System.Web;

namespace Jackett.Indexers
{
    class FrenchTorrentDb : BaseIndexer, IIndexer
    {
        public event Action<IIndexer, Newtonsoft.Json.Linq.JToken> OnSaveConfigurationRequested;

        public event Action<IIndexer, string, Exception> OnResultParsingError;

        class ConfigurationDataBasicLoginFrenchTorrentDb : ConfigurationData
        {
            public StringItem Cookie { get; private set; }

            public ConfigurationDataBasicLoginFrenchTorrentDb()
            {
                Cookie = new StringItem { Name = "Cookie" };
            }

            public override Item[] GetItems()
            {
                return new Item[] { Cookie };
            }
        }


        private readonly string MainUrl = "";
        private readonly string SearchUrl = "";

        string cookie = string.Empty;

        CookieContainer cookies;
        HttpClientHandler handler;
        HttpClient client;

           public FrenchTorrentDb(IIndexerManagerService i, Logger l) :
            base(name: "FrenchTorrentDb",
            description: "One the biggest French Torrent Tracker",
            link: new Uri("http://www.frenchtorrentdb.com/"),
            rageid: true,
            manager: i,
            logger: l)
        {
            MainUrl = SiteLink + "?section=INDEX";
            SearchUrl = SiteLink + "?section=TORRENTS&exact=1&name={0}&submit=GO";

            cookies = new CookieContainer();
            handler = new HttpClientHandler
            {
                CookieContainer = cookies,
                AllowAutoRedirect = true,
                UseCookies = true,
            };
            client = new HttpClient(handler);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(BrowserUtil.ChromeUserAgent);
        }

        public Task<ConfigurationData> GetConfigurationForSetup()
        {
            var config = new ConfigurationDataUrl(SiteLink);
            return Task.FromResult<ConfigurationData>(config);
        }

        public async Task ApplyConfiguration(Newtonsoft.Json.Linq.JToken configJson)
        {
            var config = new ConfigurationDataBasicLoginFrenchTorrentDb();
            config.LoadValuesFromJson(configJson);
            cookies.SetCookies(SiteLink, "WebsiteID=" + config.Cookie.Value);
            var mainPage = await client.GetAsync(MainUrl);
            string responseContent = await mainPage.Content.ReadAsStringAsync();

            if (!responseContent.Contains("/?section=LOGOUT"))
            {
                throw new ExceptionWithConfigData("Failed to login", (ConfigurationData)config);
            }
            else
            {
                var configSaveData = new JObject();
                configSaveData["cookie"] = config.Cookie.Value;

                if (OnSaveConfigurationRequested != null)
                    OnSaveConfigurationRequested(this, configSaveData);

                IsConfigured = true;
            }
        }

        public void LoadFromSavedConfiguration(Newtonsoft.Json.Linq.JToken jsonConfig)
        {
            cookie = (string)jsonConfig["cookie"];
            cookies.SetCookies(SiteLink, "WebsiteID=" + cookie);
            IsConfigured = true;
        }

        public async Task<ReleaseInfo[]> PerformQuery(TorznabQuery query)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            var searchString = query.SanitizedSearchTerm + " " + query.GetEpisodeSearchString();
            var episodeSearchUrl = string.Format(SearchUrl, HttpUtility.UrlEncode(searchString));

            var message = new HttpRequestMessage();
            message.Method = HttpMethod.Get;
            message.RequestUri = new Uri(episodeSearchUrl);

            var response = await client.SendAsync(message);
            var results = await response.Content.ReadAsStringAsync();
            try
            {

                CQ dom = results;
                var rows = dom[".results_index ul"];
                foreach (var row in rows)
                {
                    var release = new ReleaseInfo();
                    CQ qRow = row.Cq();
                    CQ qLink = qRow.Find("li.torrents_name > .torrents_name_link").First();
                    CQ qDlLink = qRow.Find("li.torrents_download  > a").First();

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;
                    release.Title = qLink.Text().Trim();
                    release.Description = release.Title;
                    release.Comments = new Uri(SiteLink + "/" + qLink.Attr("href").TrimStart('/'));
                    release.Guid = release.Comments;
                    release.Link = new Uri(SiteLink + "/" + qDlLink.Attr("href").TrimStart('/'));
                    release.PublishDate = DateTime.Now;
                    release.Seeders = ParseUtil.CoerceInt(qRow.Find("li.torrents_seeders").Text());
                    release.Peers = ParseUtil.CoerceInt(qRow.Find("li.torrents_leechers").Text()) + release.Seeders;
                    var sizeParts = qRow.Find("li.torrents_size").Text().Split(' ');
                    var sizeVal = ParseUtil.CoerceFloat(sizeParts[0]);
                    var sizeUnit = sizeParts[1];
                    release.Size = ReleaseInfo.GetBytes(sizeUnit, sizeVal);

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnResultParsingError(this, results, ex);
                throw ex;
            }

            return releases.ToArray();
        }

        public Task<byte[]> Download(Uri link)
        {
            return client.GetByteArrayAsync(link);
        }
    }
}
