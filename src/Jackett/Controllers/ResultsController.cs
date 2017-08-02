using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Xml.Linq;
using Jackett.Indexers;
using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
using NLog;

namespace Jackett.Controllers.V20
{
    [JackettAuthorized]
    [JackettAPINoCache]
    [RoutePrefix("api/v2.0/indexers")]
    public class ResultsController : ApiController, IIndexerController
    {
        public IIndexerManagerService IndexerService { get; private set; }
        public IIndexer CurrentIndexer { get; set; }

        public ResultsController(IIndexerManagerService indexerManagerService, IServerService ss, ICacheService c, IWebClient w, Logger logger)
        {
            IndexerService = indexerManagerService;
            serverService = ss;
            cacheService = c;
            webClient = w;
            this.logger = logger;
        }

        [HttpGet]
        [RequiresIndexer]
        public Models.DTO.ManualSearchResult Results([FromUri]Models.DTO.ApiSearch value)
        {
            var stringQuery = new TorznabQuery();

            var queryStr = value.Query;
            if (queryStr != null)
            {
                var seasonMatch = Regex.Match(queryStr, @"S(\d{2,4})");
                if (seasonMatch.Success)
                {
                    stringQuery.Season = int.Parse(seasonMatch.Groups[1].Value);
                    queryStr = queryStr.Remove(seasonMatch.Index, seasonMatch.Length);
                }

                var episodeMatch = Regex.Match(queryStr, @"E(\d{2,4}[A-Za-z]?)");
                if (episodeMatch.Success)
                {
                    stringQuery.Episode = episodeMatch.Groups[1].Value;
                    queryStr = queryStr.Remove(episodeMatch.Index, episodeMatch.Length);
                }
                queryStr = queryStr.Trim();
            }


            stringQuery.SearchTerm = queryStr;
            stringQuery.Categories = value.Category == 0 ? new int[0] : new int[1] { value.Category };
            stringQuery.ExpandCatsToSubCats();

            // try to build an IMDB Query
            var imdbID = ParseUtil.GetFullImdbID(stringQuery.SanitizedSearchTerm);
            TorznabQuery imdbQuery = null;
            if (imdbID != null)
            {
                imdbQuery = new TorznabQuery()
                {
                    ImdbID = imdbID,
                    Categories = stringQuery.Categories,
                    Season = stringQuery.Season,
                    Episode = stringQuery.Episode,
                };
                imdbQuery.ExpandCatsToSubCats();
            }

            var trackers = IndexerService.GetAllIndexers().Where(t => t.IsConfigured).ToList();
            var indexerId = CurrentIndexer.ID;
            if (!string.IsNullOrWhiteSpace(indexerId) && indexerId != "all")
            {
                trackers = trackers.Where(t => t.ID == indexerId).ToList();
            }

            if (value.Category != 0)
            {
                trackers = trackers.Where(t => t.TorznabCaps.Categories.Select(c => c.ID).Contains(value.Category)).ToList();
            }

            var results = trackers.ToList().AsParallel().SelectMany(indexer =>
            {
                var query = stringQuery;
                // use imdb Query for trackers which support it
                if (imdbQuery != null && indexer.TorznabCaps.SupportsImdbSearch)
                    query = imdbQuery;

                var searchResults = indexer.ResultsForQuery(query).Result;
                cacheService.CacheRssResults(indexer, searchResults);

                return searchResults.AsParallel().Select(result =>
                {
                    var item = AutoMapper.Mapper.Map<TrackerCacheResult>(result);
                    item.Tracker = indexer.DisplayName;
                    item.TrackerId = indexer.ID;
                    item.Peers = item.Peers - item.Seeders; // Use peers as leechers

                    return item;
                });
            }).AsSequential().OrderByDescending(d => d.PublishDate).ToList();

            //ConfigureCacheResults(results);

            var manualResult = new Models.DTO.ManualSearchResult()
            {
                Results = results,
                Indexers = trackers.Select(t => t.DisplayName).ToList()
            };


            if (manualResult.Indexers.Count() == 0)
                manualResult.Indexers = new List<string>() { "None" };

            logger.Info(string.Format("Manual search for \"{0}\" on {1} with {2} results.", stringQuery.GetQueryString(), string.Join(", ", manualResult.Indexers), manualResult.Results.Count()));
            return manualResult;
        }

