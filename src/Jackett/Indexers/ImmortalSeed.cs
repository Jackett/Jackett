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
    public class ImmortalSeed : BaseWebIndexer
    {
        private string BrowsePage { get { return SiteLink + "browse.php"; } }
        private string LoginUrl { get { return SiteLink + "takelogin.php"; } }
        private string QueryString { get { return "?do=search&keywords={0}&search_type=t_name&category=0&include_dead_torrents=no"; } }

        new ConfigurationDataBasicLogin configData
        {
            get { return (ConfigurationDataBasicLogin)base.configData; }
            set { base.configData = value; }
        }

        public ImmortalSeed(IIndexerConfigurationService configService, IWebClient wc, Logger l, IProtectionService ps)
            : base(name: "ImmortalSeed",
                description: "ImmortalSeed",
                link: "http://immortalseed.me/",
                caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                configService: configService,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLogin())
        {
            Encoding = Encoding.GetEncoding("UTF-8");
            Language = "en-us";
            Type = "private";

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

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value }
            };

            var response = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, LoginUrl);
            CQ resultDom = response.Content;

            await ConfigureIfOK(response.Cookies, response.Content.Contains("You have successfully logged in"), () =>
            {
                var errorMessage = response.Content;
                throw new ExceptionWithConfigData(errorMessage, configData);
            });

            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchUrl = BrowsePage;

            if (!string.IsNullOrWhiteSpace(query.GetQueryString()))
            {
                searchUrl += string.Format(QueryString, HttpUtility.UrlEncode(query.GetQueryString()));
            }

            var results = await RequestStringWithCookiesAndRetry(searchUrl);

            // Occasionally the cookies become invalid, login again if that happens
            if (results.Content.Contains("You do not have permission to access this page."))
            {
                await ApplyConfiguration(null);
                results = await RequestStringWithCookiesAndRetry(searchUrl);
            }

            try
            {
                CQ dom = results.Content;

                var rows = dom["#sortabletable tr:has(a[href*=\"details.php?id=\"])"];
                foreach (var row in rows.Skip(1))
                {
                    var release = new ReleaseInfo();
                    var qRow = row.Cq();

                    var qDetails = qRow.Find("div > a[href*=\"details.php?id=\"]"); // details link, release name get's shortened if it's to long
                    var qTitle = qRow.Find("td:eq(1) .tooltip-content div:eq(0)"); // use Title from tooltip
                    if (!qTitle.Any()) // fallback to Details link if there's no tooltip
                    {
                        qTitle = qDetails;
                    }
                    release.Title = qTitle.Text();

                    var qDesciption = qRow.Find(".tooltip-content > div");
                    if (qDesciption.Any())
                        release.Description = qDesciption.Get(1).InnerText.Trim();

                    var qLink = row.Cq().Find("td:eq(2) a:eq(1)");
                    release.Link = new Uri(qLink.Attr("href"));
                    release.Guid = release.Link;
                    release.Comments = new Uri(qDetails.Attr("href"));

                    // 07-22-2015 11:08 AM
                    var dateString = qRow.Find("td:eq(1) div").Last().Get(0).LastChild.ToString().Trim();
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

                    var grabs = qRow.Find("td:nth-child(6)").Text();
                    release.Grabs = ParseUtil.CoerceInt(grabs);

                    if (qRow.Find("img[title^=\"Free Torrent\"]").Length >= 1)
                        release.DownloadVolumeFactor = 0;
                    else if (qRow.Find("img[title^=\"Silver Torrent\"]").Length >= 1)
                        release.DownloadVolumeFactor = 0.5;
                    else
                        release.DownloadVolumeFactor = 1;

                    release.UploadVolumeFactor = 1;

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
