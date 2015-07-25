using CsQuery;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Net.Http.Headers;
using Jackett.Models;
using Jackett.Utils;
using NLog;
using Jackett.Services;
using Jackett.Utils.Clients;

namespace Jackett.Indexers
{
    public class AlphaRatio : BaseIndexer, IIndexer
    {
        private readonly string LoginUrl = "";
        private readonly string SearchUrl = "";
        private readonly string DownloadUrl = "";
        private readonly string GuidUrl = "";

        private IWebClient webclient;
        private string cookieHeader = "";

        public AlphaRatio(IIndexerManagerService i, IWebClient w, Logger l)
            : base(name: "AlphaRatio",
                description: "Legendary",
                link: new Uri("https://alpharatio.cc"),
                caps: TorznabCapsUtil.CreateDefaultTorznabTVCaps(),
                manager: i,
                logger: l)
        {
            LoginUrl = SiteLink + "login.php";
            SearchUrl = SiteLink + "ajax.php?action=browse&searchstr=";
            DownloadUrl = SiteLink + "torrents.php?action=download&id=";
            GuidUrl = SiteLink + "torrents.php?torrentid=";
            webclient = w;
        }

        public Task<ConfigurationData> GetConfigurationForSetup()
        {
            var config = new ConfigurationDataBasicLogin();
            return Task.FromResult<ConfigurationData>(config);
        }

        public async Task ApplyConfiguration(JToken configJson)
        {
            var incomingConfig = new ConfigurationDataBasicLogin();
            incomingConfig.LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string> {
				{ "username", incomingConfig.Username.Value },
				{ "password", incomingConfig.Password.Value },
				{ "login", "Login" },
			    { "keeplogged", "1" }
			};

            // Do the login
            var response = await webclient.GetString(new Utils.Clients.WebRequest()
            {
                PostData = pairs,
                Referer = LoginUrl,
                Type = RequestType.POST,
                Url = LoginUrl
            });

            cookieHeader = response.Cookies;

            if (response.Status == HttpStatusCode.Found || response.Status == HttpStatusCode.Redirect || response.Status == HttpStatusCode.RedirectMethod)
            {
                response = await webclient.GetString(new Utils.Clients.WebRequest()
                {
                    Url = SiteLink.ToString(),
                    Referer = LoginUrl.ToString(),
                    Cookies = cookieHeader
                });
            }

            if (!response.Content.Contains("logout.php?"))
            {
                CQ dom = response.Content;
                dom["#loginform > table"].Remove();
                var errorMessage = dom["#loginform"].Text().Trim().Replace("\n\t", " ");
                throw new ExceptionWithConfigData(errorMessage, (ConfigurationData)incomingConfig);
            }
            else
            {
                var configSaveData = new JObject();
                configSaveData["cookie_header"] = cookieHeader;
                SaveConfig(configSaveData);
                IsConfigured = true;
            }
        }

        HttpRequestMessage CreateHttpRequest(Uri uri)
        {
            var message = new HttpRequestMessage();
            message.Method = HttpMethod.Post;
            message.RequestUri = uri;
            message.Headers.UserAgent.ParseAdd(BrowserUtil.ChromeUserAgent);
            return message;
        }

        public void LoadFromSavedConfiguration(JToken jsonConfig)
        {
            cookieHeader = (string)jsonConfig["cookie_header"];
            if (!string.IsNullOrEmpty(cookieHeader))
            {
                IsConfigured = true;
            }
        }

        void FillReleaseInfoFromJson(ReleaseInfo release, JObject r)
        {
            var id = r["torrentId"];
            release.Size = (long)r["size"];
            release.Seeders = (int)r["seeders"];
            release.Peers = (int)r["leechers"] + release.Seeders;
            release.Guid = new Uri(GuidUrl + id);
            release.Comments = release.Guid;
            release.Link = new Uri(DownloadUrl + id);
        }

        public async Task<ReleaseInfo[]> PerformQuery(TorznabQuery query)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();
            var searchString = query.SanitizedSearchTerm + " " + query.GetEpisodeSearchString();
            var episodeSearchUrl = SearchUrl + HttpUtility.UrlEncode(searchString);

            var results = await webclient.GetString(new Utils.Clients.WebRequest()
            {
                Cookies = cookieHeader,
                Type = RequestType.GET,
                Url = episodeSearchUrl
            });


            try
            {

                var json = JObject.Parse(results.Content);
                foreach (JObject r in json["response"]["results"])
                {
                    DateTime pubDate = DateTime.MinValue;
                    double dateNum;
                    if (double.TryParse((string)r["groupTime"], out dateNum))
                        pubDate = UnixTimestampToDateTime(dateNum);

                    var groupName = (string)r["groupName"];

                    if (r["torrents"] is JArray)
                    {
                        foreach (JObject t in r["torrents"])
                        {
                            var release = new ReleaseInfo();
                            release.PublishDate = pubDate;
                            release.Title = groupName;
                            release.Description = groupName;
                            FillReleaseInfoFromJson(release, t);
                            releases.Add(release);
                        }
                    }
                    else
                    {
                        var release = new ReleaseInfo();
                        release.PublishDate = pubDate;
                        release.Title = groupName;
                        release.Description = groupName;
                        FillReleaseInfoFromJson(release, r);
                        releases.Add(release);
                    }

                }
            }
            catch (Exception ex)
            {
                OnParseError(results.Content, ex);
            }

            return releases.ToArray();
        }

        static DateTime UnixTimestampToDateTime(double unixTime)
        {
            DateTime unixStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            long unixTimeStampInTicks = (long)(unixTime * TimeSpan.TicksPerSecond);
            return new DateTime(unixStart.Ticks + unixTimeStampInTicks);
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

    }
}