        [HttpGet]
        [RequiresIndexer]
        public async Task<HttpResponseMessage> Torznab()
        {
            var torznabQuery = TorznabQuery.FromHttpQuery(HttpUtility.ParseQueryString(Request.RequestUri.Query));

            if (string.Equals(torznabQuery.QueryType, "caps", StringComparison.InvariantCultureIgnoreCase))
            {
                return new HttpResponseMessage()
                {
                    Content = new StringContent(CurrentIndexer.TorznabCaps.ToXml(), Encoding.UTF8, "application/xml")
                };
            }

            torznabQuery.ExpandCatsToSubCats();
            var allowBadApiDueToDebug = false;
#if DEBUG
            allowBadApiDueToDebug = Debugger.IsAttached;
#endif

            if (!allowBadApiDueToDebug && !string.Equals(torznabQuery.ApiKey, serverService.Config.APIKey, StringComparison.InvariantCultureIgnoreCase))
            {
                logger.Warn(string.Format("A request from {0} was made with an incorrect API key.", Request.GetOwinContext().Request.RemoteIpAddress));
                return Request.CreateResponse(HttpStatusCode.Forbidden, "Incorrect API key");
            }

            if (!CurrentIndexer.IsConfigured)
            {
                logger.Warn(string.Format("Rejected a request to {0} which is unconfigured.", CurrentIndexer.DisplayName));
                return Request.CreateResponse(HttpStatusCode.Forbidden, "This indexer is not configured.");
            }

            if (torznabQuery.ImdbID != null)
            {
                if (torznabQuery.QueryType != "movie")
                {
                    logger.Warn(string.Format("A non movie request with an imdbid was made from {0}.", Request.GetOwinContext().Request.RemoteIpAddress));
                    return GetErrorXML(201, "Incorrect parameter: only movie-search supports the imdbid parameter");
                }

                if (!string.IsNullOrEmpty(torznabQuery.SearchTerm))
                {
                    logger.Warn(string.Format("A movie-search request from {0} was made contining q and imdbid.", Request.GetOwinContext().Request.RemoteIpAddress));
                    return GetErrorXML(201, "Incorrect parameter: please specify either imdbid or q");
                }

                torznabQuery.ImdbID = ParseUtil.GetFullImdbID(torznabQuery.ImdbID); // normalize ImdbID
                if (torznabQuery.ImdbID == null)
                {
                    logger.Warn(string.Format("A movie-search request from {0} was made with an invalid imdbid.", Request.GetOwinContext().Request.RemoteIpAddress));
                    return GetErrorXML(201, "Incorrect parameter: invalid imdbid format");
                }

                if (!CurrentIndexer.TorznabCaps.SupportsImdbSearch)
                {
                    logger.Warn(string.Format("A movie-search request with imdbid from {0} was made but the indexer {1} doesn't support it.", Request.GetOwinContext().Request.RemoteIpAddress, CurrentIndexer.DisplayName));
                    return GetErrorXML(203, "Function Not Available: imdbid is not supported by this indexer");
                }
            }

            var releases = await CurrentIndexer.ResultsForQuery(torznabQuery);

            // Some trackers do not keep their clocks up to date and can be ~20 minutes out!
            foreach (var release in releases.Where(r => r.PublishDate > DateTime.Now))
            {
                release.PublishDate = DateTime.Now;
            }

            // Some trackers do not support multiple category filtering so filter the releases that match manually.
            int? newItemCount = null;

            // Cache non query results
            if (string.IsNullOrEmpty(torznabQuery.SanitizedSearchTerm))
            {
                newItemCount = cacheService.GetNewItemCount(CurrentIndexer, releases);
                cacheService.CacheRssResults(CurrentIndexer, releases);
            }

            // Log info
            var logBuilder = new StringBuilder();
            if (newItemCount != null)
            {
                logBuilder.AppendFormat(string.Format("Found {0} ({1} new) releases from {2}", releases.Count(), newItemCount, CurrentIndexer.DisplayName));
            }
            else
            {
                logBuilder.AppendFormat(string.Format("Found {0} releases from {1}", releases.Count(), CurrentIndexer.DisplayName));
            }

            if (!string.IsNullOrWhiteSpace(torznabQuery.SanitizedSearchTerm))
            {
                logBuilder.AppendFormat(" for: {0}", torznabQuery.GetQueryString());
            }

            logger.Info(logBuilder.ToString());

            var serverUrl = string.Format("{0}://{1}:{2}{3}", Request.RequestUri.Scheme, Request.RequestUri.Host, Request.RequestUri.Port, serverService.BasePath());
            var resultPage = new ResultPage(new ChannelInfo
            {
                Title = CurrentIndexer.DisplayName,
                Description = CurrentIndexer.DisplayDescription,
                Link = new Uri(CurrentIndexer.SiteLink),
                ImageUrl = new Uri(serverUrl + "logos/" + CurrentIndexer.ID + ".png"),
                ImageTitle = CurrentIndexer.DisplayName,
                ImageLink = new Uri(CurrentIndexer.SiteLink),
                ImageDescription = CurrentIndexer.DisplayName
            });


            foreach (var result in releases)
            {
                var clone = AutoMapper.Mapper.Map<ReleaseInfo>(result);
                clone.Link = serverService.ConvertToProxyLink(clone.Link, serverUrl, result.Origin.ID, "dl", result.Title + ".torrent");
                resultPage.Releases.Add(clone);
            }

            var xml = resultPage.ToXml(new Uri(serverUrl));
            // Force the return as XML
            return new HttpResponseMessage()
            {
                Content = new StringContent(xml, Encoding.UTF8, "application/rss+xml")
            };
        }

