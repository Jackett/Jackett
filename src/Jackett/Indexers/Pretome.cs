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
using Jackett.Models.IndexerConfig;

namespace Jackett.Indexers
{
    public class Pretome : BaseIndexer, IIndexer
    {
        private string LoginUrl { get { return SiteLink + "takelogin.php"; } }
        private string LoginReferer { get { return SiteLink + "index.php?cat=1"; } }
        private string SearchUrl { get { return SiteLink + "browse.php?tags=&st=1&tf=all&cat%5B%5D=7&search={0}"; } }

        new ConfigurationDataPinNumber configData
        {
            get { return (ConfigurationDataPinNumber)base.configData; }
            set { base.configData = value; }
        }

        public Pretome(IIndexerManagerService i, IWebClient wc, Logger l, IProtectionService ps)
            : base(name: "PreToMe",
                description: "BitTorrent site for High Quality, High Definition (HD) movies and TV Shows",
                link: "https://pretome.info/",
                caps: TorznabCapsUtil.CreateDefaultTorznabTVCaps(),
                client: wc,
                manager: i,
                logger: l,
                p: ps,
                configData: new ConfigurationDataPinNumber())
        {
        }

        public async Task ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);

            var loginPage = await RequestStringWithCookies(LoginUrl, string.Empty);

            var pairs = new Dictionary<string, string> {
                { "returnto", "%2F" },
                { "login_pin", configData.Pin.Value },
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "login", "Login" }
            };

            // Send Post
            var result = await PostDataWithCookies(LoginUrl, pairs, loginPage.Cookies);
            if (result.RedirectingTo == null)
            {
                throw new ExceptionWithConfigData("Login failed. Did you use the PIN number that pretome emailed you?", configData);
            }
            var loginCookies = result.Cookies;
            // Get result from redirect
            await FollowIfRedirect(result, LoginUrl, null, loginCookies);

            await ConfigureIfOK(loginCookies, result.Content != null && result.Content.Contains("logout.php"), () =>
            {
                CookieHeader = string.Empty;
                throw new ExceptionWithConfigData("Failed", configData);
            });
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.SanitizedSearchTerm + " " + query.GetEpisodeSearchString();
            var episodeSearchUrl = string.Format(SearchUrl, HttpUtility.UrlEncode(searchString));

            var response = await RequestStringWithCookiesAndRetry(episodeSearchUrl);

            try
            {
                CQ dom = response.Content;
                var rows = dom["table > tbody > tr.browse"];
                foreach (var row in rows)
                {
                    CQ qRow = row.Cq();
                    var release = new ReleaseInfo();

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;

                    var qLink = row.ChildElements.ElementAt(1).Cq().Find("a").First();
                    release.Title = qLink.Text().Trim();
                    if (qLink.Find("span").Count() == 1 && release.Title.StartsWith("NEW! |"))
                    {
                        release.Title = release.Title.Substring(6).Trim();
                    }

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
                OnParseError(response.Content, ex);
            }
            return releases;
        }
    }
}
