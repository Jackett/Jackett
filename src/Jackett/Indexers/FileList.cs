using CsQuery;
using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
    public class FileList : BaseIndexer, IIndexer
    {
        string LoginUrl { get { return SiteLink + "takelogin.php"; } }
        string BrowseUrl { get { return SiteLink + "browse.php"; } }

        new ConfigurationDataFileList configData
        {
            get { return (ConfigurationDataFileList)base.configData; }
            set { base.configData = value; }
        }

        public FileList(IIndexerManagerService i, IWebClient wc, Logger l, IProtectionService ps)
            : base(name: "FileList",
                description: "The best Romanian site.",
                link: "http://filelist.ro/",
                caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                manager: i,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationDataFileList())
        {
            AddCategoryMapping(24, TorznabCatType.Anime);
            AddCategoryMapping(11, TorznabCatType.Audio);
            AddCategoryMapping(15, TorznabCatType.TV);
            //AddCategoryMapping(18, TorznabCatType.); Other
            AddCategoryMapping(16, TorznabCatType.TVDocs);
            AddCategoryMapping(25, TorznabCatType.Movies3D);
            AddCategoryMapping(20, TorznabCatType.MoviesBlueRay);
            AddCategoryMapping(2, TorznabCatType.MoviesSD);
            AddCategoryMapping(3, TorznabCatType.MoviesForeign); //RO
            AddCategoryMapping(4, TorznabCatType.MoviesHD);
            AddCategoryMapping(19, TorznabCatType.MoviesForeign); // RO
            AddCategoryMapping(1, TorznabCatType.MoviesSD);
            AddCategoryMapping(10, TorznabCatType.Consoles);
            AddCategoryMapping(9, TorznabCatType.PCGames);
            //AddCategoryMapping(17, TorznabCatType); Linux No cat
            AddCategoryMapping(22, TorznabCatType.AppsMobile); //Apps/mobile
            AddCategoryMapping(8, TorznabCatType.Apps);
            AddCategoryMapping(21, TorznabCatType.TVHD);
            AddCategoryMapping(23, TorznabCatType.TVSD);
            AddCategoryMapping(13, TorznabCatType.TVSport);
            AddCategoryMapping(14, TorznabCatType.TV);
            AddCategoryMapping(12, TorznabCatType.AudioMusicVideos);
            AddCategoryMapping(7, TorznabCatType.XXX);
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
                var errorMessage = dom[".main"].Text().Trim();
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.SanitizedSearchTerm + " " + query.GetEpisodeSearchString();
            var searchUrl = BrowseUrl;

            if (!string.IsNullOrWhiteSpace(searchString))
                searchUrl += string.Format("?search={0}&cat=0&searchin=0&sort=0", HttpUtility.UrlEncode(searchString));

            var response = await RequestStringWithCookiesAndRetry(searchUrl, null, BrowseUrl);
            var results = response.Content;
            try
            {
                CQ dom = results;
                var rows = dom[".torrentrow"];
                foreach (var row in rows)
                {
                    var release = new ReleaseInfo();
                    var qRow = row.Cq();
                    var qTitleLink = qRow.Find(".torrenttable:eq(1) a").First();
                    release.Title = qRow.Find(".torrenttable:eq(1) a b").Text().Trim();
                    release.Description = release.Title;
                    release.Guid = new Uri(SiteLink + qTitleLink.Attr("href"));
                    release.Comments = release.Guid;

                    //22:05:3716/02/2013
                    var dateStr = qRow.Find(".torrenttable:eq(5)").Text().Trim();
                    release.PublishDate = DateTime.ParseExact(dateStr, "H:mm:ssdd/MM/yyyy", CultureInfo.InvariantCulture);

                    var qLink = qRow.Find(".torrenttable:eq(2) a").First();
                    release.Link = new Uri(SiteLink + qLink.Attr("href"));

                    var sizeStr = qRow.Find(".torrenttable:eq(6)").Text().Trim();
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    release.Seeders = ParseUtil.CoerceInt(qRow.Find(".torrenttable:eq(8)").Text().Trim());
                    release.Peers = ParseUtil.CoerceInt(qRow.Find(".torrenttable:eq(9)").Text().Trim()) + release.Seeders;

                    var cat = qRow.Find(".torrenttable:eq(0) a").First().Attr("href").Substring(15);
                    release.Category = MapTrackerCatToNewznab(cat);

                    // Skip other
                    if (release.Category != 0)
                    {
                        // Skip Romanian releases
                        if (release.Category == TorznabCatType.MoviesForeign.ID && !configData.IncludeRomanianReleases.Value)
                            continue;

                        releases.Add(release);
                    }
                }
            }
            catch (Exception ex)
            {
                OnParseError(results, ex);
            }

            return releases;
        }
    }
}
