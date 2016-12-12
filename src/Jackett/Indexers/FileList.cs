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
using Jackett.Models.IndexerConfig.Bespoke;

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
            Encoding = Encoding.GetEncoding("UTF-8");
            Language = "ro-ro";

            AddCategoryMapping(24, TorznabCatType.TVAnime);
            AddCategoryMapping(11, TorznabCatType.Audio);
            AddCategoryMapping(15, TorznabCatType.TV);
            AddCategoryMapping(18, TorznabCatType.Other);
            AddCategoryMapping(16, TorznabCatType.TVDocumentary);
            AddCategoryMapping(25, TorznabCatType.Movies3D);
            AddCategoryMapping(20, TorznabCatType.MoviesBluRay);
            AddCategoryMapping(2, TorznabCatType.MoviesSD);
            AddCategoryMapping(3, TorznabCatType.MoviesForeign); //RO
            AddCategoryMapping(4, TorznabCatType.MoviesHD);
            AddCategoryMapping(19, TorznabCatType.MoviesForeign); // RO
            AddCategoryMapping(1, TorznabCatType.MoviesSD);
            AddCategoryMapping(10, TorznabCatType.Console);
            AddCategoryMapping(9, TorznabCatType.PCGames);
            AddCategoryMapping(17, TorznabCatType.PC);
            AddCategoryMapping(22, TorznabCatType.PCPhoneOther); //Apps/mobile
            AddCategoryMapping(8, TorznabCatType.PC);
            AddCategoryMapping(21, TorznabCatType.TVHD);
            AddCategoryMapping(23, TorznabCatType.TVSD);
            AddCategoryMapping(13, TorznabCatType.TVSport);
            AddCategoryMapping(14, TorznabCatType.TV);
            AddCategoryMapping(12, TorznabCatType.AudioVideo);
            AddCategoryMapping(7, TorznabCatType.XXX);
        }

        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
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
            return IndexerConfigurationStatus.RequiresTesting;
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchUrl = BrowseUrl;
            var searchString = query.GetQueryString();

            var cats = MapTorznabCapsToTrackers(query);
            string cat = "0";
            if (cats.Count == 1)
            {
                cat = cats[0];
            }

            if (!string.IsNullOrWhiteSpace(searchString) || cat != "0")
                searchUrl += string.Format("?search={0}&cat={1}&searchin=0&sort=0", HttpUtility.UrlEncode(searchString), cat);



            var response = await RequestStringWithCookiesAndRetry(searchUrl, null, BrowseUrl);
            var results = response.Content;
            try
            {
                CQ dom = results;
                var globalFreeLeech = dom.Find("div.globalFreeLeech").Any();
                var rows = dom[".torrentrow"];
                foreach (var row in rows)
                {
                    var release = new ReleaseInfo();
                    var qRow = row.Cq();
                    var qTitleLink = qRow.Find(".torrenttable:eq(1) a").First();
                    release.Title = qRow.Find(".torrenttable:eq(1) a").Attr("title");
                    release.Description = release.Title;
                    release.Guid = new Uri(SiteLink + qTitleLink.Attr("href"));
                    release.Comments = release.Guid;

                    //22:05:3716/02/2013
                    var dateStr = qRow.Find(".torrenttable:eq(5)").Text().Trim()+" +0200";
                    release.PublishDate = DateTime.ParseExact(dateStr, "H:mm:ssdd/MM/yyyy zzz", CultureInfo.InvariantCulture);

                    var qLink = qRow.Find("a[href^=\"download.php?id=\"]").First();
                    release.Link = new Uri(SiteLink + qLink.Attr("href"));

                    var sizeStr = qRow.Find(".torrenttable:eq(6)").Text().Trim();
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    release.Seeders = ParseUtil.CoerceInt(qRow.Find(".torrenttable:eq(8)").Text().Trim());
                    release.Peers = ParseUtil.CoerceInt(qRow.Find(".torrenttable:eq(9)").Text().Trim()) + release.Seeders;

                    var catId = qRow.Find(".torrenttable:eq(0) a").First().Attr("href").Substring(15);
                    release.Category = MapTrackerCatToNewznab(catId);

                    var grabs = qRow.Find(".torrenttable:eq(7)").First().Get(0).FirstChild;
                    release.Grabs = ParseUtil.CoerceLong(catId);

                    if (globalFreeLeech || row.Cq().Find("img[alt=\"FreeLeech\"]").Any())
                        release.DownloadVolumeFactor = 0;
                    else
                        release.DownloadVolumeFactor = 1;

                    release.UploadVolumeFactor = 1;

                    // Skip Romanian releases
                    if (release.Category == TorznabCatType.MoviesForeign.ID && !configData.IncludeRomanianReleases.Value)
                        continue;

                    releases.Add(release);
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
