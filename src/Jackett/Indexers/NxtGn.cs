using CsQuery;
using Jackett.Indexers;
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
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI.WebControls;
using Jackett.Models.IndexerConfig;

namespace Jackett.Indexers
{
    public class NxtGn : BaseIndexer, IIndexer
    {
        private string LoginUrl { get { return SiteLink + "login.php"; } }
        private string SearchUrl { get { return SiteLink + "browse.php"; } }
        private string ProfileUrl { get { return SiteLink + "my.php"; } }

        new ConfigurationDataBasicLoginWithRSS configData
        {
            get { return (ConfigurationDataBasicLoginWithRSS)base.configData; }
            set { base.configData = value; }
        }

        public NxtGn(IIndexerManagerService i, Logger l, IWebClient c, IProtectionService ps)
            : base(name: "NextGen",
                description: "A danish closed torrent tracker",
                link: "https://nxtgn.org/",
                caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                manager: i,
                client: c,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLoginWithRSS())
        {
            AddCategoryMapping(47, TorznabCatType.Movies3D);
            AddCategoryMapping(38, TorznabCatType.MoviesHD);
            AddCategoryMapping(38, TorznabCatType.MoviesWEBDL);
            AddCategoryMapping(38, TorznabCatType.MoviesBluRay);
            AddCategoryMapping(5, TorznabCatType.MoviesSD);
            AddCategoryMapping(23, TorznabCatType.MoviesForeign);
            AddCategoryMapping(22, TorznabCatType.MoviesSD);
            //AddCategoryMapping(4, TorznabCatType.TVFOREIGN);
            //AddCategoryMapping(4, TorznabCatType.TVSD);
            //AddCategoryMapping(4, TorznabCatType.TVDocumentary);
            //AddCategoryMapping(4, TorznabCatType.TVSport);
            //AddCategoryMapping(4, TorznabCatType.TV);
            //AddCategoryMapping(31, TorznabCatType.TVHD);
            //AddCategoryMapping(21, TorznabCatType.TVFOREIGN);
            AddCategoryMapping(46, TorznabCatType.TV);
            AddCategoryMapping(46, TorznabCatType.TVHD);
            //AddCategoryMapping(45, TorznabCatType.TV);
            //AddCategoryMapping(45, TorznabCatType.TVSD);
            //AddCategoryMapping(24, TorznabCatType.TVFOREIGN);
            AddCategoryMapping(26, TorznabCatType.TV);
            AddCategoryMapping(26, TorznabCatType.TVHD);
            AddCategoryMapping(26, TorznabCatType.TVWEBDL);
            AddCategoryMapping(33, TorznabCatType.MoviesHD);
            AddCategoryMapping(33, TorznabCatType.Movies);
            AddCategoryMapping(17, TorznabCatType.MoviesForeign);
            AddCategoryMapping(17, TorznabCatType.MoviesDVD);
            AddCategoryMapping(9, TorznabCatType.MoviesHD);
            AddCategoryMapping(9, TorznabCatType.Movies);
            AddCategoryMapping(9, TorznabCatType.MoviesBluRay);
            AddCategoryMapping(43, TorznabCatType.TV);
            AddCategoryMapping(43, TorznabCatType.TVHD);
            AddCategoryMapping(43, TorznabCatType.TVWEBDL);
        }

        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            var loginPage = await RequestStringWithCookies(LoginUrl);
            CQ loginDom = loginPage.Content;
            var loginPostUrl = loginDom["#login"].Attr("action");

