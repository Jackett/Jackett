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
    public class Partis : BaseWebIndexer
    {
        private string LoginUrl => SiteLink + "user/login/";
        private string SearchUrl => SiteLink + "torrent/show/";

        private new ConfigurationDataBasicLogin configData
        {
            get => (ConfigurationDataBasicLogin)base.configData;
            set => base.configData = value;
        }

        public Partis(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(id: "partis",
                   name: "Partis",
                   description: "Partis is a SLOVENIAN Private Torrent Tracker",
                   link: "https://www.partis.si/",
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
                   configData: new ConfigurationDataBasicLogin())
        {
            Encoding = Encoding.UTF8;
            Language = "sl-sl";
            Type = "private";

            // Blu Ray
            AddCategoryMapping(40, TorznabCatType.MoviesBluRay, "Blu-Ray 1080p/i");
            AddCategoryMapping(42, TorznabCatType.MoviesBluRay, "Blu-Ray 720p/i");
            AddCategoryMapping(43, TorznabCatType.MoviesBluRay, "Blu-Ray B-Disc");
            AddCategoryMapping(41, TorznabCatType.MoviesBluRay, "Blu-Ray 3D");
            AddCategoryMapping(44, TorznabCatType.MoviesBluRay, "Blu-Ray Remux");
            AddCategoryMapping(45, TorznabCatType.MoviesBluRay, "Blu-Ray Remux/Disc");

            // UHD
            AddCategoryMapping(32, TorznabCatType.MoviesUHD, "UHD 4K Disc");
            AddCategoryMapping(55, TorznabCatType.MoviesUHD, "UHD 4K Remux");

            // HD
            AddCategoryMapping(20, TorznabCatType.MoviesHD, "HD");
            AddCategoryMapping(4, TorznabCatType.MoviesSD, "DVD-R");
            AddCategoryMapping(7, TorznabCatType.MoviesSD, "XviD");
            AddCategoryMapping(12, TorznabCatType.MoviesSD, "Anime");
            AddCategoryMapping(30, TorznabCatType.MoviesSD, "Risanke");
            AddCategoryMapping(15, TorznabCatType.MoviesSD, "Sport");

            // TV Show
            AddCategoryMapping(53, TorznabCatType.TVWEBDL, "TV WEB-DL");
            AddCategoryMapping(60, TorznabCatType.TVSD, "TV-XviD");
            AddCategoryMapping(38, TorznabCatType.TVSD, "SD-TV");
            AddCategoryMapping(51, TorznabCatType.TVHD, "TV 1080p/i");
            AddCategoryMapping(52, TorznabCatType.TVHD, "TV 720p/i");

            AddCategoryMapping(54, TorznabCatType.MoviesSD, "WEBRip");
            AddCategoryMapping(59, TorznabCatType.MoviesWEBDL, "WEB-DL");
            AddCategoryMapping(24, TorznabCatType.TVDocumentary, "Dokumentarci");

            // Games
            AddCategoryMapping(10, TorznabCatType.PCGames, "PC igre/ISO");
            AddCategoryMapping(11, TorznabCatType.PCGames, "PC igre/Rips/Repack");
            AddCategoryMapping(64, TorznabCatType.PCGames, "PC igre/Update & Patch");

            // Music
            AddCategoryMapping(46, TorznabCatType.AudioLossless, "Glasba/Flac");
            AddCategoryMapping(8, TorznabCatType.AudioOther, "Glasba/Ostalo");
            AddCategoryMapping(47, TorznabCatType.AudioMP3, "Glasba/Mp3");
            AddCategoryMapping(8, TorznabCatType.AudioVideo, "Music DVD");
            AddCategoryMapping(8, TorznabCatType.AudioVideo, "Videospoti");
            AddCategoryMapping(21, TorznabCatType.AudioAudiobook, "AudioBook");
            AddCategoryMapping(3, TorznabCatType.BooksEBook, "eKnjige");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string>
            {
                { "user[username]", configData.Username.Value },
                { "user[password]", configData.Password.Value }
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, string.Empty, false, null, null, true);
            await ConfigureIfOK(result.Cookies, result.ContentString != null && result.ContentString.Contains("/odjava"), () =>
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(result.ContentString);
                var errorMessage = dom.QuerySelector("div.obvet > span.najvecji").TextContent.Trim(); // Prijava ni uspela! obvestilo
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();     //List of releases initialization
            var searchString = query.GetQueryString();  //get search string from query

            WebResult results = null;
            var queryCollection = new NameValueCollection();
            var catList = MapTorznabCapsToTrackers(query);     // map categories from query to indexer specific
            var categ = string.Join(",", catList);

            //create GET request - search URI
            queryCollection.Add("offset", "0");
            queryCollection.Add("keyword", searchString);
            queryCollection.Add("category", categ.TrimStart(','));
            queryCollection.Add("option", "");
            queryCollection.Add("ns", "true");

            //concatenate base search url with query
            var searchUrl = SearchUrl + "?" + queryCollection.GetQueryString();

            // log search URL
            logger.Info(string.Format("Searh URL Partis_: {0}", searchUrl));

            // add necessary headers
            var header = new Dictionary<string, string>
            {
                { "X-requested-with", "XMLHttpRequest" }
            };

            //get results and follow redirect
            results = await RequestWithCookiesAsync(searchUrl, referer: SearchUrl, headers: header);
            await FollowIfRedirect(results, null, null, null, true);

            // are we logged in?
            if (!results.ContentString.Contains("/odjava"))
            {
                await ApplyConfiguration(null);
            }
            // another request with specific query - NEEDED for succesful response - return data
            results = await RequestWithCookiesAsync(
                SiteLink + "brskaj/?rs=false&offset=0", referer: SearchUrl, headers: header);
            await FollowIfRedirect(results, null, null, null, true);

            // parse results
            try
            {
                var RowsSelector = "div.list > div[name=\"torrrow\"]";

                var ResultParser = new HtmlParser();
                var SearchResultDocument = ResultParser.ParseDocument(results.ContentString);
                var Rows = SearchResultDocument.QuerySelectorAll(RowsSelector);
                foreach (var Row in Rows)
                {
                    try
                    {
                        // initialize REleaseInfo
                        var release = new ReleaseInfo
                        {
                            MinimumRatio = 1,
                            MinimumSeedTime = 0
                        };

                        // Get Category
                        var catega = Row.QuerySelector("div.likona div").GetAttribute("alt");
                        release.Category = MapTrackerCatDescToNewznab(catega);

                        var qDetailsLink = Row.QuerySelector("div.listeklink a");

                        // Title and torrent link
                        release.Title = qDetailsLink.TextContent;
                        release.Details = new Uri(SiteLink + qDetailsLink.GetAttribute("href").TrimStart('/'));
                        release.Guid = release.Details;

                        // Date of torrent creation
                        var liopis = Row.QuerySelector("div.listeklink div span.middle");
                        var ind = liopis.TextContent.IndexOf("Naloženo:");
                        var reldate = liopis.TextContent.Substring(ind + 10, 22);
                        release.PublishDate = DateTime.ParseExact(reldate, "dd.MM.yyyy ob HH:mm:ss", CultureInfo.InvariantCulture);

                        // Is freeleech?
                        var checkIfFree = (Row.QuerySelector("div.listeklink div.liopisl img[title=\"freeleech\"]") != null) ? true : false;

                        // Download link
                        var qDownloadLink = Row.QuerySelector("div.data3t a").GetAttribute("href");
                        release.Link = new Uri(SiteLink + qDownloadLink.TrimStart('/'));

                        // Various data - size, seeders, leechers, download count
                        var sel = Row.QuerySelectorAll("div.datat");
                        var size = sel[0].TextContent;
                        release.Size = ReleaseInfo.GetBytes(size);

                        release.Seeders = ParseUtil.CoerceInt(sel[1].TextContent);
                        release.Peers = ParseUtil.CoerceInt(sel[2].TextContent) + release.Seeders;
                        release.Grabs = ParseUtil.CoerceLong(sel[3].TextContent);

                        // Set download/upload factor
                        release.DownloadVolumeFactor = checkIfFree ? 0 : 1;
                        release.UploadVolumeFactor = 1;

                        // Add current release to List
                        releases.Add(release);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(string.Format("{0}: Error while parsing row '{1}':\n\n{2}", Id, Row.OuterHtml, ex));
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
