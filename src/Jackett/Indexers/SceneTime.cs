using CsQuery;
using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jackett.Models.IndexerConfig;
using System.Text.RegularExpressions;

namespace Jackett.Indexers
{
    public class SceneTime : BaseWebIndexer
    {
        private string StartPageUrl { get { return SiteLink + "login.php"; } }
        private string LoginUrl { get { return SiteLink + "takelogin.php"; } }
        private string SearchUrl { get { return SiteLink + "browse_API.php"; } }
        private string DownloadUrl { get { return SiteLink + "download.php/{0}/download.torrent"; } }

        new ConfigurationDataRecaptchaLogin configData
        {
            get { return (ConfigurationDataRecaptchaLogin)base.configData; }
            set { base.configData = value; }
        }

        public SceneTime(IIndexerConfigurationService configService, IWebClient w, Logger l, IProtectionService ps)
            : base(name: "SceneTime",
                description: "Always on time",
                link: "https://www.scenetime.com/",
                caps: new TorznabCapabilities(),
                configService: configService,
                client: w,
                logger: l,
                p: ps,
                configData: new ConfigurationDataRecaptchaLogin("For best results, change the 'Torrents per page' setting to the maximum in your profile on the SceneTime webpage."))
        {
            Encoding = Encoding.GetEncoding("iso-8859-1");
            Language = "en-us";
            Type = "private";

            AddCategoryMapping(1, TorznabCatType.MoviesSD);
            AddCategoryMapping(3, TorznabCatType.MoviesDVD);
            AddCategoryMapping(47, TorznabCatType.MoviesSD);
            AddCategoryMapping(57, TorznabCatType.MoviesSD);
            AddCategoryMapping(59, TorznabCatType.MoviesHD);
            AddCategoryMapping(61, TorznabCatType.MoviesSD);
            AddCategoryMapping(64, TorznabCatType.Movies3D);
            AddCategoryMapping(80, TorznabCatType.MoviesForeign);
            AddCategoryMapping(81, TorznabCatType.MoviesBluRay);
            AddCategoryMapping(82, TorznabCatType.MoviesOther);
            AddCategoryMapping(102, TorznabCatType.MoviesOther);
            AddCategoryMapping(103, TorznabCatType.MoviesWEBDL);
            AddCategoryMapping(105, TorznabCatType.Movies);

            AddCategoryMapping(6, TorznabCatType.PCGames);
            AddCategoryMapping(48, TorznabCatType.ConsoleXbox);
            AddCategoryMapping(49, TorznabCatType.ConsolePSP);
            AddCategoryMapping(50, TorznabCatType.ConsolePS3);
            AddCategoryMapping(51, TorznabCatType.ConsoleWii);
            AddCategoryMapping(55, TorznabCatType.ConsoleNDS);
            AddCategoryMapping(107, TorznabCatType.ConsolePS4);

            AddCategoryMapping(2, TorznabCatType.TVSD);
            AddCategoryMapping(43, TorznabCatType.TV);
            AddCategoryMapping(9, TorznabCatType.TVHD);
            AddCategoryMapping(63, TorznabCatType.TV);
            AddCategoryMapping(77, TorznabCatType.TVSD);
            AddCategoryMapping(79, TorznabCatType.TVSport);
            AddCategoryMapping(100, TorznabCatType.TVFOREIGN);
            AddCategoryMapping(83, TorznabCatType.TVWEBDL);

            AddCategoryMapping(5, TorznabCatType.PC0day);
            AddCategoryMapping(7, TorznabCatType.Books);
            AddCategoryMapping(52, TorznabCatType.PCMac);
            AddCategoryMapping(65, TorznabCatType.BooksComics);
            AddCategoryMapping(53, TorznabCatType.PC);

            AddCategoryMapping(4, TorznabCatType.Audio);
            AddCategoryMapping(11, TorznabCatType.AudioVideo);
        }

        public override async Task<ConfigurationData> GetConfigurationForSetup()
        {
            var loginPage = await RequestStringWithCookies(StartPageUrl, string.Empty);
            CQ cq = loginPage.Content;
            var result = this.configData;
            result.Captcha.Version = "2";
            CQ recaptcha = cq.Find(".g-recaptcha").Attr("data-sitekey");
            if (recaptcha.Length != 0)
            {
                result.CookieHeader.Value = loginPage.Cookies;
                result.Captcha.SiteKey = cq.Find(".g-recaptcha").Attr("data-sitekey");
                return result;
            }
            else
            {
                var stdResult = new ConfigurationDataBasicLogin();
                stdResult.SiteLink.Value = configData.SiteLink.Value;
                stdResult.Username.Value = configData.Username.Value;
                stdResult.Password.Value = configData.Password.Value;
                stdResult.CookieHeader.Value = loginPage.Cookies;
                return stdResult;
            }
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "g-recaptcha-response", configData.Captcha.Value }
            };

