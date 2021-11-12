using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Dom;
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
    public class BitCityReloaded : BaseWebIndexer
    {
        private string LoginUrl => SiteLink + "login/index.php";
        private string BrowseUrl => SiteLink + "uebersicht.php";
        private readonly TimeZoneInfo germanyTz = TimeZoneInfo.CreateCustomTimeZone("W. Europe Standard Time", new TimeSpan(1, 0, 0), "W. Europe Standard Time", "W. Europe Standard Time");

        private new ConfigurationDataBasicLoginWithRSSAndDisplay configData
        {
            get => (ConfigurationDataBasicLoginWithRSSAndDisplay)base.configData;
            set => base.configData = value;
        }

        public BitCityReloaded(IIndexerConfigurationService configService, WebClient wc, Logger l,
            IProtectionService ps, ICacheService cs)
            : base(id: "bitcityreloaded",
                   name: "Bit-City Reloaded",
                   description: "A German general tracker.",
                   link: "https://bc-reloaded.net/",
                   caps: new TorznabCapabilities
                   {
                       TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                       },
                       MovieSearchParams = new List<MovieSearchParam>
                       {
                           MovieSearchParam.Q
                       },
                       MusicSearchParams = new List<MusicSearchParam>
                       {
                           MusicSearchParam.Q
                       },
                       BookSearchParams = new List<BookSearchParam>
                       {
                           BookSearchParam.Q
                       }
                   },
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataBasicLoginWithRSSAndDisplay("Only the results from the first search result page are shown, adjust your profile settings to show a reasonable amount (it looks like there's no maximum)."))
        {
            Encoding = Encoding.GetEncoding("iso-8859-1");
            Language = "de-DE";
            Type = "private";

            AddCategoryMapping(1, TorznabCatType.Other, "Other/Anderes");
            AddCategoryMapping(2, TorznabCatType.TVAnime, "TV/Anime");
            AddCategoryMapping(34, TorznabCatType.PC, "Appz/Linux");
            AddCategoryMapping(35, TorznabCatType.PCMac, "Appz/Mac");
            AddCategoryMapping(36, TorznabCatType.PC, "Appz/Other");
            AddCategoryMapping(20, TorznabCatType.PC, "Appz/Win");
            AddCategoryMapping(3, TorznabCatType.TVDocumentary, "TV/Doku/Alle Formate");
            AddCategoryMapping(4, TorznabCatType.Books, "EBooks");
            AddCategoryMapping(12, TorznabCatType.ConsolePS4, "Games/PS & PSx");
            AddCategoryMapping(11, TorznabCatType.ConsoleNDS, "Games/Nintendo DS");
            AddCategoryMapping(10, TorznabCatType.PCGames, "Games/PC");
            AddCategoryMapping(13, TorznabCatType.ConsoleWii, "Games/Wii");
            AddCategoryMapping(14, TorznabCatType.ConsoleXBox, "Games/Xbox & 360");
            AddCategoryMapping(15, TorznabCatType.PCMobileOther, "Handy & PDA");
            AddCategoryMapping(16, TorznabCatType.AudioAudiobook, "Hörspiel/Hörbuch");
            AddCategoryMapping(30, TorznabCatType.Other, "Other/International");
            AddCategoryMapping(17, TorznabCatType.Other, "Other/MegaPack");
            AddCategoryMapping(43, TorznabCatType.Movies3D, "Movie/3D");
            AddCategoryMapping(5, TorznabCatType.MoviesDVD, "Movie/DVD/R");
            AddCategoryMapping(6, TorznabCatType.MoviesHD, "Movie/HD 1080p");
            AddCategoryMapping(7, TorznabCatType.MoviesHD, "Movie/HD 720p");
            AddCategoryMapping(32, TorznabCatType.MoviesOther, "Movie/TVRip");
            AddCategoryMapping(9, TorznabCatType.MoviesOther, "Movie/XviD,DivX,h264");
            AddCategoryMapping(26, TorznabCatType.XXX, "XXX/Movie");
            AddCategoryMapping(41, TorznabCatType.XXXOther, "XXX/Movie/Other");
            AddCategoryMapping(42, TorznabCatType.XXXPack, "XXX/Movie/Pack");
            AddCategoryMapping(45, TorznabCatType.MoviesHD, "Movies/4K");
            AddCategoryMapping(33, TorznabCatType.MoviesBluRay, "Movies/BluRay");
            AddCategoryMapping(18, TorznabCatType.Audio, "Musik");
            AddCategoryMapping(19, TorznabCatType.AudioVideo, "Musik Videos");
            AddCategoryMapping(44, TorznabCatType.TVOther, "Serie/DVD/R");
            AddCategoryMapping(22, TorznabCatType.TVHD, "Serie/HDTV");
            AddCategoryMapping(38, TorznabCatType.TV, "Serie/Pack");
            AddCategoryMapping(23, TorznabCatType.TVOther, "Serie/XviD,DivX,h264");
            AddCategoryMapping(25, TorznabCatType.TVSport, "TV/Sport");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string>
            {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value }
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, LoginUrl);
            await ConfigureIfOK(result.Cookies, result.ContentString != null && result.ContentString.Contains("logout.php"), () =>
                {
                    var parser = new HtmlParser();
                    var dom = parser.ParseDocument(result.ContentString);
                    var errorMessage = dom.QuerySelector("#login_error").Text().Trim();
                    throw new ExceptionWithConfigData(errorMessage, configData);
                });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            var searchString = query.GetQueryString();
            var searchUrl = BrowseUrl;
            var queryCollection = new NameValueCollection();
            queryCollection.Add("showsearch", "0");
            queryCollection.Add("incldead", "1");
            queryCollection.Add("blah", "0");
            queryCollection.Add("team", "0");
            queryCollection.Add("orderby", "added");
            queryCollection.Add("sort", "desc");

            if (!string.IsNullOrWhiteSpace(searchString))
                queryCollection.Add("search", searchString);

            foreach (var cat in MapTorznabCapsToTrackers(query))
                queryCollection.Add("c" + cat, "1");

            searchUrl += "?" + queryCollection.GetQueryString();

            var response = await RequestWithCookiesAndRetryAsync(searchUrl, referer: BrowseUrl);
            var results = response.ContentString;
            try
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(results);
                var rows = dom.QuerySelectorAll("table.tableinborder[cellpadding=0] > tbody > tr");

                foreach (var row in rows)
                {
                    var release = new ReleaseInfo();
                    release.MinimumRatio = 0.7;
                    release.MinimumSeedTime = 172800; // 48 hours
                    release.DownloadVolumeFactor = 1;
                    release.UploadVolumeFactor = 1;

                    var flagImgs = row.QuerySelectorAll("table tbody tr:nth-of-type(1) td > img");
                    var flags = new List<string>();
                    foreach (var flagImg in flagImgs)
                    {
                        var flag = flagImg.GetAttribute("src").Replace("pic/torrent_", "").Replace(".gif", "").ToUpper();
                        if (flag == "OU")
                            release.DownloadVolumeFactor = 0;
                        else
                            flags.Add(flag);
                    }

                    var titleLink = row.QuerySelector("table tbody tr:nth-of-type(1) td a:has(b)");
                    var dlLink = row.QuerySelector("td.tableb > a:has(img[title=\"Torrent herunterladen\"])");
                    release.Details = new Uri(SiteLink + titleLink.GetAttribute("href").Replace("&hit=1", ""));
                    release.Link = new Uri(SiteLink + dlLink.GetAttribute("href"));
                    release.Title = titleLink.TextContent.Trim();

                    if (!query.MatchQueryStringAND(release.Title))
                        continue;

                    release.Description = string.Join(", ", flags);
                    release.Guid = release.Link;

                    var dateStr = row.QuerySelector("table tbody tr:nth-of-type(2) td:nth-of-type(5)").Html().Replace("&nbsp;", " ").Trim();
                    var dateGerman = DateTime.SpecifyKind(DateTime.ParseExact(dateStr, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture), DateTimeKind.Unspecified);
                    var pubDateUtc = TimeZoneInfo.ConvertTimeToUtc(dateGerman, germanyTz);
                    release.PublishDate = pubDateUtc.ToLocalTime();

                    var sizeStr = row.QuerySelector("table tbody tr:nth-of-type(2)").QuerySelector("td b").TextContent.Trim();
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    release.Seeders = ParseUtil.CoerceInt(row.QuerySelector("table tbody tr:nth-of-type(2) td:nth-of-type(2) b:nth-of-type(1) font").TextContent.Trim());
                    release.Peers = ParseUtil.CoerceInt(row.QuerySelector("table tbody tr:nth-of-type(2) td:nth-of-type(2) b:nth-of-type(2) font").TextContent.Trim()) + release.Seeders;

                    var catId = row.QuerySelector("td:nth-of-type(1) a").GetAttribute("href").Split('=')[1];
                    release.Category = MapTrackerCatToNewznab(catId);

                    var files = row.QuerySelector("td:has(a[href*=\"&filelist=1\"])> b:nth-child(2)").TextContent;
                    release.Files = ParseUtil.CoerceInt(files);

                    var grabs = row.QuerySelector("td:has(a[href*=\"&tosnatchers=1\"])> b:nth-child(1)").TextContent;
                    release.Grabs = ParseUtil.CoerceInt(grabs);

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(results, ex);
            }

            return releases;
        }
    }
}

