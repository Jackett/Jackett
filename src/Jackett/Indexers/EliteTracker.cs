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
    class EliteTracker : BaseIndexer, IIndexer
    {
        string LoginUrl { get { return SiteLink + "takelogin.php"; } }
        string BrowseUrl { get { return SiteLink + "browse.php"; } }

        new ConfigurationDataBasicLogin configData
        {
            get { return (ConfigurationDataBasicLogin)base.configData; }
            set { base.configData = value; }
        }

        public EliteTracker(IIndexerManagerService indexerManager, IWebClient webClient, Logger logger, IProtectionService protectionService)
            : base(name: "Elite-Tracker",
                description: "French Torrent Tracker",
                link: "https://elite-tracker.net/",
                manager: indexerManager,
                logger: logger,
                p: protectionService,
                client: webClient,
                configData: new ConfigurationDataBasicLogin()
                )
        {
            Encoding = Encoding.GetEncoding("UTF-8");
            Language = "fr-fr";

            // Audio
            AddCategoryMapping(23, TorznabCatType.Audio, "Music");
            AddCategoryMapping(26, TorznabCatType.AudioVideo, "Music Clip/Concert");
            AddCategoryMapping(61, TorznabCatType.AudioLossless, "Music LossLess");
            AddCategoryMapping(60, TorznabCatType.AudioMP3, "Music MP3");

            // Movies
            AddCategoryMapping(7, TorznabCatType.Movies, "Films");
            AddCategoryMapping(11, TorznabCatType.MoviesDVD, "Films DVD");
            AddCategoryMapping(70, TorznabCatType.Movies3D, "Films 3D");
            AddCategoryMapping(49, TorznabCatType.MoviesBluRay, "Films BluRay");

            // TV
            AddCategoryMapping(30, TorznabCatType.TV, "Séries");
            AddCategoryMapping(27, TorznabCatType.TVAnime, "Animes");
            AddCategoryMapping(35, TorznabCatType.TVSport, "Sport");
            AddCategoryMapping(38, TorznabCatType.TVDocumentary, "Documentaires");
            AddCategoryMapping(47, TorznabCatType.TVOTHER, "TV SPECTACLES / EMISSIONS");

            // Apps
            AddCategoryMapping(4, TorznabCatType.PC, "PC Apps");
            AddCategoryMapping(74, TorznabCatType.PCPhoneAndroid, "Android Apps");
            AddCategoryMapping(57, TorznabCatType.PCPhoneIOS, "IOS Apps");
            AddCategoryMapping(5, TorznabCatType.PCMac, "MAC Apps");

            // Games
            AddCategoryMapping(23, TorznabCatType.Console, "Console");
            AddCategoryMapping(76, TorznabCatType.Console3DS, "Console 3DS");
            AddCategoryMapping(18, TorznabCatType.ConsoleNDS, "Console NDS");
            AddCategoryMapping(58, TorznabCatType.ConsolePS3, "Console PS3");
            AddCategoryMapping(81, TorznabCatType.ConsolePS4, "Console PS4");
            AddCategoryMapping(20, TorznabCatType.ConsolePSP, "Console PSP");
            AddCategoryMapping(19, TorznabCatType.ConsoleWii, "Console Wii");
            AddCategoryMapping(83, TorznabCatType.ConsoleWiiU, "Console WiiU");
            AddCategoryMapping(16, TorznabCatType.ConsoleXbox, "Console Xbox");
            AddCategoryMapping(17, TorznabCatType.ConsoleXbox360, "Console Xbox360");

            // Others
            AddCategoryMapping(34, TorznabCatType.Books, "EBooks");
            AddCategoryMapping(37, TorznabCatType.XXX, "XXX");
        }

        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
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

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var categoriesList = MapTorznabCapsToTrackers(query);
            var searchString = query.GetQueryString();

            var queryCollection = new Dictionary<string, string>();
            queryCollection.Add("search_type", "t_name");
            queryCollection.Add("do", "search");
            queryCollection.Add("keywords", searchString);

            if (categoriesList.Count > 0)
            {
                queryCollection.Add("category", categoriesList[0]);
            }

            var results = await PostDataWithCookies(BrowseUrl, queryCollection);

            try
            {
                var RowsSelector = "table[id='sortabletable'] > tbody > tr";
                var SearchResultParser = new HtmlParser();
                var SearchResultDocument = SearchResultParser.Parse(results.Content);
                var Rows = SearchResultDocument.QuerySelectorAll(RowsSelector);

                foreach (var Row in Rows.Skip(1))
                {
                    var release = new ReleaseInfo();
                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 0;

                    var title = Row.QuerySelector("td:nth-child(2)").QuerySelector("div > div");
                    var added = Row.QuerySelector("td:nth-child(2)");
                    var link = Row.QuerySelector("td:nth-child(3)").QuerySelector("a");
                    var comments = Row.QuerySelector("td:nth-child(3)").QuerySelector("a");
                    var Size = Row.QuerySelector("td:nth-child(5)");
                    var Grabs = Row.QuerySelector("td:nth-child(6)").QuerySelector("a");
                    var Seeders = Row.QuerySelector("td:nth-child(7)").QuerySelector("a");
                    var Leechers = Row.QuerySelector("td:nth-child(8)").QuerySelector("a");

                    release.Title = title.InnerHtml;

                    var addedRegEx = Regex.Matches(added.TextContent, @"[0-9]{2}\.[0-9]{2}\.[0-9]{4}\s[0-9]{2}:[0-9]{2}", RegexOptions.Multiline);

                    if (addedRegEx.Count > 0)
                    {
                        release.PublishDate = DateTime.ParseExact(addedRegEx[0].Value, "dd.MM.yyyy HH:mm",  CultureInfo.InvariantCulture);
                    }

                    release.Category = TvCategoryParser.ParseTvShowQuality(release.Title);
                    release.Link = new Uri(link.GetAttribute("href"));
                    release.Comments = new Uri(comments.GetAttribute("href"));
                    release.Guid = release.Link;
                    release.Size = ReleaseInfo.GetBytes(Size.TextContent);
                    release.Seeders = ParseUtil.CoerceInt(Seeders.TextContent);
                    release.Peers = ParseUtil.CoerceInt(Leechers.TextContent) + release.Seeders;
                    release.Grabs = ReleaseInfo.GetBytes(Grabs.TextContent);
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
