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
    public class RevolutionTT : BaseIndexer, IIndexer
    {        
        private string LandingPageURL { get { return SiteLink + "login.php"; } }
        private string LoginUrl { get { return SiteLink + "takelogin.php"; } }
        private string SearchUrl { get { return SiteLink + "browse.php"; } }

        new ConfigurationDataBasicLogin configData
        {
            get { return (ConfigurationDataBasicLogin)base.configData; }
            set { base.configData = value; }
        }

        public RevolutionTT(IIndexerManagerService i, Logger l, IWebClient wc, IProtectionService ps)
            : base(name: "RevolutionTT",
                description: "The Revolution has begun",
                link: "https://revolutiontt.me/",
                caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                manager: i,
                client: wc,
                logger: l,
                p: ps,
                downloadBase: "https://revolutiontt.me/download.php/",
                configData: new ConfigurationDataBasicLogin())
        {

            /* Original RevolutionTT Categories -
			
			Anime - 23
			Appz/Misc - 22
			Appz/PC-ISO - 1
			E-Book - 36
			Games/PC-ISO - 4
			Games/PC-Rips - 21
			Games/PS3 - 16
			Games/Wii - 40
			Games/XBOX360 - 39
			Handheld/NDS - 35
			Handheld/PSP - 34
			Mac	- 2
			Movies/BluRay - 10
			Movies/DVDR - 20
			Movies/HDx264 - 12
			Movies/Packs - 44
			Movies/SDx264 - 11
			Movies/XviD - 19
			Music - 6
			Music/FLAC - 8
			Music/Packs - 46
			MusicVideos - 29
			TV/DVDR - 43
			TV/HDx264 - 42
			TV/Packs - 45
			TV/SDx264 - 41
			TV/XViD - 7		
			
			*/

            //AddCategoryMapping("cat_id", TorznabCatType.Console);
            AddCategoryMapping("35", TorznabCatType.ConsoleNDS);
            AddCategoryMapping("34", TorznabCatType.ConsolePSP);
            AddCategoryMapping("40", TorznabCatType.ConsoleWii);
            //AddCategoryMapping("cat_id", TorznabCatType.ConsoleXbox);
            AddCategoryMapping("39", TorznabCatType.ConsoleXbox360);
            //AddCategoryMapping("cat_id", TorznabCatType.ConsoleWiiwareVC);
            //AddCategoryMapping("cat_id", TorznabCatType.ConsoleXBOX360DLC);
            AddCategoryMapping("16", TorznabCatType.ConsolePS3);
            //AddCategoryMapping("cat_id", TorznabCatType.ConsoleOther);
            //AddCategoryMapping("cat_id", TorznabCatType.Console3DS);
            //AddCategoryMapping("cat_id", TorznabCatType.ConsolePSVita);
            AddCategoryMapping("40", TorznabCatType.ConsoleWiiU);
            //AddCategoryMapping("cat_id", TorznabCatType.ConsoleXboxOne);
            //AddCategoryMapping("cat_id", TorznabCatType.ConsolePS4);			
            AddCategoryMapping("44", TorznabCatType.Movies);
            //AddCategoryMapping("cat_id", TorznabCatType.MoviesForeign);
            //AddCategoryMapping("cat_id", TorznabCatType.MoviesOther);
            //Movies/DVDR, Movies/SDx264, Movies/XviD
            AddMultiCategoryMapping(TorznabCatType.MoviesSD, 20, 11, 19);
            //Movies/BluRay, Movies/HDx264
            AddMultiCategoryMapping(TorznabCatType.MoviesHD, 10, 12);
            //AddCategoryMapping("cat_id", TorznabCatType.Movies3D);
            AddCategoryMapping("10", TorznabCatType.MoviesBluRay);
            AddCategoryMapping("20", TorznabCatType.MoviesDVD);
            //AddCategoryMapping("cat_id", TorznabCatType.MoviesWEBDL);
            //Music, Music/Packs
            AddMultiCategoryMapping(TorznabCatType.Audio, 6, 46);
            //AddCategoryMapping("cat_id", TorznabCatType.AudioMP3);
            AddCategoryMapping("29", TorznabCatType.AudioVideo);
            //AddCategoryMapping("cat_id", TorznabCatType.AudioAudiobook);
            AddCategoryMapping("8", TorznabCatType.AudioLossless);
            //AddCategoryMapping("cat_id", TorznabCatType.AudioOther);
            //AddCategoryMapping("cat_id", TorznabCatType.AudioForeign);
            AddCategoryMapping("21", TorznabCatType.PC);
            AddCategoryMapping("22", TorznabCatType.PC0day);
            AddCategoryMapping("4", TorznabCatType.PCISO);
            AddCategoryMapping("2", TorznabCatType.PCMac);
            //AddCategoryMapping("cat_id", TorznabCatType.PCPhoneOther);
            //Games/PC-ISO, Games/PC-Rips
            AddMultiCategoryMapping(TorznabCatType.PCGames, 4, 21);
            //AddCategoryMapping("cat_id", TorznabCatType.PCPhoneIOS);
            //AddCategoryMapping("cat_id", TorznabCatType.PCPhoneAndroid);
            AddCategoryMapping("45", TorznabCatType.TV);
            //AddCategoryMapping("cat_id", TorznabCatType.TVWEBDL);
            //AddCategoryMapping("cat_id", TorznabCatType.TVFOREIGN);
            //TV/DVDR, TV/SDx264, TV/XViD
            AddMultiCategoryMapping(TorznabCatType.TVSD, 43, 41, 7);
            AddCategoryMapping("42", TorznabCatType.TVHD);
            //AddCategoryMapping("cat_id", TorznabCatType.TVOTHER);
            //AddCategoryMapping("cat_id", TorznabCatType.TVSport);
            AddCategoryMapping("23", TorznabCatType.TVAnime);
            //AddCategoryMapping("cat_id", TorznabCatType.TVDocumentary);
            //AddCategoryMapping("cat_id", TorznabCatType.XXX);
            //AddCategoryMapping("cat_id", TorznabCatType.XXXDVD);
            //AddCategoryMapping("cat_id", TorznabCatType.XXXWMV);
            //AddCategoryMapping("cat_id", TorznabCatType.XXXXviD);
            //AddCategoryMapping("cat_id", TorznabCatType.XXXx264);
            //AddCategoryMapping("cat_id", TorznabCatType.XXXOther);
            //AddCategoryMapping("cat_id", TorznabCatType.XXXImageset);
            //AddCategoryMapping("cat_id", TorznabCatType.XXXPacks);
            //AddCategoryMapping("cat_id", TorznabCatType.Other);
            //AddCategoryMapping("cat_id", TorznabCatType.OtherMisc);
            //AddCategoryMapping("cat_id", TorznabCatType.OtherHashed);
            AddCategoryMapping("36", TorznabCatType.Books);
            AddCategoryMapping("36", TorznabCatType.BooksEbook);
            //AddCategoryMapping("cat_id", TorznabCatType.BooksComics);
            //AddCategoryMapping("cat_id", TorznabCatType.BooksMagazines);
            //AddCategoryMapping("cat_id", TorznabCatType.BooksTechnical);
            //AddCategoryMapping("cat_id", TorznabCatType.BooksOther);
            //AddCategoryMapping("cat_id", TorznabCatType.BooksForeign);
        }

        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value }
            };

            //--need to do an initial request to get PHP session cookie (any better way to do this?)
            var homePageLoad = await RequestLoginAndFollowRedirect(LandingPageURL, new Dictionary<string, string> { }, null, true, null, SiteLink);

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, homePageLoad.Cookies, true, null, LoginUrl);
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("/logout.php"), () =>
            {
                CQ dom = result.Content;
                var messageEl = dom[".error"];
                var errorMessage = messageEl.Text().Trim();
                //--CloudFlare error?
                if (errorMessage == "")
                {
                    errorMessage = result.Content;
                }
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();
            var searchUrl = SearchUrl;

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchUrl += "?titleonly=1&search=" + HttpUtility.UrlEncode(searchString);
            }
            string.Format(SearchUrl, HttpUtility.UrlEncode(searchString));

            var cats = MapTorznabCapsToTrackers(query);
            if (cats.Count > 0)
            {
                foreach (var cat in cats)
                {
                    searchUrl += "&c" + cat + "=1";
                }
            }

            var results = await RequestStringWithCookiesAndRetry(searchUrl);
            try
            {
                CQ dom = results.Content;

                //--table header is the first <tr> in table body, get all rows except this
                CQ qRows = dom["#torrents-table > tbody > tr:not(:first-child)"];

                foreach (var row in qRows)
                {
                    var release = new ReleaseInfo();

                    var qRow = row.Cq();

                    var debug = qRow.Html();

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;

                    CQ qLink = qRow.Find(".br_right > a").First();
                    release.Guid = new Uri(SiteLink + qLink.Attr("href"));
                    release.Comments = new Uri(SiteLink + qLink.Attr("href") + "&tocomm=1");
                    release.Title = qLink.Find("b").Text();
                    release.Description = release.Title;

                    release.Link = new Uri(SiteLink + qRow.Find("td:nth-child(4) > a").Attr("href"));

                    var dateString = qRow.Find("td:nth-child(6) nobr")[0].InnerText.Trim();
                    //"2015-04-25 23:38:12"
                    //"yyyy-MMM-dd hh:mm:ss"
                    release.PublishDate = DateTime.ParseExact(dateString, "yyyy-MM-ddHH:mm:ss", CultureInfo.InvariantCulture);

                    var sizeStr = qRow.Children().ElementAt(6).InnerText.Trim();
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    release.Seeders = ParseUtil.CoerceInt(qRow.Find("td:nth-child(9)").Text());
                    release.Peers = release.Seeders + ParseUtil.CoerceInt(qRow.Find("td:nth-child(10)").Text());

                    var category = qRow.Find(".br_type > a").Attr("href").Replace("browse.php?cat=", string.Empty);
                    release.Category = MapTrackerCatToNewznab(category);

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