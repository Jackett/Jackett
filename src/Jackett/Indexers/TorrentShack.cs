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
    public class TorrentShack : BaseIndexer, IIndexer
    {
        private readonly string LoginUrl = "";
        private readonly string SearchUrl = "";

        CookieContainer cookies;
        HttpClientHandler handler;
        HttpClient client;

        public TorrentShack(IIndexerManagerService i, Logger l)
            : base(name: "TorrentShack",
                description: "TorrentShack",
                link: new Uri("http://torrentshack.me"),
                caps: TorznabCapsUtil.CreateDefaultTorznabTVCaps(),
                manager: i,
                logger: l)
        {
            LoginUrl = SiteLink + "login.php";
            SearchUrl = SiteLink + "torrents.php?searchstr={0}&release_type=both&searchtags=&tags_type=0&order_by=s3&order_way=desc&torrent_preset=all&filter_cat%5B600%5D=1&filter_cat%5B620%5D=1&filter_cat%5B700%5D=1&filter_cat%5B981%5D=1&filter_cat%5B980%5D=1";
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
                var rows = dom["#torrent_table > tbody > tr.torrent"];
                foreach (var row in rows)
                {
                    CQ qRow = row.Cq();
                    var release = new ReleaseInfo();

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;
                    release.Title = qRow.Find(".torrent_name_link").Text();
                    release.Description = release.Title;
                    release.Guid = new Uri(SiteLink + "/" + qRow.Find(".torrent_name_link").Parent().Attr("href"));
                    release.Comments = release.Guid;
                    release.Link = new Uri(SiteLink + "/" + qRow.Find(".torrent_handle_links > a").First().Attr("href"));

                    var dateStr = qRow.Find(".time").Text().Trim();
                    release.PublishDate = DateTimeUtil.FromTimeAgo(dateStr);

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
