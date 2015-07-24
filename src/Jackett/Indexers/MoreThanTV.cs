using CsQuery;
using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Jackett.Indexers
{
    public class MoreThanTV : BaseIndexer, IIndexer
    {
        private readonly string LoginUrl = "";
        private readonly string SearchUrl = "";
        private readonly string DownloadUrl = "";
        private readonly string GuidUrl = "";

        private IWebClient client;
        private string cookieHeader = "";

        public MoreThanTV(IIndexerManagerService i, IWebClient c, Logger l)
            : base(name: "MoreThanTV",
                description: "ROMANIAN Private Torrent Tracker for TV / MOVIES, and the internal tracker for the release group DRACULA.",
                link: new Uri("https://www.morethan.tv"),
                caps: TorznabCapsUtil.CreateDefaultTorznabTVCaps(),
                manager: i,
                logger: l)
        {
            LoginUrl = SiteLink + "/login.php";
            SearchUrl = SiteLink + "/ajax.php?action=browse&searchstr=";
            DownloadUrl = SiteLink + "/torrents.php?action=download&id=";
            GuidUrl = SiteLink + "/torrents.php?torrentid=";
            client = c;
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
				{ "login", "Log in" },
				{ "keeplogged", "1" }
			};
            
            var loginResponse = await client.GetString(new Utils.Clients.WebRequest()
            {
                PostData = pairs,
                Url = LoginUrl,
                Type = RequestType.POST
            });
            
            if (loginResponse.Status == HttpStatusCode.Found)
            {
                cookieHeader = loginResponse.Cookies;
                loginResponse = await client.GetString(new Utils.Clients.WebRequest()
                {
                    Url = SiteLink.ToString(),
                    Cookies = cookieHeader
                });
            }

            if (!loginResponse.Content.Contains("logout.php?"))
            {
                CQ dom = loginResponse.Content;
                dom["#loginform > table"].Remove();
                var errorMessage = dom["#loginform"].Text().Trim().Replace("\n\t", " ");
                throw new ExceptionWithConfigData(errorMessage, (ConfigurationData)config);

            }
            else
            {
                var configSaveData = new JObject();
                configSaveData["cookie_header"] = cookieHeader;
                SaveConfig(configSaveData);
                IsConfigured = true;
            }
        }

        public void LoadFromSavedConfiguration(JToken jsonConfig)
        {
            cookieHeader = (string)jsonConfig["cookie_header"];
            IsConfigured = true;
        }

        private void FillReleaseInfoFromJson(ReleaseInfo release, JObject r)
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
            WebClientStringResult response = null; 

            // Their web server is fairly flakey - try up to three times.
            for(int i = 0; i < 3; i++)
            {
                try
                {
                    response = await client.GetString(new Utils.Clients.WebRequest()
                    {
                        Url = episodeSearchUrl,
                        Type = RequestType.GET,
                        Cookies = cookieHeader
                    });

                    break;
                }
                catch (Exception e){
                    logger.Error("On attempt " + (i+1) + " checking for results from MoreThanTv: " + e.Message );
                }
            }
            
            try
            {
                var json = JObject.Parse(response.Content);
                foreach (JObject r in json["response"]["results"])
                {
                    DateTime pubDate = DateTime.MinValue;
                    double dateNum;
                    if (double.TryParse((string)r["groupTime"], out dateNum))
                    {
                        pubDate = DateTimeUtil.UnixTimestampToDateTime(dateNum);
                        pubDate = DateTime.SpecifyKind(pubDate, DateTimeKind.Utc).ToLocalTime();
                    }

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
                OnParseError(response.Content, ex);
            }

            return releases.ToArray();
        }

        public async Task<byte[]> Download(Uri link)
        {
            var result = await client.GetBytes(new Utils.Clients.WebRequest()
            {
                Cookies = cookieHeader,
                Url = link.ToString(),
               Type = RequestType.GET
            });

            return result.Content;
        }
    }
}
