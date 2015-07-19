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
    public class SceneTime : BaseIndexer, IIndexer
    {
        private readonly string LoginUrl = "";
        private readonly string SearchUrl = "";
        private readonly string DownloadUrl = "";

        CookieContainer cookies;
        HttpClientHandler handler;
        HttpClient client;

        public SceneTime(IIndexerManagerService i, Logger l)
            : base(name: "SceneTime",
                description: "Always on time",
                link: new Uri("https://www.scenetime.com"),
                caps: TorznabCapsUtil.CreateDefaultTorznabTVCaps(),
                manager: i,
                logger: l)
        {
            LoginUrl = SiteLink + "/takelogin.php";
            SearchUrl = SiteLink + "/browse_API.php";
            DownloadUrl = SiteLink + "/download.php/{0}/download.torrent";

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
				{ "password", config.Password.Value }
			};

            var content = new FormUrlEncodedContent(pairs);

            var response = await client.PostAsync(LoginUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!responseContent.Contains("logout.php"))
            {
                CQ dom = responseContent;
                var errorMessage = dom["td.text"].Text().Trim();
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

        FormUrlEncodedContent GetSearchFormData(string searchString)
        {
            var pairs = new Dictionary<string, string> {
				{ "c2", "1" }, { "c43", "1" }, { "c9", "1" }, { "c63", "1" }, { "c77", "1" }, { "c100", "1" }, { "c101", "1" },
                { "cata", "yes" }, { "sec", "jax" },
                { "search", searchString}
			};
            var content = new FormUrlEncodedContent(pairs);
            return content;
        }

        public async Task<ReleaseInfo[]> PerformQuery(TorznabQuery query)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            var searchString = query.SanitizedSearchTerm + " " + query.GetEpisodeSearchString();

            var searchContent = GetSearchFormData(searchString);
            var response = await client.PostAsync(SearchUrl, searchContent);
            var results = await response.Content.ReadAsStringAsync();

            try
            {
                CQ dom = results;
                var rows = dom["tr.browse"];
                foreach (var row in rows)
                {
                    var release = new ReleaseInfo();
                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;

                    var descCol = row.ChildElements.ElementAt(1);
                    var qDescCol = descCol.Cq();
                    var qLink = qDescCol.Find("a");
                    release.Title = qLink.Text();
                    release.Description = release.Title;
                    release.Comments = new Uri(SiteLink + "/" + qLink.Attr("href"));
                    release.Guid = release.Comments;
                    var torrentId = qLink.Attr("href").Split('=')[1];
                    release.Link = new Uri(string.Format(DownloadUrl, torrentId));

                    var dateStr = descCol.ChildNodes.Last().NodeValue.Trim();
                    var euDate = DateTime.ParseExact(dateStr, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    var localDate = TimeZoneInfo.ConvertTimeToUtc(euDate, TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time")).ToLocalTime();
                    release.PublishDate = localDate;

                    var sizeNodes = row.ChildElements.ElementAt(3).ChildNodes;
                    var sizeVal = sizeNodes.First().NodeValue;
                    var sizeUnit = sizeNodes.Last().NodeValue;
                    release.Size = ReleaseInfo.GetBytes(sizeUnit, ParseUtil.CoerceFloat(sizeVal));

                    release.Seeders = ParseUtil.CoerceInt(row.ChildElements.ElementAt(4).Cq().Text().Trim());
                    release.Peers = ParseUtil.CoerceInt(row.ChildElements.ElementAt(5).Cq().Text().Trim()) + release.Seeders;

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
