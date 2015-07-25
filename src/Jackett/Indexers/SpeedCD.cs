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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Jackett.Indexers
{
    public class SpeedCD : BaseIndexer, IIndexer
    {
        private readonly string LoginUrl = "";
        private readonly string SearchUrl = "";
        private readonly string SearchFormData = "c53=1&c49=1&c2=1&c52=1&c41=1&c50=1&c30=1&jxt=4&jxw=b";
        private readonly string CommentsUrl = "";
        private readonly string DownloadUrl = "";

        CookieContainer cookies;
        HttpClientHandler handler;
        HttpClient client;

        public SpeedCD(IIndexerManagerService i, Logger l)
            : base(name: "Speed.cd",
                description: "Your home now!",
                link: new Uri("http://speed.cd"),
                caps: TorznabCapsUtil.CreateDefaultTorznabTVCaps(),
                manager: i,
                logger: l)
        {
            LoginUrl = SiteLink + "take_login.php";
            SearchUrl = SiteLink + "V3/API/API.php";
            CommentsUrl = SiteLink + "t/{0}";
            DownloadUrl = SiteLink + "download.php?torrent={0}";

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
			};

            var content = new FormUrlEncodedContent(pairs);

            var response = await client.PostAsync(LoginUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!responseContent.Contains("logout.php"))
            {
                CQ dom = responseContent;
                var errorMessage = dom["h5"].First().Text().Trim();
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

            var formData = HttpUtility.ParseQueryString(SearchFormData);
            var formDict = formData.AllKeys.ToDictionary(t => t, t => formData[t]);
            formDict.Add("search", query.SanitizedSearchTerm);
            var content = new FormUrlEncodedContent(formDict);

            var response = await client.PostAsync(SearchUrl, content);
            var results = await response.Content.ReadAsStringAsync();

            try
            {
                var jsonResult = JObject.Parse(results);
                var resultArray = ((JArray)jsonResult["Fs"])[0]["Cn"]["torrents"];
                foreach (var jobj in resultArray)
                {
                    var release = new ReleaseInfo();

                    var id = (int)jobj["id"];
                    release.Comments = new Uri(string.Format(CommentsUrl, id));
                    release.Guid = release.Comments;
                    release.Link = new Uri(string.Format(DownloadUrl, id));

                    release.Title = Regex.Replace((string)jobj["name"], "<.*?>", String.Empty);

                    var sizeParts = ((string)jobj["size"]).Split(' ');
                    release.Size = ReleaseInfo.GetBytes(sizeParts[1], ParseUtil.CoerceFloat(sizeParts[0]));

                    release.Seeders = ParseUtil.CoerceInt((string)jobj["seed"]);
                    release.Peers = ParseUtil.CoerceInt((string)jobj["leech"]) + release.Seeders;

                    // ex: Tuesday, May 26, 2015 at 6:00pm
                    var dateStr = new Regex("title=\"(.*?)\"").Match((string)jobj["added"]).Groups[1].ToString();
                    dateStr = dateStr.Replace(" at", "");
                    var dateTime = DateTime.ParseExact(dateStr, "dddd, MMMM d, yyyy h:mmtt", CultureInfo.InvariantCulture);
                    release.PublishDate = dateTime;

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
