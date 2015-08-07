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
using System.Threading.Tasks;
using System.Web;
using Jackett.Models.IndexerConfig;

namespace Jackett.Indexers
{
    public class SceneTime : BaseIndexer, IIndexer
    {
        private string LoginUrl { get { return SiteLink + "takelogin.php"; } }
        private string SearchUrl { get { return SiteLink + "browse_API.php"; } }
        private string DownloadUrl { get { return SiteLink + "download.php/{0}/download.torrent"; } }

        new ConfigurationDataBasicLogin configData
        {
            get { return (ConfigurationDataBasicLogin)base.configData; }
            set { base.configData = value; }
        }

        public SceneTime(IIndexerManagerService i, Logger l, IWebClient w, IProtectionService ps)
            : base(name: "SceneTime",
                description: "Always on time",
                link: "https://www.scenetime.com/",
                caps: TorznabCapsUtil.CreateDefaultTorznabTVCaps(),
                manager: i,
                client: w,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLogin())
        {
        }

        public async Task ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value }
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, LoginUrl);
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("logout.php"), () =>
            {
                CQ dom = result.Content;
                var errorMessage = dom["td.text"].Text().Trim();
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
        }

        private Dictionary<string, string> GetSearchFormData(string searchString)
        {
            return new Dictionary<string, string> {
                { "c2", "1" }, { "c43", "1" }, { "c9", "1" }, { "c63", "1" }, { "c77", "1" }, { "c100", "1" }, { "c101", "1" },
                { "cata", "yes" }, { "sec", "jax" },
                { "search", searchString}
            };
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.SanitizedSearchTerm + " " + query.GetEpisodeSearchString();
            var results = await PostDataWithCookiesAndRetry(SearchUrl, GetSearchFormData(searchString));

            try
            {
                CQ dom = results.Content;
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
                    var timezoneString = Environment.OSVersion.Platform == PlatformID.Unix ? "Europe/Berlin" : "Central European Standard Time";
                    var localDate = TimeZoneInfo.ConvertTimeToUtc(euDate, TimeZoneInfo.FindSystemTimeZoneById(timezoneString)).ToLocalTime();
                    release.PublishDate = localDate;

                    var sizeStr = row.ChildElements.ElementAt(5).Cq().Text();
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    release.Seeders = ParseUtil.CoerceInt(row.ChildElements.ElementAt(6).Cq().Text().Trim());
                    release.Peers = ParseUtil.CoerceInt(row.ChildElements.ElementAt(7).Cq().Text().Trim()) + release.Seeders;

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(results.Content, ex);
            }
            return releases;
        }
    }
}
