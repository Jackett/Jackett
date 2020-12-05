using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
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
    public class TorrentBytes : BaseWebIndexer
    {
        private string LoginUrl => SiteLink + "takelogin.php";
        private string SearchUrl => SiteLink + "browse.php";

        private new ConfigurationDataBasicLogin configData => (ConfigurationDataBasicLogin)base.configData;

        public TorrentBytes(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(id: "torrentbytes",
                   name: "TorrentBytes",
                   description: "A decade of TorrentBytes",
                   link: "https://www.torrentbytes.net/",
                   caps: new TorznabCapabilities
                   {
                       TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId
                       },
                       MovieSearchParams = new List<MovieSearchParam>
                       {
                           MovieSearchParam.Q, MovieSearchParam.ImdbId
                       },
                       MusicSearchParams = new List<MusicSearchParam>
                       {
                           MusicSearchParam.Q
                       }
                   },
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   configData: new ConfigurationDataBasicLogin("For best results, change the 'Torrents per page' setting to 100 in your profile on the TorrentBytes webpage."))
        {
            Encoding = Encoding.GetEncoding("iso-8859-1");
            Language = "en-us";
            Type = "private";

            AddCategoryMapping(23, TorznabCatType.TVAnime, "Anime");
            AddCategoryMapping(52, TorznabCatType.PCMac, "Apple/All");
            AddCategoryMapping(22, TorznabCatType.PC, "Apps/misc");
            AddCategoryMapping(1, TorznabCatType.PC, "Apps/PC");
            AddCategoryMapping(28, TorznabCatType.TVForeign, "Foreign Titles");
            AddCategoryMapping(50, TorznabCatType.Console, "Games/Consoles");
            AddCategoryMapping(42, TorznabCatType.PCGames, "Games/Pack");
            AddCategoryMapping(4, TorznabCatType.PCGames, "Games/PC");
            AddCategoryMapping(51, TorznabCatType.PC, "Linux/All");
            AddCategoryMapping(31, TorznabCatType.OtherMisc, "Misc");
            AddCategoryMapping(20, TorznabCatType.MoviesDVD, "Movies/DVD-R");
            AddCategoryMapping(12, TorznabCatType.MoviesBluRay, "Movies/Full Blu-ray");
            AddCategoryMapping(5, TorznabCatType.MoviesHD, "Movies/HD");
            AddCategoryMapping(40, TorznabCatType.Movies, "Movies/Pack");
            AddCategoryMapping(19, TorznabCatType.MoviesSD, "Movies/SD");
            AddCategoryMapping(49, TorznabCatType.MoviesUHD, "Movies/UHD");
            AddCategoryMapping(25, TorznabCatType.Audio, "Music/DVDR");
            AddCategoryMapping(48, TorznabCatType.AudioLossless, "Music/Flac");
            AddCategoryMapping(6, TorznabCatType.AudioMP3, "Music/MP3");
            AddCategoryMapping(43, TorznabCatType.Audio, "Music/Pack");
            AddCategoryMapping(34, TorznabCatType.AudioVideo, "Music/Videos");
            AddCategoryMapping(45, TorznabCatType.MoviesBluRay, "NonScene/BRrip");
            AddCategoryMapping(46, TorznabCatType.MoviesHD, "NonScene/x264");
            AddCategoryMapping(44, TorznabCatType.MoviesSD, "NonScene/Xvid");
            AddCategoryMapping(37, TorznabCatType.TVHD, "TV/BRrip");
            AddCategoryMapping(38, TorznabCatType.TVHD, "TV/HD");
            AddCategoryMapping(41, TorznabCatType.TV, "TV/Pack");
            AddCategoryMapping(33, TorznabCatType.TVSD, "TV/SD");
            AddCategoryMapping(32, TorznabCatType.TVUHD, "TV/UHD");
            AddCategoryMapping(39, TorznabCatType.XXXx264, "XXX/HD");
            AddCategoryMapping(24, TorznabCatType.XXXImageSet, "XXX/IMGSET");
            AddCategoryMapping(21, TorznabCatType.XXXPack, "XXX/Pack");
            AddCategoryMapping(9, TorznabCatType.XXXXviD, "XXX/SD");
            AddCategoryMapping(29, TorznabCatType.XXX, "XXX/Web");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string>
            {
                {"username", configData.Username.Value},
                {"password", configData.Password.Value},
                {"returnto", "/"},
                {"login", "Log in!"}
            };
            var loginPage = await RequestWithCookiesAsync(SiteLink, string.Empty);
            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, loginPage.Cookies, true, SiteLink, SiteLink);
            await ConfigureIfOK(
                result.Cookies, result.ContentString?.Contains("my.php") == true, () =>
                {
                    var parser = new HtmlParser();
                    var dom = parser.ParseDocument(result.ContentString);
                    var messageEl = dom.QuerySelector("td.embedded");
                    var errorMessage = messageEl != null ? messageEl.TextContent : result.ContentString;
                    throw new ExceptionWithConfigData(errorMessage, configData);
                });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            var qc = new NameValueCollection
            {
                {"incldead", "1"}
            };

            if (query.IsImdbQuery)
            {
                qc.Add("search", query.ImdbID);
                qc.Add("sc", "2"); // search in description
            }
            else
            {
                qc.Add("search", query.GetQueryString());
                qc.Add("sc", "1"); // search in title
            }

            foreach (var cat in MapTorznabCapsToTrackers(query))
                qc.Add("c" + cat, "1");

            var searchUrl = SearchUrl + "?" + qc.GetQueryString();
            var response = await RequestWithCookiesAndRetryAsync(searchUrl, referer: SearchUrl);

            if (response.IsRedirect) // re-login
            {
                await ApplyConfiguration(null);
                response = await RequestWithCookiesAndRetryAsync(searchUrl, referer: SearchUrl);
            }

            try
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(response.ContentString);
                var rows = dom.QuerySelectorAll("table > tbody:has(tr > td.colhead) > tr:not(:has(td.colhead))");
                foreach (var row in rows)
                {
                    var release = new ReleaseInfo();
                    var link = row.QuerySelector("td:nth-of-type(2) a:nth-of-type(2)");
                    release.Guid = new Uri(SiteLink + link.GetAttribute("href"));
                    release.Details = release.Guid;
                    release.Title = link.GetAttribute("title");

                    // There isn't a title attribute if the release name isn't truncated.
                    if (string.IsNullOrWhiteSpace(release.Title))
                        release.Title = link.FirstChild.TextContent.Trim();
                    release.Description = release.Title;

                    // If we search an get no results, we still get a table just with no info.
                    if (string.IsNullOrWhiteSpace(release.Title))
                        break;

                    // Check if the release has been assigned a category
                    var qCat = row.QuerySelector("td:nth-of-type(1) a");
                    if (qCat != null)
                    {
                        var cat = qCat.GetAttribute("href").Substring(15);
                        release.Category = MapTrackerCatToNewznab(cat);
                    }

                    var qLink = row.QuerySelector("td:nth-of-type(2) a");
                    release.Link = new Uri(SiteLink + qLink.GetAttribute("href"));
                    var added = row.QuerySelector("td:nth-of-type(5)").TextContent.Trim();
                    release.PublishDate = DateTime.ParseExact(added, "yyyy-MM-ddHH:mm:ss", CultureInfo.InvariantCulture);
                    var sizeStr = row.QuerySelector("td:nth-of-type(7)").TextContent.Trim();
                    release.Size = ReleaseInfo.GetBytes(sizeStr);
                    release.Seeders = ParseUtil.CoerceInt(row.QuerySelector("td:nth-of-type(9)").TextContent.Trim());
                    release.Peers = ParseUtil.CoerceInt(row.QuerySelector("td:nth-of-type(10)").TextContent.Trim()) +
                                    release.Seeders;
                    var files = row.QuerySelector("td:nth-child(3)").TextContent;
                    release.Files = ParseUtil.CoerceInt(files);
                    var grabs = row.QuerySelector("td:nth-child(8)").TextContent;
                    if (grabs != "----")
                        release.Grabs = ParseUtil.CoerceInt(grabs);
                    release.DownloadVolumeFactor =
                        row.QuerySelector("font[color=\"green\"]:contains(\"F\"):contains(\"L\")") != null ? 0 : 1;
                    release.UploadVolumeFactor = 1;
                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(response.ContentString, ex);
            }

            return releases;
        }
    }
}
