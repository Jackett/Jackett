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
    public class SpeedCD : BaseWebIndexer
    {
        private string LoginUrl => SiteLink + "take_login.php";
        private string SearchUrl => SiteLink + "browse.php";

        private new ConfigurationDataBasicLogin configData
        {
            get => (ConfigurationDataBasicLogin)base.configData;
            set => base.configData = value;
        }

        public SpeedCD(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(name: "Speed.cd",
                description: "Your home now!",
                link: "https://speed.cd/",
                caps: new TorznabCapabilities(),
                configService: configService,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLogin(@"Speed.Cd have increased their security. If you are having problems please check the security tab in your Speed.Cd profile.
                                                            eg. Geo Locking, your seedbox may be in a different country to the one where you login via your web browser"))
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";

            TorznabCaps.SupportsImdbMovieSearch = true;

            AddCategoryMapping(1, TorznabCatType.MoviesOther, "Movies/XviD");
            AddCategoryMapping(42, TorznabCatType.Movies, "Movies/Packs");
            AddCategoryMapping(32, TorznabCatType.Movies, "Movies/Kids");
            AddCategoryMapping(43, TorznabCatType.MoviesHD, "Movies/HD");
            AddCategoryMapping(47, TorznabCatType.Movies, "Movies/DiVERSiTY");
            AddCategoryMapping(28, TorznabCatType.MoviesBluRay, "Movies/B-Ray");
            AddCategoryMapping(48, TorznabCatType.Movies3D, "Movies/3D");
            AddCategoryMapping(40, TorznabCatType.MoviesDVD, "Movies/DVD-R");
            AddCategoryMapping(56, TorznabCatType.Movies, "Movies/Anime");
            AddCategoryMapping(50, TorznabCatType.TVSport, "TV/Sports");
            AddCategoryMapping(52, TorznabCatType.TVHD, "TV/B-Ray");
            AddCategoryMapping(53, TorznabCatType.TVSD, "TV/DVD-R");
            AddCategoryMapping(41, TorznabCatType.TV, "TV/Packs");
            AddCategoryMapping(55, TorznabCatType.TV, "TV/Kids");
            AddCategoryMapping(57, TorznabCatType.TV, "TV/DiVERSiTY");
            AddCategoryMapping(49, TorznabCatType.TVHD, "TV/HD");
            AddCategoryMapping(2, TorznabCatType.TVSD, "TV/Episodes");
            AddCategoryMapping(30, TorznabCatType.TVAnime, "TV/Anime");
            AddCategoryMapping(25, TorznabCatType.PCISO, "Games/PC ISO");
            AddCategoryMapping(39, TorznabCatType.ConsoleWii, "Games/Wii");
            AddCategoryMapping(45, TorznabCatType.ConsolePS3, "Games/PS3");
            AddCategoryMapping(35, TorznabCatType.Console, "Games/Nintendo");
            AddCategoryMapping(33, TorznabCatType.ConsoleXbox360, "Games/XboX360");
            AddCategoryMapping(46, TorznabCatType.PCPhoneOther, "Mobile");
            AddCategoryMapping(24, TorznabCatType.PC0day, "Apps/0DAY");
            AddCategoryMapping(51, TorznabCatType.PCMac, "Mac");
            AddCategoryMapping(54, TorznabCatType.Books, "Educational");
            AddCategoryMapping(27, TorznabCatType.Books, "Books-Mags");
            AddCategoryMapping(26, TorznabCatType.Audio, "Music/Audio");
            AddCategoryMapping(44, TorznabCatType.Audio, "Music/Pack");
            AddCategoryMapping(29, TorznabCatType.AudioVideo, "Music/Video");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            await DoLogin();

            return IndexerConfigurationStatus.RequiresTesting;
        }

        private async Task DoLogin()
        {
            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, SiteLink);

            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("/browse.php"), () =>
            {
                CQ dom = result.Content;
                var errorMessage = dom.Text();
                if (errorMessage.Contains("Wrong Captcha!"))
                    errorMessage = "Captcha requiered due to a failed login attempt. Login via a browser to whitelist your IP and then reconfigure jackett.";
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            var qParams = new NameValueCollection();

            if (!string.IsNullOrWhiteSpace(query.ImdbID))
            {
                qParams.Add("search", query.ImdbID);
                qParams.Add("d", "on");
            }
            else if (!string.IsNullOrEmpty(query.GetQueryString()))
            {
                qParams.Add("search", query.GetQueryString());
            }

            var catList = MapTorznabCapsToTrackers(query);
            foreach (var cat in catList)
            {
                qParams.Add("c" + cat, "1");
            }

            var urlSearch = SearchUrl;
            if (qParams.Count > 0)
            {
                urlSearch += $"?{qParams.GetQueryString()}";
            }

            var response = await RequestStringWithCookiesAndRetry(urlSearch);
            if (!response.Content.Contains("/logout.php"))
            {
                //Cookie appears to expire after a period of time or logging in to the site via browser
                await DoLogin();
                response = await RequestStringWithCookiesAndRetry(urlSearch);
            }

            try
            {
                CQ dom = response.Content;
                var rows = dom["div[id='torrentTable'] > div[class^='box torrentBox'] > div[class='boxContent'] > table > tbody > tr"];

                foreach (var row in rows)
                {
                    CQ torrentData = row.OuterHTML;
                    var cells = row.Cq().Find("td");

                    var title = torrentData.Find("td[class='lft'] > div > a").First().Text().Trim();
                    var link = new Uri(SiteLink + torrentData.Find("img[title='Download']").First().Parent().Attr("href").Trim());
                    var guid = link;
                    var comments = new Uri(SiteLink + torrentData.Find("td[class='lft'] > div > a").First().Attr("href").Trim().Remove(0, 1));
                    var size = ReleaseInfo.GetBytes(cells.Elements.ElementAt(4).Cq().Text());
                    var grabs = ParseUtil.CoerceInt(cells.Elements.ElementAt(5).Cq().Text());
                    var seeders = ParseUtil.CoerceInt(cells.Elements.ElementAt(6).Cq().Text());
                    var leechers = ParseUtil.CoerceInt(cells.Elements.ElementAt(7).Cq().Text());

                    var pubDateStr = torrentData.Find("span[class^='elapsedDate']").First().Attr("title").Trim().Replace(" at", "");
                    var publishDate = DateTime.ParseExact(pubDateStr, "dddd, MMMM d, yyyy h:mmtt", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToLocalTime();

                    var cat = torrentData.Find("img[class^='Tcat']").First().Parent().Attr("href").Trim().Remove(0, 5);
                    long.TryParse(cat, out var category);

                    // This fixes the mixed initializer issue, so it's just inconsistent in the code base.
                    // https://github.com/Jackett/Jackett/pull/7166#discussion_r376817517
                    var release = new ReleaseInfo();

                    release.Title = title;
                    release.Guid = guid;
                    release.Link = link;
                    release.PublishDate = publishDate;
                    release.Size = size;
                    release.Grabs = grabs;
                    release.Seeders = seeders;
                    release.Peers = seeders + leechers;
                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800; // 48 hours
                    release.Category = MapTrackerCatToNewznab(category.ToString());
                    release.Comments = comments;

                    if (torrentData.Find("span:contains(\"[Freeleech]\")").Any())
                        release.DownloadVolumeFactor = 0;
                    else
                        release.DownloadVolumeFactor = 1;

                    release.UploadVolumeFactor = 1;

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(response.Content, ex);
            }
            return releases;
        }
    }
}
