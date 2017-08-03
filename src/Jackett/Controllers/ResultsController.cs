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
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Xml.Linq;
using Jackett.Indexers;
using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
using NLog;

namespace Jackett.Controllers.V20
{
    public static class KeyValuePairsExtension
    {
        public static IDictionary<Key, Value> ToDictionary<Key, Value>(this IEnumerable<KeyValuePair<Key, Value>> pairs)
        {
            return pairs.ToDictionary(x => x.Key, x => x.Value);
        }
    }

    public class RequiresApiKeyAttribute : AuthorizationFilterAttribute
    {
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            var validApiKey = Engine.Server.Config.APIKey;
            var queryParams = actionContext.Request.GetQueryNameValuePairs().ToDictionary();
            var queryApiKey = queryParams.ContainsKey("apikey") ? queryParams["apikey"] : null;
            queryApiKey = queryParams.ContainsKey("passkey") ? queryParams["passkey"] : queryApiKey;

#if DEBUG
            if (Debugger.IsAttached)
                return;
#endif
            if (queryApiKey != validApiKey)
                actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized);
        }
    }

    public class RequiresConfiguredIndexerAttribute : AuthorizationFilterAttribute
    {
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            var controller = actionContext.ControllerContext.Controller;
            if (!(controller is IIndexerController))
                return;

            var indexerController = controller as IIndexerController;

            var parameters = actionContext.RequestContext.RouteData.Values;

            if (!parameters.ContainsKey("indexerId"))
            {
                indexerController.CurrentIndexer = null;
                actionContext.Response = actionContext.Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Invalid parameter");
                return;
            }

            var indexerId = parameters["indexerId"] as string;
            if (indexerId.IsNullOrEmptyOrWhitespace())
            {
                indexerController.CurrentIndexer = null;
                actionContext.Response = actionContext.Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Invalid parameter");
                return;
            }

            var indexerService = indexerController.IndexerService;
            var indexer = indexerService.GetIndexer(indexerId);

            if (!indexer.IsConfigured)
            {
                indexerController.CurrentIndexer = null;
                actionContext.Response = actionContext.Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Indexer is not configured");
                return;
            }

            indexerController.CurrentIndexer = indexer;
        }
    }

    [JackettAuthorized]
    [JackettAPINoCache]
    [RoutePrefix("api/v2.0/indexers")]
    [RequiresApiKey]
    [RequiresConfiguredIndexer]
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

            if (torznabQuery.ImdbID != null)
            {
                if (torznabQuery.QueryType != "movie")
                {
                    logger.Warn($"A non movie request with an imdbid was made from {Request.GetOwinContext().Request.RemoteIpAddress}.");
                    return GetErrorXML(201, "Incorrect parameter: only movie-search supports the imdbid parameter");
                }

                if (!string.IsNullOrEmpty(torznabQuery.SearchTerm))
                {
                    logger.Warn($"A movie-search request from {Request.GetOwinContext().Request.RemoteIpAddress} was made contining q and imdbid.");
                    return GetErrorXML(201, "Incorrect parameter: please specify either imdbid or q");
                }

                torznabQuery.ImdbID = ParseUtil.GetFullImdbID(torznabQuery.ImdbID); // normalize ImdbID
                if (torznabQuery.ImdbID == null)
                {
                    logger.Warn($"A movie-search request from {Request.GetOwinContext().Request.RemoteIpAddress} was made with an invalid imdbid.");
                    return GetErrorXML(201, "Incorrect parameter: invalid imdbid format");
                }

                if (!CurrentIndexer.TorznabCaps.SupportsImdbSearch)
                {
                    logger.Warn($"A movie-search request with imdbid from {Request.GetOwinContext().Request.RemoteIpAddress} was made but the indexer {CurrentIndexer.DisplayName} doesn't support it.");
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
                logBuilder.AppendFormat("Found {0} ({1} new) releases from {2}", releases.Count(), newItemCount, CurrentIndexer.DisplayName);
            }
            else
            {
                logBuilder.AppendFormat("Found {0} releases from {1}", releases.Count(), CurrentIndexer.DisplayName);
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

            var proxiedReleases = releases.Select(r => AutoMapper.Mapper.Map<ReleaseInfo>(r)).Select(r =>
            {
                r.Link = serverService.ConvertToProxyLink(r.Link, serverUrl, r.Origin.ID, "dl", r.Title + ".torrent");
                return r;
            });

            resultPage.Releases = proxiedReleases.ToList();

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
        public async Task<HttpResponseMessage> Potato([FromUri]TorrentPotatoRequest request)
        {
            if (!CurrentIndexer.TorznabCaps.Categories.Select(c => c.ID).Any(i => MOVIE_CATS.Contains(i)))
            {
                logger.Warn($"Rejected a request to {CurrentIndexer.DisplayName} which does not support searching for movies.");
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

            var releases = await CurrentIndexer.ResultsForQuery(torznabQuery);

            // Cache non query results
            if (string.IsNullOrEmpty(torznabQuery.SanitizedSearchTerm))
            {
                cacheService.CacheRssResults(CurrentIndexer, releases);
            }

            var serverUrl = string.Format("{0}://{1}:{2}{3}", Request.RequestUri.Scheme, Request.RequestUri.Host, Request.RequestUri.Port, serverService.BasePath());
            var potatoResponse = new TorrentPotatoResponse();
            var potatoReleases = releases.Where(r => r.Link != null || r.MagnetUri != null).Select(r =>
            {
                var release = AutoMapper.Mapper.Map<ReleaseInfo>(r);
                release.Link = serverService.ConvertToProxyLink(release.Link, serverUrl, CurrentIndexer.ID, "dl", release.Title + ".torrent");
                var item = new TorrentPotatoResponseItem()
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
                };
                return item;
            });

            potatoResponse.results = potatoReleases.ToList();

            // Log info
            if (string.IsNullOrWhiteSpace(torznabQuery.SanitizedSearchTerm))
            {
                logger.Info($"Found {releases.Count()} torrentpotato releases from {CurrentIndexer.DisplayName}");
            }
            else
            {
                logger.Info($"Found {releases.Count()} torrentpotato releases from {CurrentIndexer.DisplayName} for: {torznabQuery.GetQueryString()}");
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
