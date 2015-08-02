using CsQuery;
using Jackett.Models;
using Jackett.Models.IndexerConfig;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace Jackett.Indexers
{
    class FrenchTorrentDb : BaseIndexer, IIndexer
    {
        private string MainUrl { get { return SiteLink + "?section=INDEX"; } }
        private string SearchUrl { get { return SiteLink + "?section=TORRENTS&exact=1&name={0}&submit=GO"; } }

        public FrenchTorrentDb(IIndexerManagerService i, Logger l, IWebClient c)
            : base(name: "FrenchTorrentDb",
                description: "One the biggest French Torrent Tracker",
                link: "http://www.frenchtorrentdb.com/",
                caps: TorznabCapsUtil.CreateDefaultTorznabTVCaps(),
                manager: i,
                client: c,
                logger: l)
        {
        }

        public Task<ConfigurationData> GetConfigurationForSetup()
        {
            return Task.FromResult<ConfigurationData>(new ConfigurationDataUrl(SiteLink));
        }

        public async Task ApplyConfiguration(Newtonsoft.Json.Linq.JToken configJson)
        {
            var config = new ConfigurationDataBasicLoginFrenchTorrentDb();
            config.LoadValuesFromJson(configJson);
            var cookies = "WebsiteID=" + config.Cookie.Value;
            var response = await webclient.GetString(new Utils.Clients.WebRequest()
            {
                Url = MainUrl,
                Type = RequestType.GET,
                Cookies = cookies
            });

            await ConfigureIfOK(cookies, response.Content.Contains("/?section=LOGOUT"), () =>
            {
                throw new ExceptionWithConfigData("Failed to login", (ConfigurationData)config);
            });
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            var searchString = query.SanitizedSearchTerm + " " + query.GetEpisodeSearchString();
            var episodeSearchUrl = string.Format(SearchUrl, HttpUtility.UrlEncode(searchString));
            var response = await RequestStringWithCookiesAndRetry(episodeSearchUrl);
            try
            {
                CQ dom = response.Cookies;
                var rows = dom[".results_index ul"];
                foreach (var row in rows)
                {
                    var release = new ReleaseInfo();
                    CQ qRow = row.Cq();
                    CQ qLink = qRow.Find("li.torrents_name > .torrents_name_link").First();
                    CQ qDlLink = qRow.Find("li.torrents_download  > a").First();

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;
                    release.Title = qLink.Text().Trim();
                    release.Description = release.Title;
                    release.Comments = new Uri(SiteLink + "/" + qLink.Attr("href").TrimStart('/'));
                    release.Guid = release.Comments;
                    release.Link = new Uri(SiteLink + "/" + qDlLink.Attr("href").TrimStart('/'));
                    release.PublishDate = DateTime.Now;
                    release.Seeders = ParseUtil.CoerceInt(qRow.Find("li.torrents_seeders").Text());
                    release.Peers = ParseUtil.CoerceInt(qRow.Find("li.torrents_leechers").Text()) + release.Seeders;
                    var sizeParts = qRow.Find("li.torrents_size").Text();
                    release.Size = ReleaseInfo.GetBytes(sizeParts);

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
