using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class PixelHD : BaseWebIndexer
    {
        private string LoginUrl => SiteLink + "login.php";
        private string BrowseUrl => SiteLink + "torrents.php";

        private new ConfigurationDataCaptchaLogin configData
        {
            get => (ConfigurationDataCaptchaLogin)base.configData;
            set => base.configData = value;
        }

        private string input_captcha = null;
        private string input_username = null;
        private string input_password = null;

        public PixelHD(IIndexerConfigurationService configService, WebClient webClient, Logger logger, IProtectionService protectionService)
            : base(id: "pixelhd",
                   name: "PiXELHD",
                   description: "PixelHD (PxHD) is a Private Torrent Tracker for HD .MP4 MOVIES / TV",
                   link: "https://pixelhd.me/",
                   caps: new TorznabCapabilities
                   {
                       MovieSearchParams = new List<MovieSearchParam>
                       {
                           MovieSearchParam.Q, MovieSearchParam.ImdbId
                       }
                   },
                   configService: configService,
                   logger: logger,
                   p: protectionService,
                   client: webClient,
                   configData: new ConfigurationDataCaptchaLogin()
                )
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";

            AddCategoryMapping(1, TorznabCatType.MoviesHD);
        }

        public override async Task<ConfigurationData> GetConfigurationForSetup()
        {
            var loginPage = await RequestWithCookiesAsync(LoginUrl, string.Empty);
            var LoginParser = new HtmlParser();
            var LoginDocument = LoginParser.ParseDocument(loginPage.ContentString);

            configData.CaptchaCookie.Value = loginPage.Cookies;

            var catchaImg = LoginDocument.QuerySelector("img[alt=\"FuckOff Image\"]");
            if (catchaImg != null)
            {
                var catchaInput = LoginDocument.QuerySelector("input[maxlength=\"6\"]");
                input_captcha = catchaInput.GetAttribute("name");

                var captchaImage = await RequestWithCookiesAsync(SiteLink + catchaImg.GetAttribute("src"), loginPage.Cookies, RequestType.GET, LoginUrl);
                configData.CaptchaImage.Value = captchaImage.ContentBytes;
            }
            else
            {
                input_captcha = null;
                configData.CaptchaImage.Value = null;
            }

            var usernameInput = LoginDocument.QuerySelector("input[maxlength=\"20\"]");
            input_username = usernameInput.GetAttribute("name");

            var passwordInput = LoginDocument.QuerySelector("input[maxlength=\"40\"]");
            input_password = passwordInput.GetAttribute("name");

            return configData;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string>
            {
                { input_username, configData.Username.Value },
                { input_password, configData.Password.Value },
                { "keeplogged", "1" }
            };

            if (input_captcha != null)
                pairs.Add(input_captcha, configData.CaptchaText.Value);

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, configData.CaptchaCookie.Value, true);

            await ConfigureIfOK(result.Cookies, result.ContentString.Contains("logout.php"), () =>
           {
               var LoginParser = new HtmlParser();
               var LoginDocument = LoginParser.ParseDocument(result.ContentString);
               var errorMessage = LoginDocument.QuerySelector("span.warning[id!=\"no-cookies\"]:has(br)").TextContent;
               throw new ExceptionWithConfigData(errorMessage, configData);
           });

            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();
            var queryCollection = new NameValueCollection
            {
                { "order_by", "time" },
                { "order_way", "desc" }
            };

            if (!string.IsNullOrWhiteSpace(query.ImdbID))
            {
                queryCollection.Add("imdbid", query.ImdbID);
            }
            else if (!string.IsNullOrWhiteSpace(searchString))
            {
                queryCollection.Add("groupname", searchString);
            }
            //Add anyway after checking above?
            queryCollection.Add("groupname", searchString);

            var searchUrl = BrowseUrl + "?" + queryCollection.GetQueryString();

            var results = await RequestWithCookiesAsync(searchUrl);
            if (results.IsRedirect)
            {
                // re login
                await GetConfigurationForSetup();
                await ApplyConfiguration(null);
                results = await RequestWithCookiesAsync(searchUrl);
            }

            var IMDBRegEx = new Regex(@"tt(\d+)", RegexOptions.Compiled);
            var hParser = new HtmlParser();
            var ResultDocument = hParser.ParseDocument(results.ContentString);
            try
            {
                var Groups = ResultDocument.QuerySelectorAll("div.browsePoster");

                foreach (var Group in Groups)
                {
                    var groupPoster = Group.QuerySelector("img.classBrowsePoster");
                    var poster = new Uri(SiteLink + groupPoster.GetAttribute("src"));

                    long? IMDBId = null;
                    var imdbLink = Group.QuerySelector("a[href*=\"www.imdb.com/title/tt\"]");
                    if (imdbLink != null)
                    {
                        var IMDBMatch = IMDBRegEx.Match(imdbLink.GetAttribute("href"));
                        IMDBId = ParseUtil.CoerceLong(IMDBMatch.Groups[1].Value);
                    }

                    var group = Group.QuerySelector("strong:has(a[title=\"View Torrent\"])").TextContent.Replace(" ]", "]");

                    var Rows = Group.QuerySelectorAll("tr.group_torrent:has(a[href^=\"torrents.php?id=\"])");
                    foreach (var Row in Rows)
                    {
                        var title = Row.QuerySelector("a[href^=\"torrents.php?id=\"]");
                        var added = Row.QuerySelector("td:nth-child(3)");
                        var Size = Row.QuerySelector("td:nth-child(4)");
                        var Grabs = Row.QuerySelector("td:nth-child(6)");
                        var Seeders = Row.QuerySelector("td:nth-child(7)");
                        var Leechers = Row.QuerySelector("td:nth-child(8)");
                        var link = new Uri(SiteLink + Row.QuerySelector("a[href^=\"torrents.php?action=download\"]").GetAttribute("href"));
                        var seeders = ParseUtil.CoerceInt(Seeders.TextContent);
                        var details = new Uri(SiteLink + title.GetAttribute("href"));
                        var size = ReleaseInfo.GetBytes(Size.TextContent);
                        var leechers = ParseUtil.CoerceInt(Leechers.TextContent);
                        var grabs = ParseUtil.CoerceLong(Grabs.TextContent);
                        var publishDate = DateTimeUtil.FromTimeAgo(added.TextContent);

                        var release = new ReleaseInfo
                        {
                            MinimumRatio = 1,
                            MinimumSeedTime = 72 * 60 * 60,
                            Title = group + " " + title.TextContent,
                            Category = new List<int> { TorznabCatType.MoviesHD.ID },
                            Link = link,
                            Details = details,
                            Guid = link,
                            Size = size,
                            Seeders = seeders,
                            Peers = leechers + seeders,
                            Grabs = grabs,
                            PublishDate = publishDate,
                            Poster = poster,
                            Imdb = IMDBId
                        };
                        releases.Add(release);
                    }
                }
            }
            catch (Exception ex)
            {
                OnParseError(results.ContentString, ex);
            }

            return releases;
        }
    }
}
