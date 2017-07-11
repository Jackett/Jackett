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
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Jackett.Models.IndexerConfig;
using System.Web;

namespace Jackett.Indexers
{
    class SceneAccess : BaseWebIndexer
    {
        private string LoginUrl { get { return SiteLink + "login"; } }
        private string SearchUrl { get { return SiteLink + "all?search={0}&method=2"; } }

        new ConfigurationDataBasicLogin configData
        {
            get { return (ConfigurationDataBasicLogin)base.configData; }
            set { base.configData = value; }
        }

        public SceneAccess(IIndexerConfigurationService configService, IWebClient c, Logger l, IProtectionService ps)
            : base(name: "SceneAccess",
                description: "Your gateway to the scene",
                link: "https://sceneaccess.eu/",
                caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                configService: configService,
                client: c,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLogin())
        {
            Encoding = Encoding.GetEncoding("UTF-8");
            Language = "en-us";
            Type = "private";

            AddCategoryMapping(8, TorznabCatType.MoviesSD);
            AddCategoryMapping(22, TorznabCatType.MoviesHD);
            AddCategoryMapping(7, TorznabCatType.MoviesSD);
            AddCategoryMapping(4, TorznabCatType.Movies);

            AddCategoryMapping(27, TorznabCatType.TVHD);
            AddCategoryMapping(17, TorznabCatType.TVSD);
            AddCategoryMapping(11, TorznabCatType.MoviesSD);
            AddCategoryMapping(26, TorznabCatType.TV);

            AddCategoryMapping(3, TorznabCatType.PCGames);
            AddCategoryMapping(5, TorznabCatType.ConsolePS3);
            AddCategoryMapping(20, TorznabCatType.ConsolePSP);
            AddCategoryMapping(28, TorznabCatType.TV);
            AddCategoryMapping(23, TorznabCatType.Console);
            AddCategoryMapping(29, TorznabCatType.Console);

            AddCategoryMapping(40, TorznabCatType.AudioLossless);
            AddCategoryMapping(13, TorznabCatType.AudioMP3);
            AddCategoryMapping(15, TorznabCatType.AudioVideo);

            AddCategoryMapping(1, TorznabCatType.PCISO);
            AddCategoryMapping(2, TorznabCatType.PCISO);
            AddCategoryMapping(14, TorznabCatType.PCISO);
            AddCategoryMapping(21, TorznabCatType.Other);

            AddCategoryMapping(41, TorznabCatType.MoviesHD);
            AddCategoryMapping(42, TorznabCatType.MoviesSD);
            AddCategoryMapping(43, TorznabCatType.MoviesSD);
            AddCategoryMapping(44, TorznabCatType.TVHD);
            AddCategoryMapping(45, TorznabCatType.TVSD);

            AddCategoryMapping(12, TorznabCatType.XXXXviD);
            AddCategoryMapping(35, TorznabCatType.XXXx264);
            AddCategoryMapping(36, TorznabCatType.XXX);

            AddCategoryMapping(30, TorznabCatType.MoviesForeign);
            AddCategoryMapping(31, TorznabCatType.MoviesForeign);
            AddCategoryMapping(32, TorznabCatType.MoviesForeign);
            AddCategoryMapping(33, TorznabCatType.TVFOREIGN);
            AddCategoryMapping(34, TorznabCatType.TVFOREIGN);

            AddCategoryMapping(4, TorznabCatType.Movies);
            AddCategoryMapping(37, TorznabCatType.XXX);
            AddCategoryMapping(38, TorznabCatType.Audio);

        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "submit", "come on in" }
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, SiteLink, LoginUrl);
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("nav_profile"), () =>
            {
                CQ dom = result.Content;
                var messageEl = dom["#login_box_desc"];
                var errorMessage = messageEl.Text().Trim();
                throw new ExceptionWithConfigData(errorMessage, configData);
            });

            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var results = await RequestStringWithCookiesAndRetry(string.Format(SearchUrl, HttpUtility.UrlEncode(query.GetQueryString())));

            try
            {
                CQ dom = results.Content;
                var rows = dom["#torrents-table > tbody > tr.tt_row"];
                foreach (var row in rows)
                {
                    CQ qRow = row.Cq();
                    var release = new ReleaseInfo();

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 129600;
                    release.Title = qRow.Find(".ttr_name > a").Text();
                    release.Description = release.Title;
                    release.Guid = new Uri(SiteLink  + qRow.Find(".ttr_name > a").Attr("href"));
                    release.Comments = release.Guid;
                    release.Link = new Uri(SiteLink + qRow.Find(".td_dl > a").Attr("href"));

                    var sizeStr = qRow.Find(".ttr_size").Contents()[0].NodeValue;
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    var timeStr = qRow.Find(".ttr_added").Text();
                    DateTime time;
                    if (DateTime.TryParseExact(timeStr, "yyyy-MM-ddHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out time))
                    {
                        release.PublishDate = time;
                    }

                    release.Seeders = ParseUtil.CoerceInt(qRow.Find(".ttr_seeders").Text());
                    release.Peers = ParseUtil.CoerceInt(qRow.Find(".ttr_leechers").Text()) + release.Seeders;

                    var cat = qRow.Find(".ttr_type a").Attr("href").Replace("?cat=",string.Empty);

                    release.Category = MapTrackerCatToNewznab(cat);

                    var files = qRow.Find("td.ttr_size > a").Text().Split(' ')[0];
                    release.Files = ParseUtil.CoerceInt(files);

                    var grabs = qRow.Find("td.ttr_snatched").Get(0).FirstChild.ToString();
                    release.Grabs = ParseUtil.CoerceInt(grabs);

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
