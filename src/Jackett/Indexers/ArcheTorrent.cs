using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Parser.Html;
using Newtonsoft.Json.Linq;
using NLog;
using Jackett.Models;
using Jackett.Models.IndexerConfig;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;

namespace Jackett.Indexers
{
    class ArcheTorrent : BaseWebIndexer
    {
        string LoginUrl { get { return SiteLink + "account-login.php"; } }
        string BrowseUrl { get { return SiteLink + "torrents-search.php"; } }
        string DownloadUrl { get { return SiteLink + "download.php"; } }

        new ConfigurationDataBasicLogin configData
        {
            get { return (ConfigurationDataBasicLogin)base.configData; }
            set { base.configData = value; }
        }

        public ArcheTorrent(IIndexerConfigurationService configService, IWebClient webClient, Logger logger, IProtectionService protectionService)
            : base(name: "Arche Torrent",
                description: "French Torrent Tracker",
                link: "https://www.archetorrent.com/",
                configService: configService,
                logger: logger,
                p: protectionService,
                client: webClient,
                configData: new ConfigurationDataBasicLogin()
                )
        {
            Encoding = Encoding.GetEncoding("UTF-8");
            Language = "fr-fr";
            Type = "private";

            AddCategoryMapping (18, TorznabCatType.PC, "Applications: PC");
            AddCategoryMapping (19, TorznabCatType.PCMac, "Applications: Mac");
            AddCategoryMapping (54, TorznabCatType.PC, "Applications: linux");
            AddCategoryMapping (56, TorznabCatType.XXXOther, "Autres: ebook xxx");
            AddCategoryMapping (36, TorznabCatType.Books, "Autres: E-Books");
            AddCategoryMapping (37, TorznabCatType.Other, "Autres: Images");
            AddCategoryMapping (38, TorznabCatType.PCPhoneOther, "Autres: Telephone-mobile");
            AddCategoryMapping (47, TorznabCatType.Movies, "Films: Animé");
            AddCategoryMapping (1, TorznabCatType.MoviesDVD, "Films: DVD");
            AddCategoryMapping (2, TorznabCatType.MoviesSD, "Films: Dvdrip");
            AddCategoryMapping (68, TorznabCatType.Movies, "Films: TAT Releases");
            AddCategoryMapping (70, TorznabCatType.MoviesHD, "Films: UHD 4K");
            AddCategoryMapping (69, TorznabCatType.Movies, "Films: Retro");
            AddCategoryMapping (3, TorznabCatType.MoviesHD, "Films: HD1080");
            AddCategoryMapping (42, TorznabCatType.MoviesHD, "Films: HD720");
            AddCategoryMapping (4, TorznabCatType.Movies, "Films: Cam/Ts");
            AddCategoryMapping (22, TorznabCatType.MoviesBluRay, "Films: bluray");
            AddCategoryMapping (23, TorznabCatType.Movies3D, "Films: 3D");
            AddCategoryMapping (24, TorznabCatType.MoviesForeign, "Films: VOSTFR");
            AddCategoryMapping (25, TorznabCatType.XXX, "Films: Adulte");
            AddCategoryMapping (48, TorznabCatType.TVDocumentary, "Films: Documentaire");
            AddCategoryMapping (49, TorznabCatType.MoviesOther, "Films: Spectacle");
            AddCategoryMapping (51, TorznabCatType.MoviesSD, "Films: R5");
            AddCategoryMapping (52, TorznabCatType.MoviesSD, "Films: bdrip");
            AddCategoryMapping (53, TorznabCatType.MoviesSD, "Films: brrip");
            AddCategoryMapping (55, TorznabCatType.MoviesDVD, "Films: dvd-pack");
            AddCategoryMapping (57, TorznabCatType.Movies, "Films: manga");
            AddCategoryMapping (59, TorznabCatType.MoviesWEBDL, "Films: Webrip");
            AddCategoryMapping (63, TorznabCatType.MoviesSD, "Films: M-HD");
            AddCategoryMapping (10, TorznabCatType.PCGames, "Jeux: PC");
            AddCategoryMapping (11, TorznabCatType.ConsoleOther, "Jeux: PS2");
            AddCategoryMapping (43, TorznabCatType.ConsolePS3, "Jeux: PS3");
            AddCategoryMapping (12, TorznabCatType.ConsolePSP, "Jeux: PSP");
            AddCategoryMapping (14, TorznabCatType.ConsoleXbox360, "Jeux: Xbox360");
            AddCategoryMapping (44, TorznabCatType.ConsoleWii, "Jeux: Wii");
            AddCategoryMapping (45, TorznabCatType.ConsoleNDS, "Jeux: DS");
            AddCategoryMapping (27, TorznabCatType.AudioVideo, "Musique: Clip Video");
            AddCategoryMapping (62, TorznabCatType.TVSD, "Serie tv: TV BDRip");
            AddCategoryMapping (5, TorznabCatType.TVSD, "Serie tv: Dvdrip");
            AddCategoryMapping (41, TorznabCatType.TVHD, "Serie tv: Hd");
            AddCategoryMapping (60, TorznabCatType.TVSD, "Serie tv: pack série tv");
            AddCategoryMapping (64, TorznabCatType.TVFOREIGN, "Serie tv: vostfr");
            AddCategoryMapping (65, TorznabCatType.TVHD, "Serie tv: Série tv 720P");
            AddCategoryMapping (66, TorznabCatType.TVHD, "Serie tv: Série tv 1080P");
            AddCategoryMapping (67, TorznabCatType.TVHD, "Serie tv: Série tv PackHD");
            AddCategoryMapping (73, TorznabCatType.TVAnime, "Serie tv: Anime ");
            AddCategoryMapping (72, TorznabCatType.TVSport, "Sport: sport");
            AddCategoryMapping (61, TorznabCatType.TVSD, "Tv: DVDRip");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string>
            {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value }
            };

