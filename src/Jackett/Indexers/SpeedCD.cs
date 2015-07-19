using CsQuery;
using Newtonsoft.Json.Linq;
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
    public class SpeedCD : IndexerInterface
    {
        public event Action<IndexerInterface, JToken> OnSaveConfigurationRequested;

        public event Action<IndexerInterface, string, Exception> OnResultParsingError;

        public string DisplayName { get { return "Speed.cd"; } }

        public string DisplayDescription { get { return "Your home now!"; } }

        public Uri SiteLink { get { return new Uri(BaseUrl); } }

        public bool RequiresRageIDLookupDisabled { get { return true; } }

        const string BaseUrl = "http://speed.cd";
        const string LoginUrl = BaseUrl + "/take_login.php";
        const string SearchUrl = BaseUrl + "/V3/API/API.php";
        const string SearchFormData = "c53=1&c49=1&c2=1&c52=1&c41=1&c50=1&c30=1&jxt=4&jxw=b";
        const string CommentsUrl = BaseUrl + "/t/{0}";
        const string DownloadUrl = BaseUrl + "/download.php?torrent={0}";

        CookieContainer cookies;
        HttpClientHandler handler;
        HttpClient client;

        public bool IsConfigured { get; private set; }

        public SpeedCD()
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

                if (OnSaveConfigurationRequested != null)
                    OnSaveConfigurationRequested(this, configSaveData);

                IsConfigured = true;
            }
        }

        public void LoadFromSavedConfiguration(JToken jsonConfig)
        {
            cookies.FillFromJson(new Uri(BaseUrl), jsonConfig);
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