        public HttpResponseMessage GetErrorXML(int code, string description)
        {
            var xdoc = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement("error",
                    new XAttribute("code", code.ToString()),
                    new XAttribute("description", description)
                )
            );

            var xml = xdoc.Declaration.ToString() + Environment.NewLine + xdoc.ToString();

            return new HttpResponseMessage()
            {
                Content = new StringContent(xml, Encoding.UTF8, "application/xml")
            };
        }


        public static int[] MOVIE_CATS
        {
            get
            {
                var torznabQuery = new TorznabQuery()
                {
                    Categories = new int[1] { TorznabCatType.Movies.ID },
                };

                torznabQuery.ExpandCatsToSubCats();
                return torznabQuery.Categories;
            }
        }

        [HttpGet]
        [RequiresIndexer]
        public async Task<HttpResponseMessage> Potato([FromUri]TorrentPotatoRequest request)
        {
            var allowBadApiDueToDebug = false;
#if DEBUG
            allowBadApiDueToDebug = Debugger.IsAttached;
#endif

            if (!allowBadApiDueToDebug && !string.Equals(request.passkey, serverService.Config.APIKey, StringComparison.InvariantCultureIgnoreCase))
            {
                logger.Warn(string.Format("A request from {0} was made with an incorrect API key.", Request.GetOwinContext().Request.RemoteIpAddress));
                return Request.CreateResponse(HttpStatusCode.Forbidden, "Incorrect API key");
            }

            if (!CurrentIndexer.IsConfigured)
            {
                logger.Warn(string.Format("Rejected a request to {0} which is unconfigured.", CurrentIndexer.DisplayName));
                return Request.CreateResponse(HttpStatusCode.Forbidden, "This indexer is not configured.");
            }

            if (!CurrentIndexer.TorznabCaps.Categories.Select(c => c.ID).Any(i => MOVIE_CATS.Contains(i)))
            {
                logger.Warn(string.Format("Rejected a request to {0} which does not support searching for movies.", CurrentIndexer.DisplayName));
                return Request.CreateResponse(HttpStatusCode.Forbidden, "This indexer does not support movies.");
            }

            var year = 0;

            var omdbApiKey = serverService.Config.OmdbApiKey;
            if (!request.imdbid.IsNullOrEmptyOrWhitespace() && !omdbApiKey.IsNullOrEmptyOrWhitespace())
            {
                // We are searching by IMDB id so look up the name
                var resolver = new OmdbResolver(webClient, omdbApiKey.ToNonNull());
                var movie = await resolver.MovieForId(request.imdbid.ToNonNull());
                request.search = movie.Title;
                year = ParseUtil.CoerceInt(movie.Year);
            }

            var torznabQuery = new TorznabQuery()
            {
                ApiKey = request.passkey,
                Categories = MOVIE_CATS,
                SearchTerm = request.search,
                ImdbID = request.imdbid,
                QueryType = "TorrentPotato"
            };

            IEnumerable<ReleaseInfo> releases = new List<ReleaseInfo>();

            if (CurrentIndexer.CanHandleQuery(torznabQuery))
                releases = await CurrentIndexer.ResultsForQuery(torznabQuery);

            // Cache non query results
            if (string.IsNullOrEmpty(torznabQuery.SanitizedSearchTerm))
            {
                cacheService.CacheRssResults(CurrentIndexer, releases);
            }

            var serverUrl = string.Format("{0}://{1}:{2}{3}", Request.RequestUri.Scheme, Request.RequestUri.Host, Request.RequestUri.Port, serverService.BasePath());
            var potatoResponse = new TorrentPotatoResponse();

            if (!torznabQuery.SanitizedSearchTerm.IsNullOrEmptyOrWhitespace())
                releases = TorznabUtil.FilterResultsToTitle(releases, torznabQuery.SanitizedSearchTerm, year);
            if (!torznabQuery.ImdbID.IsNullOrEmptyOrWhitespace())
                releases = TorznabUtil.FilterResultsToImdb(releases, request.imdbid);

            foreach (var r in releases)
            {
                var release = AutoMapper.Mapper.Map<ReleaseInfo>(r);
                release.Link = serverService.ConvertToProxyLink(release.Link, serverUrl, CurrentIndexer.ID, "dl", release.Title + ".torrent");

                // Only accept torrent links, magnet is not supported
                // This seems to be no longer the case, allowing magnet URIs for now
                if (release.Link != null || release.MagnetUri != null)
                {
                    potatoResponse.results.Add(new TorrentPotatoResponseItem()
                    {
                        release_name = release.Title + "[" + CurrentIndexer.DisplayName + "]", // Suffix the indexer so we can see which tracker we are using in CPS as it just says torrentpotato >.>
                        torrent_id = release.Guid.ToString(),
                        details_url = release.Comments.ToString(),
                        download_url = (release.Link != null ? release.Link.ToString() : release.MagnetUri.ToString()),
                        imdb_id = release.Imdb.HasValue ? "tt" + release.Imdb : null,
                        freeleech = (release.DownloadVolumeFactor == 0 ? true : false),
                        type = "movie",
                        size = (long)release.Size / (1024 * 1024), // This is in MB
                        leechers = (int)release.Peers - (int)release.Seeders,
                        seeders = (int)release.Seeders,
                        publish_date = r.PublishDate == DateTime.MinValue ? null : release.PublishDate.ToUniversalTime().ToString("s")
                    });
                }
            }

            // Log info
            if (string.IsNullOrWhiteSpace(torznabQuery.SanitizedSearchTerm))
            {
                logger.Info(string.Format("Found {0} torrentpotato releases from {1}", releases.Count(), CurrentIndexer.DisplayName));
            }
            else
            {
                logger.Info(string.Format("Found {0} torrentpotato releases from {1} for: {2}", releases.Count(), CurrentIndexer.DisplayName, torznabQuery.GetQueryString()));
            }

            // Force the return as Json
            return new HttpResponseMessage()
            {
                Content = new JsonContent(potatoResponse)
            };
        }

        private Logger logger;
        private IServerService serverService;
        private ICacheService cacheService;
        private IWebClient webClient;
    }
}
