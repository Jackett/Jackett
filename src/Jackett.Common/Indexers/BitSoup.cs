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
    public class BitSoup : BaseWebIndexer
    {
        private string BrowseUrl { get { return SiteLink + "browse.php"; } }
        private string LoginUrl { get { return SiteLink + "takelogin.php"; } }
        private string LoginReferer { get { return SiteLink + "login.php"; } }
        public override string[] AlternativeSiteLinks { get; protected set; } = new string[] { "https://www.bitsoup.me/", "https://www.bitsoup.org/" };

        private new ConfigurationDataBasicLogin configData
        {
            get { return (ConfigurationDataBasicLogin)base.configData; }
            set { base.configData = value; }
        }

        public BitSoup(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(name: "BitSoup",
                description: "SoupieBits",
                link: "https://www.bitsoup.me/",
                caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                configService: configService,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLogin())
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";

            //AddCategoryMapping("624", TorznabCatType.Console);
            //AddCategoryMapping("307", TorznabCatType.ConsoleNDS);
            //AddCategoryMapping("308", TorznabCatType.ConsolePSP);
            AddCategoryMapping("35", TorznabCatType.ConsoleWii);
            //AddCategoryMapping("309", TorznabCatType.ConsoleXbox);
            AddCategoryMapping("12", TorznabCatType.ConsoleXbox360);
            //AddCategoryMapping("305", TorznabCatType.ConsoleWiiwareVC);
            //AddCategoryMapping("309", TorznabCatType.ConsoleXBOX360DLC);
            AddCategoryMapping("38", TorznabCatType.ConsolePS3);
            //AddCategoryMapping("239", TorznabCatType.ConsoleOther);
            //AddCategoryMapping("245", TorznabCatType.ConsoleOther);
            //AddCategoryMapping("246", TorznabCatType.ConsoleOther);
            //AddCategoryMapping("626", TorznabCatType.ConsoleOther);
            //AddCategoryMapping("628", TorznabCatType.ConsoleOther);
            //AddCategoryMapping("630", TorznabCatType.ConsoleOther);
            //AddCategoryMapping("307", TorznabCatType.Console3DS);
            //AddCategoryMapping("308", TorznabCatType.ConsolePSVita);
            //AddCategoryMapping("307", TorznabCatType.ConsoleWiiU);
            //AddCategoryMapping("309", TorznabCatType.ConsoleXboxOne);
            //AddCategoryMapping("308", TorznabCatType.ConsolePS4);
            //AddCategoryMapping("631", TorznabCatType.Movies);
            //AddCategoryMapping("631", TorznabCatType.MoviesForeign);
            //AddCategoryMapping("455", TorznabCatType.MoviesOther);
            //AddCategoryMapping("633", TorznabCatType.MoviesOther);
            AddCategoryMapping("19", TorznabCatType.MoviesSD);
            AddCategoryMapping("41", TorznabCatType.MoviesHD);
            AddCategoryMapping("17", TorznabCatType.Movies3D);
            AddCategoryMapping("80", TorznabCatType.MoviesBluRay);
            AddCategoryMapping("20", TorznabCatType.MoviesDVD);
            //AddCategoryMapping("631", TorznabCatType.MoviesWEBDL);
            AddCategoryMapping("6", TorznabCatType.Audio);
            //AddCategoryMapping("623", TorznabCatType.AudioMP3);
            AddCategoryMapping("29", TorznabCatType.AudioVideo);
            //AddCategoryMapping("402", TorznabCatType.AudioVideo);
            AddCategoryMapping("5", TorznabCatType.AudioAudiobook);
            //AddCategoryMapping("1", TorznabCatType.AudioLossless);
            //AddCategoryMapping("403", TorznabCatType.AudioOther);
            //AddCategoryMapping("642", TorznabCatType.AudioOther);
            //AddCategoryMapping("1", TorznabCatType.AudioForeign);
            //AddCategoryMapping("233", TorznabCatType.PC);
            //AddCategoryMapping("236", TorznabCatType.PC);
            //AddCategoryMapping("1", TorznabCatType.PC0day);
            AddCategoryMapping("1", TorznabCatType.PCISO);
            //AddCategoryMapping("235", TorznabCatType.PCMac);
            //AddCategoryMapping("627", TorznabCatType.PCPhoneOther);
            AddCategoryMapping("21", TorznabCatType.PCGames);
            AddCategoryMapping("4", TorznabCatType.PCGames);
            //AddCategoryMapping("625", TorznabCatType.PCPhoneIOS);
            //AddCategoryMapping("625", TorznabCatType.PCPhoneAndroid);
            AddCategoryMapping("45", TorznabCatType.TV);
            //AddCategoryMapping("433", TorznabCatType.TV);
            //AddCategoryMapping("639", TorznabCatType.TVWEBDL);
            //AddCategoryMapping("433", TorznabCatType.TVWEBDL);
            //AddCategoryMapping("639", TorznabCatType.TVFOREIGN);
            //AddCategoryMapping("433", TorznabCatType.TVFOREIGN);
            AddCategoryMapping("7", TorznabCatType.TVSD);
            AddCategoryMapping("49", TorznabCatType.TVSD);
            AddCategoryMapping("42", TorznabCatType.TVHD);
            //AddCategoryMapping("433", TorznabCatType.TVHD);
            //AddCategoryMapping("635", TorznabCatType.TVOTHER);
            //AddCategoryMapping("636", TorznabCatType.TVSport);
            AddCategoryMapping("23", TorznabCatType.TVAnime);
            //AddCategoryMapping("634", TorznabCatType.TVDocumentary);
            AddCategoryMapping("9", TorznabCatType.XXX);
            //AddCategoryMapping("1", TorznabCatType.XXXDVD);
            //AddCategoryMapping("1", TorznabCatType.XXXWMV);
            //AddCategoryMapping("1", TorznabCatType.XXXXviD);
            //AddCategoryMapping("1", TorznabCatType.XXXx264);
            //AddCategoryMapping("1", TorznabCatType.XXXOther);
            //AddCategoryMapping("1", TorznabCatType.XXXImageset);
            //AddCategoryMapping("1", TorznabCatType.XXXPacks);
            //AddCategoryMapping("340", TorznabCatType.Other);
            //AddCategoryMapping("342", TorznabCatType.Other);
            //AddCategoryMapping("344", TorznabCatType.Other);
            //AddCategoryMapping("391", TorznabCatType.Other);
            //AddCategoryMapping("392", TorznabCatType.Other);
            //AddCategoryMapping("393", TorznabCatType.Other);
            //AddCategoryMapping("394", TorznabCatType.Other);
            //AddCategoryMapping("234", TorznabCatType.Other);
            //AddCategoryMapping("638", TorznabCatType.Other);
            //AddCategoryMapping("629", TorznabCatType.Other);
            //AddCategoryMapping("1", TorznabCatType.OtherMisc);
            //AddCategoryMapping("1", TorznabCatType.OtherHashed);
            //AddCategoryMapping("408", TorznabCatType.Books);
            AddCategoryMapping("24", TorznabCatType.BooksEbook);
            //AddCategoryMapping("406", TorznabCatType.BooksComics);
            //AddCategoryMapping("407", TorznabCatType.BooksComics);
            //AddCategoryMapping("409", TorznabCatType.BooksComics);
            //AddCategoryMapping("410", TorznabCatType.BooksMagazines);
            //AddCategoryMapping("1", TorznabCatType.BooksTechnical);
            //AddCategoryMapping("1", TorznabCatType.BooksOther);
            //AddCategoryMapping("1", TorznabCatType.BooksForeign);
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
            };

            var loginPage = await RequestStringWithCookies(SiteLink, string.Empty);

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, loginPage.Cookies, true, null, LoginReferer, true);
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("logout.php"), () =>
            {
                CQ dom = result.Content;
                var messageEl = dom["body > table.statusbar1 > tbody > tr > td > table > tbody > tr > td > table > tbody > tr > td"].First();
                var errorMessage = messageEl.Text().Trim();
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();
            var searchUrl = BrowseUrl;
            var trackerCats = MapTorznabCapsToTrackers(query);
            var queryCollection = new NameValueCollection();

            queryCollection.Add("search", string.IsNullOrWhiteSpace(searchString) ? "" : searchString);
            if (trackerCats.Count > 1)
            {
                for (var ct = 0; ct < trackerCats.Count; ct++) queryCollection.Add("cat" + (ct + 1), trackerCats.ElementAt(ct));
            }
            else
            {
                queryCollection.Add("cat", (trackerCats.Count == 1 ? trackerCats.ElementAt(0) : "0"));
            }
            //queryCollection.Add("cat", (trackerCats.Count == 1 ? trackerCats.ElementAt(0) : "0"));
            searchUrl += "?" + queryCollection.GetQueryString();
            await ProcessPage(releases, searchUrl);

            return releases;
        }

        private async Task ProcessPage(List<ReleaseInfo> releases, string searchUrl)
        {
            var response = await RequestStringWithCookiesAndRetry(searchUrl, null, BrowseUrl);
            var results = response.Content;
            try
            {
                CQ dom = results;

                var rows = dom["table.koptekst tr"];
                foreach (var row in rows.Skip(1))
                {
                    var release = new ReleaseInfo();

                    release.Title = row.Cq().Find("td:eq(1) a").First().Text().Trim();
                    release.Comments = new Uri(SiteLink + row.Cq().Find("td:eq(1) a").First().Attr("href"));

                    release.Link = new Uri(SiteLink + row.Cq().Find("td:eq(2) a").First().Attr("href"));
                    release.Guid = release.Link;
                    release.Description = release.Title;
                    var cat = row.Cq().Find("td:eq(0) a").First().Attr("href").Substring(15);
                    release.Category = MapTrackerCatToNewznab(cat);

                    var added = row.Cq().Find("td:eq(7)").First().Text().Trim();
                    release.PublishDate = DateTime.ParseExact(added, "yyyy-MM-ddH:mm:ss", CultureInfo.InvariantCulture);

                    var sizeStr = row.Cq().Find("td:eq(8)").First().Text().Trim();
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    release.Seeders = ParseUtil.CoerceInt(row.Cq().Find("td:eq(10)").First().Text().Trim());
                    release.Peers = ParseUtil.CoerceInt(row.Cq().Find("td:eq(11)").First().Text().Trim()) + release.Seeders;

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(results, ex);
            }
        }
    }
}