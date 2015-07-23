using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jackett.Models;
using Newtonsoft.Json.Linq;
using NLog;
using Jackett.Utils;
using System.Net;
using System.Net.Http;
using CsQuery;
using System.Web;
using Jackett.Services;
using Jackett.Utils.Clients;
using System.Text.RegularExpressions;

namespace Jackett.Indexers
{
    public class PrivateHD : BaseIndexer, IIndexer
    {
        private readonly string LoginUrl = "";
        private readonly string SearchUrl = "";
        private string cookieHeader = "";

        private IWebClient webclient;

        public PrivateHD(IIndexerManagerService i, IWebClient wc, Logger l)
            : base(name: "PrivateHD",
                description: "BitTorrent site for High Quality, High Definition (HD) movies and TV Shows",
                link: new Uri("https://privatehd.to"),
                caps: TorznabCapsUtil.CreateDefaultTorznabTVCaps(),
                manager: i,
                logger: l)
        {
            LoginUrl = SiteLink + "auth/login";
            SearchUrl = SiteLink + "torrents?in=1&type=2&search={0}";
            webclient = wc;
        }

        public async Task ApplyConfiguration(JToken configJson)
        {
            var config = new ConfigurationDataBasicLogin();
            config.LoadValuesFromJson(configJson);

            var loginPage = await webclient.GetString(new Utils.Clients.WebRequest()
            {
                Url = LoginUrl,
                Type = RequestType.GET,
                AutoRedirect = true,
            });

            var token = new Regex("Avz.CSRF_TOKEN = '(.*?)';").Match(loginPage.Content).Groups[1].ToString();
            var pairs = new Dictionary<string, string> {
                { "_token", token },
                { "username_email", config.Username.Value },
                { "password", config.Password.Value },
                { "remember", "on" }
            };

            var response = await webclient.GetString(new Utils.Clients.WebRequest()
            {
                Url = LoginUrl,
                PostData = pairs,
                Referer = LoginUrl,
                Type = RequestType.POST,
                AutoRedirect = true,
                Cookies = loginPage.Cookies
            });

            if (!response.Content.Contains("auth/logout"))
            {
                CQ dom = response.Content;
                var messageEl = dom[".form-error"];
                var errorMessage = messageEl.Text().Trim();
                throw new ExceptionWithConfigData(errorMessage, (ConfigurationData)config);
            }
            else
            {
                cookieHeader = response.Cookies;
                var configSaveData = new JObject();
                configSaveData["cookies"] = cookieHeader;
                SaveConfig(configSaveData);
                IsConfigured = true;
            }

        }

        public async Task<byte[]> Download(Uri link)
        {
            var response = await webclient.GetBytes(new Utils.Clients.WebRequest()
            {
                Url = link.ToString(),
                Cookies = cookieHeader
            });

            return response.Content;
        }

        public Task<ConfigurationData> GetConfigurationForSetup()
        {
            var config = new ConfigurationDataBasicLogin();
            return Task.FromResult<ConfigurationData>(config);
        }

        public void LoadFromSavedConfiguration(JToken jsonConfig)
        {
            cookieHeader = (string)jsonConfig["cookies"];
            IsConfigured = true;
        }

        public async Task<ReleaseInfo[]> PerformQuery(TorznabQuery query)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            var searchString = query.SanitizedSearchTerm + " " + query.GetEpisodeSearchString();
            var episodeSearchUrl = string.Format(SearchUrl, HttpUtility.UrlEncode(searchString));

            var response = await webclient.GetString(new Utils.Clients.WebRequest()
            {
                Url = episodeSearchUrl,
                Referer = SiteLink.ToString(),
                Cookies = cookieHeader
            });
            var results = response.Content;

            try
            {
                CQ dom = results;
                var rows = dom["table > tbody > tr"];
                foreach (var row in rows)
                {
                    CQ qRow = row.Cq();
                    var release = new ReleaseInfo();

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;

                    var qLink = row.ChildElements.ElementAt(1).FirstElementChild.Cq();
                    release.Title = qLink.Text().Trim();
                    release.Comments = new Uri(qLink.Attr("href"));
                    release.Guid = release.Comments;

                    var qDownload = row.ChildElements.ElementAt(3).FirstElementChild.Cq();
                    release.Link = new Uri(qDownload.Attr("href"));

                    var dateStr = row.ChildElements.ElementAt(5).Cq().Text().Trim();
                    release.PublishDate = DateTimeUtil.FromTimeAgo(dateStr);

                    var sizeStr = row.ChildElements.ElementAt(6).Cq().Text().Trim();
                    var sizeParts = sizeStr.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
                    release.Size = ReleaseInfo.GetBytes(sizeParts[1], ParseUtil.CoerceFloat(sizeParts[0]));

                    release.Seeders = ParseUtil.CoerceInt(row.ChildElements.ElementAt(8).Cq().Text());
                    release.Peers = ParseUtil.CoerceInt(row.ChildElements.ElementAt(9).Cq().Text()) + release.Seeders;

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(results, ex);
            }
            return releases.ToArray();
        }
    }
}
