using CsQuery;
using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
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
        private string LoginUrl { get { return SiteLink + "take_login.php"; } }
        private string SearchUrl { get { return SiteLink + "V3/API/API.php"; } }
        private string SearchFormData { get { return "c53=1&c49=1&c2=1&c52=1&c41=1&c50=1&c30=1&jxt=4&jxw=b"; } }
        private string CommentsUrl { get { return SiteLink + "t/{0}"; } }
        private string DownloadUrl { get { return SiteLink + "download.php?torrent={0}"; } }
       
        public SpeedCD(IIndexerManagerService i, Logger l, IWebClient wc)
            : base(name: "Speed.cd",
                description: "Your home now!",
                link: "http://speed.cd/",
                caps: TorznabCapsUtil.CreateDefaultTorznabTVCaps(),
                manager: i,
                client: wc,
                logger: l)
        {
        }

        public Task<ConfigurationData> GetConfigurationForSetup()
        {
            return Task.FromResult<ConfigurationData>(new ConfigurationDataBasicLogin());
        }

        public async Task ApplyConfiguration(JToken configJson)
        {
            var incomingConfig = new ConfigurationDataBasicLogin();
            incomingConfig.LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string> {
				{ "username", incomingConfig.Username.Value },
				{ "password", incomingConfig.Password.Value },
			};

            var result = await RequestLoginAndFollowRedirect(SiteLink, pairs, null, true, null, SiteLink);
            ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("logout.php"), () =>
            {
                CQ dom = result.Content;
                var errorMessage = dom["h5"].First().Text().Trim();
                throw new ExceptionWithConfigData(errorMessage, (ConfigurationData)incomingConfig);
            });
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var formData = HttpUtility.ParseQueryString(SearchFormData);
            var formDict = formData.AllKeys.ToDictionary(t => t, t => formData[t]);
            formDict.Add("search", query.SanitizedSearchTerm);
            var response = await PostDataWithCookies(SearchUrl, formDict);
            try
            {
                var jsonResult = JObject.Parse(response.Content);
                var resultArray = ((JArray)jsonResult["Fs"])[0]["Cn"]["torrents"];
                foreach (var jobj in resultArray)
                {
                    var release = new ReleaseInfo();

                    var id = (int)jobj["id"];
                    release.Comments = new Uri(string.Format(CommentsUrl, id));
                    release.Guid = release.Comments;
                    release.Link = new Uri(string.Format(DownloadUrl, id));

                    release.Title = Regex.Replace((string)jobj["name"], "<.*?>", String.Empty);

                    var SizeStr = ((string)jobj["size"]);
                    release.Size = ReleaseInfo.GetBytes(SizeStr);

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
                OnParseError(response.Content, ex);
            }
            return releases;
        }
    }
}