            configData.LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value }
            };
            // Get inital cookies
            CookieHeader = string.Empty;
            var response = await RequestLoginAndFollowRedirect(SiteLink + loginPostUrl, pairs, CookieHeader, true, null, LoginUrl);

            await ConfigureIfOK(response.Cookies, response.Content != null && response.Content.Contains("Velkommen tilbage"), () =>
            {
                CQ dom = response.Content;
                var messageEl = dom["inputs"];
                var errorMessage = messageEl.Text().Trim();
                throw new ExceptionWithConfigData(errorMessage, configData);
            });

            var profilePage = await RequestStringWithCookies(ProfileUrl, response.Cookies);
            CQ profileDom = profilePage.Content;
            var passKey = profileDom["input[name=resetkey]"].Parent().Text();
            passKey = passKey.Substring(0, passKey.IndexOf(' '));
            configData.RSSKey.Value = passKey;
            SaveConfig();
            return IndexerConfigurationStatus.RequiresTesting;
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var breakWhile = false;
            var page = 0;
            while (page < 3)
            {
                string episodeSearchUrl;
                if (string.IsNullOrEmpty(query.GetQueryString()))
                {
                    episodeSearchUrl = SearchUrl + "?page=" + page;
                    breakWhile = true;
                }
                else
                {
                    var cats = MapTorznabCapsToTrackers(query);
                    var catsUrlPart = string.Join("&", cats.Select(c => $"c{c}=1"));
                    episodeSearchUrl = string.Format("{0}?search={1}&cat=0&incldead=0&{2}&page={3}", SearchUrl, HttpUtility.UrlEncode(query.GetQueryString()), catsUrlPart, page);
                }
                page++;
                var results = await RequestStringWithCookiesAndRetry(episodeSearchUrl);
                try
                {
                    CQ dom = results.Content;

                    var rows = dom["#torrent-table-wrapper > div"];

                    foreach (var row in rows.Skip(1))
                    {
                        var release = new ReleaseInfo();

                        var qRow = row.Cq();
                        var qLink = qRow.Find("#torrent-udgivelse2-users > a").First();
                        var qDesc = qRow.Find("#torrent-udgivelse2-users > p").FirstOrDefault();

                        var moviesCats = new[] { 47, 38, 5, 23, 22, 33, 17, 9 };
                        var seriesCats = new[] { 46, 26, 43 };
                        var catUrl = qRow.Find(".torrent-icon > a").Attr("href");
                        var cat = catUrl.Substring(catUrl.LastIndexOf('=') + 1);
                        var catNo = int.Parse(cat);
                        if (moviesCats.Contains(catNo))
                            release.Category = TorznabCatType.Movies.ID;
                        else if (seriesCats.Contains(catNo))
                            release.Category = TorznabCatType.TV.ID;
                        else
                            continue;

                        releases.Add(release);

                        var torrentUrl = qLink.Attr("href");
                        var torrentId = torrentUrl.Substring(torrentUrl.LastIndexOf('=') + 1);

                        release.MinimumRatio = 1;
                        release.MinimumSeedTime = 172800;
                        release.Title = qLink.Attr("title");
                        release.Description = qDesc != null ? qDesc.InnerText : release.Title;
                        release.Guid = new Uri(SiteLink + torrentUrl);
                        release.Comments = new Uri(release.Guid + "#startcomments");

                        var downloadUrl = $"{SiteLink}download.php?id={torrentId}&rss&passkey={configData.RSSKey.Value}";
                        release.Link = new Uri(downloadUrl);

                        var qAdded = qRow.Find("#torrent-added").First();
                        var addedStr = qAdded.Text().Trim();
                        release.PublishDate = DateTime.ParseExact(addedStr, "dd-MM-yyyyHH:mm:ss", CultureInfo.InvariantCulture);

                        release.Seeders = ParseUtil.CoerceInt(qRow.Find("#torrent-seeders").Text().Trim());
                        release.Peers = ParseUtil.CoerceInt(qRow.Find("#torrent-leechers").Text().Trim()) + release.Seeders;

                        var sizeStr = qRow.Find("#torrent-size").First().Text();
                        release.Size = ReleaseInfo.GetBytes(sizeStr);

                        var infoLink = qRow.Find("#infolink");
                        var linkContainer = infoLink.Children().First().Children().First();
                        var url = linkContainer.Attr("href");
                        var img = linkContainer.Children().First();
                        var imgUrl = img.Attr("src");
                        if (imgUrl == "/pic/imdb.png")
                        {
                            release.Imdb = long.Parse(url.Substring(url.LastIndexOf('t') + 1));
                        }
                        else if (imgUrl == "/pic/TV.png")
                        {
                            release.TheTvDbId = long.Parse(url.Substring(url.LastIndexOf('=') + 1));
                        }
                    }
                    var nextPage = dom["#torrent-table-wrapper + p[align=center]"].Children().Last();
                    if (!nextPage.Is("a"))
                        breakWhile = true;
                }
                catch (Exception ex)
                {
                    OnParseError(results.Content, ex);
                }
                if (breakWhile)
                    break;
            }
            return releases;
        }
    }
}
