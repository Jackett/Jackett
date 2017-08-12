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
using System.Text.RegularExpressions;

namespace Jackett.Indexers
{
    public class FileList : BaseWebIndexer
    {
        string LoginUrl { get { return SiteLink + "takelogin.php"; } }
        string BrowseUrl { get { return SiteLink + "browse.php"; } }

        new ConfigurationDataFileList configData
        {
            get { return (ConfigurationDataFileList)base.configData; }
            set { base.configData = value; }
        }

        public FileList(IIndexerConfigurationService configService, IWebClient wc, Logger l, IProtectionService ps)
            : base(name: "FileList",
                description: "The best Romanian site.",
                link: "http://filelist.ro/",
                caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                configService: configService,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationDataFileList())
        {
            Encoding = Encoding.GetEncoding("UTF-8");
            Language = "ro-ro";
            Type = "private";

            TorznabCaps.SupportsImdbSearch = true;

            AddCategoryMapping(24, TorznabCatType.TVAnime); //Anime
            AddCategoryMapping(11, TorznabCatType.Audio); //Audio
            AddCategoryMapping(18, TorznabCatType.Other); //Misc
            AddCategoryMapping(16, TorznabCatType.Books); //Docs
            AddCategoryMapping(25, TorznabCatType.Movies3D); //Movies 3D
            AddCategoryMapping(20, TorznabCatType.MoviesBluRay); // Movies Blu-Ray
            AddCategoryMapping(2, TorznabCatType.MoviesDVD); //Movies DVD
            AddCategoryMapping(3, TorznabCatType.MoviesForeign); //Movies DVD-RO
            AddCategoryMapping(4, TorznabCatType.MoviesHD); //Movies HD
            AddCategoryMapping(19, TorznabCatType.MoviesForeign); //Movies HD-RO
            AddCategoryMapping(1, TorznabCatType.MoviesSD); //Movies SD
            AddCategoryMapping(10, TorznabCatType.Console); //Console Games
            AddCategoryMapping(9, TorznabCatType.PCGames); //PC Games
            AddCategoryMapping(17, TorznabCatType.PC); //Linux
            AddCategoryMapping(22, TorznabCatType.PCPhoneOther); //Apps/mobile
            AddCategoryMapping(8, TorznabCatType.PC); //Software
            AddCategoryMapping(21, TorznabCatType.TVHD); //TV HD
            AddCategoryMapping(23, TorznabCatType.TVSD); //TV SD
            AddCategoryMapping(13, TorznabCatType.TVSport); //Sport
            AddCategoryMapping(14, TorznabCatType.TV); //TV
            AddCategoryMapping(12, TorznabCatType.AudioVideo); //Music Video
            AddCategoryMapping(7, TorznabCatType.XXX); //XXX
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
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

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
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

            var queryCollection = new NameValueCollection();

            if (query.ImdbID != null)
            {
                queryCollection.Add("search", query.ImdbID);
            }
            else if (!string.IsNullOrWhiteSpace(searchString))
            {
                queryCollection.Add("search", searchString);
            }

            queryCollection.Add("cat", cat);
            queryCollection.Add("searchin", "0");
            queryCollection.Add("sort", "0");

            searchUrl += "?" + queryCollection.GetQueryString();

            var response = await RequestStringWithCookiesAndRetry(searchUrl, null, BrowseUrl);
            
            // Occasionally the cookies become invalid, login again if that happens
            if (response.IsRedirect)
            {
                await ApplyConfiguration(null);
                response = await RequestStringWithCookiesAndRetry(searchUrl, null, BrowseUrl);
            }

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
                    release.Title = qRow.Find(".torrenttable:eq(1) b").Text();
                    var longtitle = qRow.Find(".torrenttable:eq(1) a[title]").Attr("title");
                    if (!string.IsNullOrEmpty(longtitle) && !longtitle.Contains("<")) // releases with cover image have no full title
                        release.Title = longtitle;

                    if (query.ImdbID == null && !query.MatchQueryStringAND(release.Title))
                    continue;

                    release.Description = qRow.Find(".torrenttable:eq(1) > span > font.small").First().Text();

                    var tooltip = qTitleLink.Attr("title");
                    if (!string.IsNullOrEmpty(tooltip))
                    {
                        var ImgRegexp = new Regex("src='(.*?)'");
                        var ImgRegexpMatch = ImgRegexp.Match(tooltip);
                        if (ImgRegexpMatch.Success)
                            release.BannerUrl = new Uri(ImgRegexpMatch.Groups[1].Value);
                    }

                    release.Guid = new Uri(SiteLink + qTitleLink.Attr("href"));
                    release.Comments = release.Guid;

                    //22:05:3716/02/2013
                    var dateStr = qRow.Find(".torrenttable:eq(5)").Text().Trim()+" +0200";
                    release.PublishDate = DateTime.ParseExact(dateStr, "H:mm:ssdd/MM/yyyy zzz", CultureInfo.InvariantCulture);

                    var qLink = qRow.Find("a[href^=\"download.php?id=\"]").First();
                    release.Link = new Uri(SiteLink + qLink.Attr("href").Replace("&usetoken=1",""));

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
                    if (release.Category.Contains(TorznabCatType.MoviesForeign.ID) && !configData.IncludeRomanianReleases.Value)
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
