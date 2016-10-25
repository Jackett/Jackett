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
    public class ImmortalSeed : BaseIndexer, IIndexer
    {
        private string BrowsePage { get { return SiteLink + "browse.php"; } }
        private string LoginUrl { get { return SiteLink + "takelogin.php"; } }
        private string QueryString { get { return "?do=search&keywords={0}&search_type=t_name&category=0&include_dead_torrents=no"; } }

        new ConfigurationDataBasicLogin configData
        {
            get { return (ConfigurationDataBasicLogin)base.configData; }
            set { base.configData = value; }
        }

        public ImmortalSeed(IIndexerManagerService i, IWebClient wc, Logger l, IProtectionService ps)
            : base(name: "ImmortalSeed",
                description: "ImmortalSeed",
                link: "http://immortalseed.me/",
                caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                manager: i,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLogin())
        {
            AddCategoryMapping(32, TorznabCatType.TVAnime);
            AddCategoryMapping(47, TorznabCatType.TVSD);
            AddCategoryMapping(8, TorznabCatType.TVHD);
            AddCategoryMapping(48, TorznabCatType.TVHD);
            AddCategoryMapping(9, TorznabCatType.TVSD);
            AddCategoryMapping(4, TorznabCatType.TVHD);
            AddCategoryMapping(6, TorznabCatType.TVSD);

            AddCategoryMapping(22, TorznabCatType.Books);
            AddCategoryMapping(41, TorznabCatType.BooksComics);
            AddCategoryMapping(23, TorznabCatType.PC);

            AddCategoryMapping(16, TorznabCatType.MoviesHD);
            AddCategoryMapping(17, TorznabCatType.MoviesSD);
            AddCategoryMapping(14, TorznabCatType.MoviesSD);
            AddCategoryMapping(34, TorznabCatType.MoviesForeign);
            AddCategoryMapping(18, TorznabCatType.MoviesForeign);
            AddCategoryMapping(33, TorznabCatType.MoviesForeign);

            AddCategoryMapping(34, TorznabCatType.Audio);
            AddCategoryMapping(37, TorznabCatType.AudioLossless);
            AddCategoryMapping(35, TorznabCatType.AudioAudiobook);
            AddCategoryMapping(36, TorznabCatType.AudioMP3);

        }

        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value }
            };

            var response = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, LoginUrl);
            CQ resultDom = response.Content;

            await ConfigureIfOK(response.Cookies, response.Content.Contains("/logout.php"), () =>
            {
                var errorMessage = response.Content;
                throw new ExceptionWithConfigData(errorMessage, configData);
            });

            return IndexerConfigurationStatus.RequiresTesting;
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchUrl = BrowsePage;

            if (!string.IsNullOrWhiteSpace(query.GetQueryString()))
            {
                searchUrl += string.Format(QueryString, HttpUtility.UrlEncode(query.GetQueryString()));
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
                    if (string.IsNullOrWhiteSpace(release.Title))
                        continue;
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
