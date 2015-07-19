using CsQuery;
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
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Jackett.Indexers
{
    public class TorrentShack : IIndexer
    {
        public event Action<IIndexer, JToken> OnSaveConfigurationRequested;

        public event Action<IIndexer, string, Exception> OnResultParsingError;

        public string DisplayName
        {
            get { return "TorrentShack"; }
        }

        public string DisplayDescription
        {
            get { return DisplayName; }
        }

        public Uri SiteLink
        {
            get { return new Uri(BaseUrl); }
        }

        public bool RequiresRageIDLookupDisabled { get { return true; } }

        const string BaseUrl = "http://torrentshack.me";
        const string LoginUrl = BaseUrl + "/login.php";
        const string SearchUrl = BaseUrl + "/torrents.php?searchstr={0}&release_type=both&searchtags=&tags_type=0&order_by=s3&order_way=desc&torrent_preset=all&filter_cat%5B600%5D=1&filter_cat%5B620%5D=1&filter_cat%5B700%5D=1&filter_cat%5B981%5D=1&filter_cat%5B980%5D=1";


        CookieContainer cookies;
        HttpClientHandler handler;
        HttpClient client;
        Logger logger;

        public bool IsConfigured { get; private set; }

        public TorrentShack(Logger l)
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
                { "keeplogged", "1" },
                { "login", "Login" }
			};

            var content = new FormUrlEncodedContent(pairs);

            var response = await client.PostAsync(LoginUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!responseContent.Contains("logout.php"))
            {
                CQ dom = responseContent;
                var messageEl = dom["#loginform"];
                messageEl.Children("table").Remove();
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

        public async Task<ReleaseInfo[]> PerformQuery(TorznabQuery query)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();


            var searchString = query.SanitizedSearchTerm + " " + query.GetEpisodeSearchString();
            var episodeSearchUrl = string.Format(SearchUrl, HttpUtility.UrlEncode(searchString));
            var results = await client.GetStringAsync(episodeSearchUrl);
            try
            {
                CQ dom = results;
                var rows = dom["#torrent_table > tbody > tr.torrent"];
                foreach (var row in rows)
                {
                    CQ qRow = row.Cq();
                    var release = new ReleaseInfo();

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;
                    release.Title = qRow.Find(".torrent_name_link").Text();
                    release.Description = release.Title;
                    release.Guid = new Uri(BaseUrl + "/" + qRow.Find(".torrent_name_link").Parent().Attr("href"));
                    release.Comments = release.Guid;
                    release.Link = new Uri(BaseUrl + "/" + qRow.Find(".torrent_handle_links > a").First().Attr("href"));

                    var dateStr = qRow.Find(".time").Text().Trim();
                    if (dateStr.ToLower().Contains("just now"))
                        release.PublishDate = DateTime.Now;
                    else
                    {
                        var dateParts = dateStr.Split(' ');
                        var dateValue = ParseUtil.CoerceInt(dateParts[0]);
                        TimeSpan ts = TimeSpan.Zero;
                        if (dateStr.Contains("Just now"))
                            ts = TimeSpan.Zero;
                        else if (dateStr.Contains("sec"))
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
                    }

                    var sizeStr = qRow.Find(".size")[0].ChildNodes[0].NodeValue.Trim();
                    var sizeParts = sizeStr.Split(' ');
                    release.Size = ReleaseInfo.GetBytes(sizeParts[1], ParseUtil.CoerceFloat(sizeParts[0]));
                    release.Seeders = ParseUtil.CoerceInt(qRow.Children().ElementAt(6).InnerText.Trim());
                    release.Peers = ParseUtil.CoerceInt(qRow.Children().ElementAt(7).InnerText.Trim()) + release.Seeders;

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
