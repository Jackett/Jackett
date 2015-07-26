using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jackett.Models;
using Newtonsoft.Json.Linq;
using Jackett.Utils.Clients;
using Jackett.Services;
using NLog;
using Jackett.Utils;
using CsQuery;
using System.Web;

namespace Jackett.Indexers
{
    public class Pretome : BaseIndexer, IIndexer
    {

        class PretomeConfiguration : ConfigurationDataBasicLogin
        {
            public StringItem Pin { get; private set; }

            public PretomeConfiguration() : base()
            {
                Pin = new StringItem { Name = "Login Pin Number" };
            }

            public override Item[] GetItems()
            {
                return new Item[] { Pin, Username, Password };
            }
        }

        private readonly string LoginUrl = "";
        private readonly string LoginReferer = "";
        private readonly string SearchUrl = "";
        private string cookieHeader = "";

        private IWebClient webclient;

        public Pretome(IIndexerManagerService i, IWebClient wc, Logger l)
            : base(name: "PrivateHD",
                description: "BitTorrent site for High Quality, High Definition (HD) movies and TV Shows",
                link: new Uri("https://pretome.info"),
                caps: TorznabCapsUtil.CreateDefaultTorznabTVCaps(),
                manager: i,
                logger: l)
        {
            LoginUrl = SiteLink + "takelogin.php";
            LoginReferer = SiteLink + "index.php?cat=1";
            SearchUrl = SiteLink + "browse.php?tags=&st=1&tf=all&cat%5B%5D=7&search={0}";
            webclient = wc;
        }

        public Task<ConfigurationData> GetConfigurationForSetup()
        {
            var config = new PretomeConfiguration();
            return Task.FromResult<ConfigurationData>(config);
        }

        public async Task ApplyConfiguration(JToken configJson)
        {
            var config = new PretomeConfiguration();
            config.LoadValuesFromJson(configJson);

            var loginPage = await webclient.GetString(new WebRequest()
            {
                Url = LoginUrl,
                Type = RequestType.GET
            });

            var pairs = new Dictionary<string, string> {
                { "returnto", "%2F" },
                { "login_pin", config.Pin.Value },
                { "username", config.Username.Value },
                { "password", config.Password.Value },
                { "login", "Login" }
            };


            // Send Post
            var loginPost = await webclient.GetString(new WebRequest()
            {
                Url = LoginUrl,
                PostData = pairs,
                Referer = LoginReferer,
                Type = RequestType.POST,
                Cookies = loginPage.Cookies
            });

            if (loginPost.RedirectingTo == null)
            {
                throw new ExceptionWithConfigData("Login failed. Did you use the PIN number that pretome emailed you?", (ConfigurationData)config);
            }

            // Get result from redirect
            var loginResult = await webclient.GetString(new WebRequest()
            {
                Url = loginPost.RedirectingTo,
                Type = RequestType.GET,
                Cookies = loginPost.Cookies
            });

            if (!loginResult.Content.Contains("logout.php"))
            {
                throw new ExceptionWithConfigData("Failed", (ConfigurationData)config);
            }
            else
            {
                cookieHeader = loginPost.Cookies;
                var configSaveData = new JObject();
                configSaveData["cookies"] = cookieHeader;
                SaveConfig(configSaveData);
                IsConfigured = true;
            }
        }

        public void LoadFromSavedConfiguration(JToken jsonConfig)
        {
            cookieHeader = (string)jsonConfig["cookies"];
            IsConfigured = true;
        }

        public async Task<byte[]> Download(Uri link)
        {
            var response = await webclient.GetBytes(new WebRequest()
            {
                Url = link.ToString(),
                Cookies = cookieHeader
            });
            return response.Content;
        }

        public async Task<ReleaseInfo[]> PerformQuery(TorznabQuery query)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            var searchString = query.SanitizedSearchTerm + " " + query.GetEpisodeSearchString();
            var episodeSearchUrl = string.Format(SearchUrl, HttpUtility.UrlEncode(searchString));

            var response = await webclient.GetString(new WebRequest()
            {
                Url = episodeSearchUrl,
                Referer = SiteLink.ToString(),
                Cookies = cookieHeader
            });
            var results = response.Content;

            try
            {
                CQ dom = results;
                var rows = dom["table > tbody > tr.browse"];
                foreach (var row in rows)
                {
                    CQ qRow = row.Cq();
                    var release = new ReleaseInfo();

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;

                    var qLink = row.ChildElements.ElementAt(1).Cq().Find("a").First();
                    release.Title = qLink.Text().Trim();
                    release.Comments = new Uri(SiteLink + qLink.Attr("href"));
                    release.Guid = release.Comments;
                    
                    var qDownload = row.ChildElements.ElementAt(2).Cq().Find("a").First();
                    release.Link = new Uri(SiteLink + qDownload.Attr("href"));

                    var dateStr = row.ChildElements.ElementAt(5).InnerHTML.Replace("<br>", " ");
                    release.PublishDate = DateTimeUtil.FromTimeAgo(dateStr);

                    var sizeStr = row.ChildElements.ElementAt(7).Cq().Text();
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    release.Seeders = ParseUtil.CoerceInt(row.ChildElements.ElementAt(9).InnerText);
                    release.Peers = ParseUtil.CoerceInt(row.ChildElements.ElementAt(10).InnerText) + release.Seeders;

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
