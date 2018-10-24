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
        private string LoginUrl { get { return SiteLink + "takelogin.php"; } }
        private string SearchUrl { get { return SiteLink + "browse.php"; } }

        private new ConfigurationDataBasicLogin configData
        {
            get { return (ConfigurationDataBasicLogin)base.configData; }
            set { base.configData = value; }
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

            TorznabCaps.SupportsImdbSearch = true;

            AddCategoryMapping("1", TorznabCatType.MoviesOther);
            AddCategoryMapping("42", TorznabCatType.Movies);
            AddCategoryMapping("32", TorznabCatType.Movies);
            AddCategoryMapping("43", TorznabCatType.MoviesHD);
            AddCategoryMapping("47", TorznabCatType.Movies);
            AddCategoryMapping("28", TorznabCatType.MoviesBluRay);
            AddCategoryMapping("48", TorznabCatType.Movies3D);
            AddCategoryMapping("40", TorznabCatType.MoviesDVD);
            AddCategoryMapping("49", TorznabCatType.TVHD);
            AddCategoryMapping("50", TorznabCatType.TVSport);
            AddCategoryMapping("52", TorznabCatType.TVHD);
            AddCategoryMapping("53", TorznabCatType.TVSD);
            AddCategoryMapping("41", TorznabCatType.TV);
            AddCategoryMapping("55", TorznabCatType.TV);
            AddCategoryMapping("2", TorznabCatType.TVSD);
            AddCategoryMapping("30", TorznabCatType.TVAnime);
            AddCategoryMapping("25", TorznabCatType.PCISO);
            AddCategoryMapping("39", TorznabCatType.ConsoleWii);
            AddCategoryMapping("45", TorznabCatType.ConsolePS3);
            AddCategoryMapping("35", TorznabCatType.Console);
            AddCategoryMapping("33", TorznabCatType.ConsoleXbox360);
            AddCategoryMapping("46", TorznabCatType.PCPhoneOther);
            AddCategoryMapping("24", TorznabCatType.PC0day);
            AddCategoryMapping("51", TorznabCatType.PCMac);
            AddCategoryMapping("27", TorznabCatType.Books);
            AddCategoryMapping("26", TorznabCatType.Audio);
            AddCategoryMapping("44", TorznabCatType.Audio);
            AddCategoryMapping("29", TorznabCatType.AudioVideo);
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
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            NameValueCollection qParams = new NameValueCollection();

            if (!string.IsNullOrWhiteSpace(query.ImdbID))
            {
                qParams.Add("search", query.ImdbID);
                qParams.Add("d", "on");
            }
            else if (!string.IsNullOrEmpty(query.GetQueryString()))
            {
                qParams.Add("search", query.GetQueryString());
            }

            List<string> catList = MapTorznabCapsToTrackers(query);
            foreach (string cat in catList)
            {
                qParams.Add("c" + cat, "1");
            }

            string urlSearch = SearchUrl;
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

                foreach (IDomObject row in rows)
                {
                    CQ torrentData = row.OuterHTML;
                    CQ cells = row.Cq().Find("td");

                    string title = torrentData.Find("td[class='lft'] > div > a").First().Text().Trim();
                    Uri link = new Uri(SiteLink + torrentData.Find("img[title='Download']").First().Parent().Attr("href").Trim());
                    Uri guid = link;
                    long size = ReleaseInfo.GetBytes(cells.Elements.ElementAt(4).Cq().Text());
                    int grabs = ParseUtil.CoerceInt(cells.Elements.ElementAt(5).Cq().Text());
                    int seeders = ParseUtil.CoerceInt(cells.Elements.ElementAt(6).Cq().Text());
                    int leechers = ParseUtil.CoerceInt(cells.Elements.ElementAt(7).Cq().Text());

                    string pubDateStr = torrentData.Find("span[class^='elapsedDate']").First().Attr("title").Trim().Replace(" at", "");
                    DateTime publishDate = DateTime.ParseExact(pubDateStr, "dddd, MMMM d, yyyy h:mmtt", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToLocalTime();

                    long category = 0;
                    string cat = torrentData.Find("img[class^='Tcat']").First().Parent().Attr("href").Trim().Remove(0, 5);
                    long.TryParse(cat, out category);

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
                    release.MinimumSeedTime = 172800;
                    release.Category = MapTrackerCatToNewznab(category.ToString());
                    release.Comments = guid;

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