            if (!string.IsNullOrWhiteSpace(configData.Captcha.Cookie))
            {
                CookieHeader = configData.Captcha.Cookie;
                try
                {
                    var results = await PerformQuery(new TorznabQuery());
                    if (results.Count() == 0)
                    {
                        throw new Exception("Your cookie did not work");
                    }

                    IsConfigured = true;
                    SaveConfig();
                    return IndexerConfigurationStatus.Completed;
                }
                catch (Exception e)
                {
                    IsConfigured = false;
                    throw new Exception("Your cookie did not work: " + e.Message);
                }
            }

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, LoginUrl);
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("logout.php"), () =>
            {
                CQ dom = result.Content;
                var errorMessage = dom["td.text"].Text().Trim();
                throw new ExceptionWithConfigData(errorMessage, configData);
            });

            return IndexerConfigurationStatus.RequiresTesting;
        }


        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            Dictionary<string, string> qParams = new Dictionary<string, string>();
            qParams.Add("cata", "yes");
            qParams.Add("sec", "jax");
            
            List<string> catList = MapTorznabCapsToTrackers(query);
            foreach (string cat in catList)
            {
                qParams.Add("c" + cat, "1");
            }

            if (!string.IsNullOrEmpty(query.SanitizedSearchTerm))
            {
                qParams.Add("search", query.GetQueryString());
            }

            var results = await PostDataWithCookiesAndRetry(SearchUrl, qParams);
            List<ReleaseInfo> releases = ParseResponse(query, results.Content);
            
            return releases;
        }

        public List<ReleaseInfo> ParseResponse(TorznabQuery query, string htmlResponse)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            try
            {
                CQ dom = htmlResponse;

                List<string> headerColumns = dom["table[class*='movehere']"].First().Find("tbody > tr > td[class='cat_Head']").Select(x => x.Cq().Text()).ToList();
                int categoryIndex = headerColumns.FindIndex(x => x.Equals("Type"));
                int nameIndex = headerColumns.FindIndex(x => x.Equals("Name"));
                int sizeIndex = headerColumns.FindIndex(x => x.Equals("Size"));
                int seedersIndex = headerColumns.FindIndex(x => x.Equals("Seeders"));
                int leechersIndex = headerColumns.FindIndex(x => x.Equals("Leechers"));

                var rows = dom["tr.browse"];
                foreach (var row in rows)
                {
                    var release = new ReleaseInfo();
                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;

                    var categoryCol = row.ChildElements.ElementAt(categoryIndex);
                    string catLink = categoryCol.Cq().Find("a").Attr("href");
                    string catId = new Regex(@"\?cat=(\d*)").Match(catLink).Groups[1].ToString().Trim();
                    release.Category = MapTrackerCatToNewznab(catId);

                    var descCol = row.ChildElements.ElementAt(nameIndex);
                    var qDescCol = descCol.Cq();
                    var qLink = qDescCol.Find("a");
                    release.Title = qLink.Text();
                    if (!query.MatchQueryStringAND(release.Title))
                        continue;

                    release.Description = release.Title;
                    release.Comments = new Uri(SiteLink + "/" + qLink.Attr("href"));
                    release.Guid = release.Comments;
                    var torrentId = qLink.Attr("href").Split('=')[1];
                    release.Link = new Uri(string.Format(DownloadUrl, torrentId));

                    release.PublishDate = DateTimeUtil.FromTimeAgo(descCol.ChildNodes.Last().InnerText);

                    var sizeStr = row.ChildElements.ElementAt(sizeIndex).Cq().Text();
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    release.Seeders = ParseUtil.CoerceInt(row.ChildElements.ElementAt(seedersIndex).Cq().Text().Trim());
                    release.Peers = ParseUtil.CoerceInt(row.ChildElements.ElementAt(leechersIndex).Cq().Text().Trim()) + release.Seeders;

                    if (row.Cq().Find("font > b:contains(Freeleech)").Length >= 1)
                        release.DownloadVolumeFactor = 0;
                    else
                        release.DownloadVolumeFactor = 1;

                    release.UploadVolumeFactor = 1;

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(htmlResponse, ex);
            }

            return releases;
        }
    }
}
