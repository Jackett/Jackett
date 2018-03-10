using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsQuery;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    //
    //     Quick and dirty indexer for GFTracker.
    //
    public class GFTracker : BaseWebIndexer
    {
        private string StartPageUrl { get { return SiteLink + "login.php?returnto=%2F"; } }
        private string LoginUrl { get { return SiteLink + "loginsite.php"; } }
        private string SearchUrl { get { return SiteLink + "browse.php"; } }

        private new ConfigurationDataRecaptchaLogin configData
        {
            get { return (ConfigurationDataRecaptchaLogin)base.configData; }
            set { base.configData = value; }
        }

        public GFTracker(IIndexerConfigurationService configService, WebClient w, Logger l, IProtectionService ps)
            : base(name: "GFTracker",
                description: "Home of user happiness",
                link: "https://www.thegft.org/",
                caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                configService: configService,
                client: w,
                logger: l,
                p: ps,
                configData: new ConfigurationDataRecaptchaLogin())
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";

            AddCategoryMapping(2, TorznabCatType.PC0day, "0DAY");
            AddCategoryMapping(16, TorznabCatType.TVAnime, "Anime");
            AddCategoryMapping(1, TorznabCatType.PC0day, "APPS");
            AddCategoryMapping(9, TorznabCatType.Other, "E-Learning");
            AddCategoryMapping(35, TorznabCatType.TVFOREIGN, "Foreign");
            AddCategoryMapping(32, TorznabCatType.ConsoleNDS, "Games/NDS");
            AddCategoryMapping(6, TorznabCatType.PCGames, "Games/PC");
            AddCategoryMapping(36, TorznabCatType.ConsolePS4, "Games/Playstation");
            AddCategoryMapping(29, TorznabCatType.ConsolePSP, "Games/PSP");
            AddCategoryMapping(23, TorznabCatType.ConsoleWii, "Games/WII");
            AddCategoryMapping(12, TorznabCatType.ConsoleXbox, "Games/XBOX");
            AddCategoryMapping(11, TorznabCatType.Other, "Misc");
            AddCategoryMapping(48, TorznabCatType.MoviesBluRay, "Movies/BLURAY");
            AddCategoryMapping(8, TorznabCatType.MoviesDVD, "Movies/DVDR");
            AddCategoryMapping(18, TorznabCatType.MoviesHD, "Movies/X264-HD");
            AddCategoryMapping(49, TorznabCatType.MoviesSD, "Movies/X264-SD");
            AddCategoryMapping(7, TorznabCatType.MoviesSD, "Movies/XVID");
            AddCategoryMapping(38, TorznabCatType.AudioOther, "Music/DVDR");
            AddCategoryMapping(46, TorznabCatType.AudioLossless, "Music/FLAC");
            AddCategoryMapping(5, TorznabCatType.AudioMP3, "Music/MP3");
            AddCategoryMapping(13, TorznabCatType.AudioVideo, "Music/Vids");
            AddCategoryMapping(26, TorznabCatType.TVHD, "TV/BLURAY");
            AddCategoryMapping(37, TorznabCatType.TVSD, "TV/DVDR");
            AddCategoryMapping(19, TorznabCatType.TVSD, "TV/DVDRIP");
            AddCategoryMapping(47, TorznabCatType.TVSD, "TV/SD");
            AddCategoryMapping(17, TorznabCatType.TVHD, "TV/X264");
            AddCategoryMapping(4, TorznabCatType.TVSD, "TV/XVID");
            AddCategoryMapping(22, TorznabCatType.XXX, "XXX/0DAY");
            AddCategoryMapping(25, TorznabCatType.XXXDVD, "XXX/DVDR");
            AddCategoryMapping(20, TorznabCatType.XXX, "XXX/HD");
            AddCategoryMapping(3, TorznabCatType.XXXXviD, "XXX/XVID");
        }

        public override async Task<ConfigurationData> GetConfigurationForSetup()
        {
            var loginPage = await RequestStringWithCookies(StartPageUrl, string.Empty);
            CQ cq = loginPage.Content;
            var result = this.configData;
            CQ recaptcha = cq.Find(".g-recaptcha").Attr("data-sitekey");
            if (recaptcha.Length != 0)   // recaptcha not always present in login form, perhaps based on cloudflare uid or just phase of the moon
            {
                result.CookieHeader.Value = loginPage.Cookies;
                result.Captcha.SiteKey = cq.Find(".g-recaptcha").Attr("data-sitekey");
                result.Captcha.Version = "2";
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

            var response = await RequestLoginAndFollowRedirect(LoginUrl, pairs, configData.CookieHeader.Value, true, SearchUrl, StartPageUrl);
            UpdateCookieHeader(response.Cookies);
            UpdateCookieHeader("mybbuser=;"); // add dummy cookie, otherwise we get logged out after each request

            await ConfigureIfOK(configData.CookieHeader.Value, response.Content != null && response.Content.Contains("logout.php"), () =>
            {
                CQ dom = response.Content;
                var messageEl = dom["div:has(h2)"].Last();
                messageEl.Children("a").Remove();
                messageEl.Children("style").Remove();
                var errorMessage = messageEl.Text().Trim();
                IsConfigured = false;
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();

            // search in normal + gems view
            foreach (var view in new string[] { "0", "1" })
            {
                var queryCollection = new NameValueCollection();

                queryCollection.Add("view", view);
                queryCollection.Add("searchtype", "1");
                queryCollection.Add("incldead", "1");
                if (!string.IsNullOrWhiteSpace(searchString))
                {
                    queryCollection.Add("search", searchString);
                }

                foreach (var cat in MapTorznabCapsToTrackers(query))
                {
                    queryCollection.Add(string.Format("c{0}", cat), "1");
                }

                var searchUrl = SearchUrl + "?" + queryCollection.GetQueryString();

                var results = await RequestStringWithCookiesAndRetry(searchUrl);
                if (results.IsRedirect)
                {
                    // re-login
                    await ApplyConfiguration(null);
                    results = await RequestStringWithCookiesAndRetry(searchUrl);
                }

                try
                {
                    CQ dom = results.Content;
                    var rows = dom["#torrentBrowse > table > tbody > tr"];
                    foreach (var row in rows.Skip(1))
                    {
                        var release = new ReleaseInfo();
                        CQ qRow = row.Cq();

                        release.MinimumRatio = 0;
                        release.MinimumSeedTime = 2 * 24 * 60 * 60;

                        var qLink = qRow.Find("a[title][href^=\"details.php?id=\"]");
                        release.Title = qLink.Attr("title");
                        release.Guid = new Uri(SiteLink + qLink.Attr("href").TrimStart('/'));
                        release.Comments = release.Guid;

                        qLink = qRow.Children().ElementAt(3).Cq().Children("a").First();
                        release.Link = new Uri(string.Format("{0}{1}", SiteLink, qLink.Attr("href")));

                        var catUrl = qRow.Children().ElementAt(0).FirstElementChild.Cq().Attr("href");
                        var catNum = catUrl.Split(new char[] { '=', '&' })[2].Replace("c", "");
                        release.Category = MapTrackerCatToNewznab(catNum);

                        var dateString = qRow.Children().ElementAt(6).Cq().Text().Trim();
                        if (dateString.Contains("ago"))
                            release.PublishDate = DateTimeUtil.FromTimeAgo(dateString);
                        else
                            release.PublishDate = DateTime.ParseExact(dateString, "yyyy-MM-ddHH:mm:ss", CultureInfo.InvariantCulture);

                        var sizeStr = qRow.Children().ElementAt(7).Cq().Text().Split(new char[] { '/' })[0];
                        release.Size = ReleaseInfo.GetBytes(sizeStr);

                        release.Seeders = ParseUtil.CoerceInt(qRow.Children().ElementAt(8).Cq().Text().Split(new char[] { '/' })[0].Trim());
                        release.Peers = ParseUtil.CoerceInt(qRow.Children().ElementAt(8).Cq().Text().Split(new char[] { '/' })[1].Trim()) + release.Seeders;
                        release.Files = ParseUtil.CoerceLong(qRow.Find("td:nth-child(5)").Text());
                        release.Grabs = ParseUtil.CoerceLong(qRow.Find("a[href^=\"snatches.php?id=\"]").Text().Split(' ')[0]);

                        release.DownloadVolumeFactor = 0;
                        release.UploadVolumeFactor = 1;

                        var desc = qRow.Find("td:nth-child(2)");
                        desc.Find("a").Remove();
                        desc.Find("small").Remove(); // Remove release name (if enabled in the user cp)
                        release.Description = desc.Text().Trim(new char[] { '-', ' ' });

                        releases.Add(release);
                    }
                }
                catch (Exception ex)
                {
                    OnParseError(results.Content, ex);
                }
            }

            return releases;
        }
    }
}
