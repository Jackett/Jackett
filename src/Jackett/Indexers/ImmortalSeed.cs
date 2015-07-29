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

namespace Jackett.Indexers
{
    public class ImmortalSeed : BaseIndexer, IIndexer
    {
        private string BrowsePage { get { return SiteLink + "browse.php"; } }
        private string LoginUrl { get { return SiteLink + "takelogin.php"; } }
        private string QueryString { get { return "?do=search&keywords={0}&search_type=t_name&category=0&include_dead_torrents=no"; } }

        public ImmortalSeed(IIndexerManagerService i, IWebClient wc, Logger l)
            : base(name: "ImmortalSeed",
                description: "ImmortalSeed",
                link: "http://immortalseed.me/",
                caps: TorznabCapsUtil.CreateDefaultTorznabTVCaps(),
                manager: i,
                client: wc,
                logger: l)
        {
            AddCategoryMapping(32, TorznabCatType.Anime);
            AddCategoryMapping(47, TorznabCatType.TVSD);
            AddCategoryMapping(8, TorznabCatType.TVHD);
            AddCategoryMapping(48, TorznabCatType.TVHD);
            AddCategoryMapping(9, TorznabCatType.TVSD);
            AddCategoryMapping(4, TorznabCatType.TVHD);
            AddCategoryMapping(6, TorznabCatType.TVSD);

            AddCategoryMapping(22, TorznabCatType.Books);
            AddCategoryMapping(41, TorznabCatType.Comic);
            AddCategoryMapping(23, TorznabCatType.Apps);

            AddCategoryMapping(16, TorznabCatType.MoviesHD);
            AddCategoryMapping(17, TorznabCatType.MoviesSD);
            AddCategoryMapping(14, TorznabCatType.MoviesSD);
            AddCategoryMapping(34, TorznabCatType.MoviesForeign);
            AddCategoryMapping(18, TorznabCatType.MoviesForeign);
            AddCategoryMapping(33, TorznabCatType.MoviesForeign);

            AddCategoryMapping(34, TorznabCatType.Audio);
            AddCategoryMapping(37, TorznabCatType.AudioLossless);
            AddCategoryMapping(35, TorznabCatType.AudioBooks);
            AddCategoryMapping(36, TorznabCatType.AudioLossy);

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
                { "password", incomingConfig.Password.Value }
            };
            var request = new Utils.Clients.WebRequest()
            {
                Url = LoginUrl,
                Type = RequestType.POST,
                Referer = SiteLink,
                PostData = pairs
            };
            var response = await webclient.GetString(request);
            CQ splashDom = response.Content;
            var link = splashDom[".trow2 a"].First();
            var resultPage = await RequestStringWithCookies(link.Attr("href"), response.Cookies);
            CQ resultDom = resultPage.Content;

            ConfigureIfOK(response.Cookies, resultPage.Content.Contains("/logout.php"), () =>
            {
                var tries = resultDom["#main tr:eq(1) td font"].First().Text();
                var errorMessage = "Incorrect username or password! " + tries + " tries remaining.";
                throw new ExceptionWithConfigData(errorMessage, (ConfigurationData)incomingConfig);
            });
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.SanitizedSearchTerm + " " + query.GetEpisodeSearchString();
            var searchUrl = BrowsePage;

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchUrl += string.Format(QueryString, HttpUtility.UrlEncode(searchString));
            }

            var results = await RequestStringWithCookiesAndRetry(searchUrl);

            try
            {
                CQ dom = results.Content;

                var rows = dom["#sortabletable tr"];
                foreach (var row in rows.Skip(1))
                {
                    var release = new ReleaseInfo();
                    var qRow = row.Cq();
                    release.Title = qRow.Find(".tooltip-content div").First().Text();
                    release.Description = qRow.Find(".tooltip-content div").Get(1).InnerText.Trim();

                    var qLink = row.Cq().Find("td:eq(2) a:eq(1)");
                    release.Link = new Uri(qLink.Attr("href"));
                    release.Guid = release.Link;
                    release.Comments = new Uri(qRow.Find(".tooltip-target a").First().Attr("href"));

                    // 07-22-2015 11:08 AM
                    var dateString = qRow.Find("td:eq(1) div").Last().Children().Remove().End().Text().Trim();
                    release.PublishDate = DateTime.ParseExact(dateString, "MM-dd-yyyy hh:mm tt", CultureInfo.InvariantCulture);

                    var sizeStr = qRow.Find("td:eq(4)").Text().Trim();
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    release.Seeders = ParseUtil.CoerceInt(qRow.Find("td:eq(6)").Text().Trim());
                    release.Peers = ParseUtil.CoerceInt(qRow.Find("td:eq(7)").Text().Trim()) + release.Seeders;

                    var catLink = row.Cq().Find("td:eq(0) a").First().Attr("href");
                    var catSplit = catLink.IndexOf("category=");
                    if (catSplit > -1)
                    {
                        catLink = catLink.Substring(catSplit + 9);
                    }

                    release.Category = MapTrackerCatToNewznab(catLink);
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