            var result = await PostDataWithCookies(LoginUrl, pairs);

            await ConfigureIfOK(result.Cookies, result.Cookies != null, () =>
           {
               var errorMessage = result.Content;
               throw new ExceptionWithConfigData(errorMessage, configData);
           });

            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();

            var arraySearchString = searchString.Split(' ');
            searchString = "";
            foreach (var subSearchString in arraySearchString) 
            {
                searchString += "+" + subSearchString + " ";
            }
            searchString = searchString.Trim ();

            var queryCollection = new Dictionary<string, string>();
            queryCollection.Add ("search", searchString);
            queryCollection.Add ("cat", "0");
            queryCollection.Add ("incldead", "0");
            queryCollection.Add ("freeleech", "0");
            queryCollection.Add ("lang", "0");

            var searchUrl = BrowseUrl + "?" + queryCollection.GetQueryString ();

            var results = await RequestStringWithCookies (searchUrl);

            try
            {
                var RowsSelector = "table.ttable_headinner tr.t-row";
                var SearchResultParser = new HtmlParser();
                var SearchResultDocument = SearchResultParser.Parse(results.Content);
                var Rows = SearchResultDocument.QuerySelectorAll(RowsSelector);
                var lastDate = DateTime.Now;

                foreach (var Row in Rows)
                {
                    var release = new ReleaseInfo();
                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 0;

                    var category = Row.QuerySelector("td:nth-child(1) a");
                    var title = Row.QuerySelector("td:nth-child(2) a b");

                    var link = Row.QuerySelector("td:nth-child(2) a");
                    var Size = Row.QuerySelector("td:nth-child(5)");
                    var Grabs = Row.QuerySelector("td:nth-child(8) font b");
                    var Seeders = Row.QuerySelector("td:nth-child(6) font b");
                    var Leechers = Row.QuerySelector("td:nth-child(7) font b");
                    var categoryId = category.GetAttribute("href").Split('=').Last();
                    var torrentId = link.GetAttribute ("href").Split ('&').First ().Split ('=').Last ();

                    release.Title = title.TextContent;
                    release.Category = MapTrackerCatToNewznab(categoryId);
                    release.Link = new Uri(DownloadUrl + "?id=" + torrentId);
                    release.Guid = release.Link;
                    release.Size = ReleaseInfo.GetBytes(Size.TextContent);
                    release.Seeders = ParseUtil.CoerceInt(Seeders.TextContent);
                    release.Peers = ParseUtil.CoerceInt(Leechers.TextContent) + release.Seeders;
                    release.Grabs = ParseUtil.CoerceLong(Grabs.TextContent);

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
