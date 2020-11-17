using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Jackett.Common;
using Jackett.Common.Indexers;
using Jackett.Common.Indexers.Meta;
using Jackett.Common.Models;
using Jackett.Common.Models.DTO;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using NLog;

namespace Jackett.Server.Controllers
{
    public class RequiresApiKey : IActionFilter
    {
        public IServerService serverService;

        public RequiresApiKey(IServerService ss) => serverService = ss;

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var validApiKey = serverService.GetApiKey();
            var queryParams = context.HttpContext.Request.Query;
            var queryApiKey = queryParams.Where(x => x.Key == "apikey" || x.Key == "passkey").Select(x => x.Value).FirstOrDefault();

#if DEBUG
            if (Debugger.IsAttached)
            {
                return;
            }
#endif
            if (queryApiKey != validApiKey)
            {
                context.Result = ResultsController.GetErrorActionResult(context.RouteData, HttpStatusCode.Unauthorized, 100, "Invalid API Key");
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // do something after the action executes
        }
    }

    public class RequiresConfiguredIndexer : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            var controller = context.Controller;
            if (!(controller is IIndexerController))
                return;

            var indexerController = controller as IIndexerController;

            var parameters = context.RouteData.Values;

            if (!parameters.ContainsKey("indexerId"))
            {
                indexerController.CurrentIndexer = null;
                context.Result = ResultsController.GetErrorActionResult(context.RouteData, HttpStatusCode.NotFound, 200, "Indexer is not specified");
                return;
            }

            var indexerId = parameters["indexerId"] as string;
            if (string.IsNullOrWhiteSpace(indexerId))
            {
                indexerController.CurrentIndexer = null;
                context.Result = ResultsController.GetErrorActionResult(context.RouteData, HttpStatusCode.NotFound, 201, "Indexer is not specified (empty value)");
                return;
            }

            var indexerService = indexerController.IndexerService;
            var indexer = indexerService.GetIndexer(indexerId);

            if (indexer == null)
            {
                indexerController.CurrentIndexer = null;
                context.Result = ResultsController.GetErrorActionResult(context.RouteData, HttpStatusCode.NotFound, 201, "Indexer is not supported");
                return;
            }

            if (!indexer.IsConfigured)
            {
                indexerController.CurrentIndexer = null;
                context.Result = ResultsController.GetErrorActionResult(context.RouteData, HttpStatusCode.NotFound, 201, "Indexer is not configured");
                return;
            }

            indexerController.CurrentIndexer = indexer;
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // do something after the action executes
        }
    }

    public class RequiresValidQuery : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            //TODO: Not sure what this is meant to do
            //if (context.HttpContext.Response != null)
            //    return;

            var controller = context.Controller;
            if (!(controller is IResultController))
            {
                return;
            }

            var resultController = controller as IResultController;

            var query = context.ActionArguments.First().Value;
            var queryType = query.GetType();
            var converter = queryType.GetMethod("ToTorznabQuery", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            if (converter == null)
            {
                context.Result = ResultsController.GetErrorActionResult(context.RouteData, HttpStatusCode.BadRequest, 900, "ToTorznabQuery() not found");
            }

            var converted = converter.Invoke(null, new object[] { query });
            var torznabQuery = converted as TorznabQuery;
            resultController.CurrentQuery = torznabQuery;

            if (queryType == typeof(ApiSearch)) // Skip CanHandleQuery() check for manual search (CurrentIndexer isn't used during manul search)
            {
                return;
            }

            if (!resultController.CurrentIndexer.CanHandleQuery(resultController.CurrentQuery))
            {
                context.Result = ResultsController.GetErrorActionResult(context.RouteData, HttpStatusCode.BadRequest, 201, 
                    $"{resultController.CurrentIndexer.Id} does not support the requested query. " +
                    "Please check the capabilities (t=caps) and make sure the search mode and parameters are supported.");

            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // do something after the action executes
        }
    }

    public interface IResultController : IIndexerController
    {
        TorznabQuery CurrentQuery { get; set; }
    }

    [AllowAnonymous]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    [Route("api/v2.0/indexers/{indexerId}/results")]
    [TypeFilter(typeof(RequiresApiKey))]
    [TypeFilter(typeof(RequiresConfiguredIndexer))]
    [TypeFilter(typeof(RequiresValidQuery))]
    public class ResultsController : Controller, IResultController
    {
        public IIndexerManagerService IndexerService { get; private set; }
        public IIndexer CurrentIndexer { get; set; }
        public TorznabQuery CurrentQuery { get; set; }
        private readonly Logger logger;
        private readonly IServerService serverService;
        private readonly ICacheService cacheService;
        private readonly Common.Models.Config.ServerConfig serverConfig;

        public ResultsController(IIndexerManagerService indexerManagerService, IServerService ss, ICacheService c, Logger logger, Common.Models.Config.ServerConfig sConfig)
        {
            IndexerService = indexerManagerService;
            serverService = ss;
            cacheService = c;
            this.logger = logger;
            serverConfig = sConfig;
        }

        [Route("")]
        [HttpGet]
        public async Task<IActionResult> Results([FromQuery] ApiSearch requestt)
        {
            //TODO: Better way to parse querystring

            var request = new ApiSearch();

            foreach (var t in Request.Query)
            {
                if (t.Key == "Tracker[]")
                {
                    request.Tracker = t.Value.ToString().Split(',');
                }

                if (t.Key == "Category[]")
                {
                    request.Category = t.Value.ToString().Split(',').Select(int.Parse).ToArray();
                    CurrentQuery.Categories = request.Category;
                }

                if (t.Key == "query")
                {
                    request.Query = t.Value.ToString();
                }
            }

            var manualResult = new ManualSearchResult();
            var trackers = IndexerService.GetAllIndexers().ToList().Where(t => t.IsConfigured);
            if (request.Tracker != null)
                trackers = trackers.Where(t => request.Tracker.Contains(t.Id));
            trackers = trackers.Where(t => t.CanHandleQuery(CurrentQuery));

            var isMetaIndexer = request.Tracker == null || request.Tracker.Length > 1;
            var tasks = trackers.ToList().Select(t => t.ResultsForQuery(CurrentQuery, isMetaIndexer)).ToList();
            try
            {
                var aggregateTask = Task.WhenAll(tasks);
                await aggregateTask;
            }
            catch (AggregateException e)
            {
                foreach (var ex in e.InnerExceptions)
                {
                    logger.Error(ex);
                }
            }
            catch (Exception e)
            {
                logger.Error(e);
            }

            manualResult.Indexers = tasks.Select(t =>
            {
                var resultIndexer = new ManualSearchResultIndexer();
                IIndexer indexer = null;
                if (t.Status == TaskStatus.RanToCompletion)
                {
                    resultIndexer.Status = ManualSearchResultIndexerStatus.OK;
                    resultIndexer.Results = t.Result.Releases.Count();
                    resultIndexer.Error = null;
                    indexer = t.Result.Indexer;
                }
                else if (t.Exception.InnerException is IndexerException)
                {
                    resultIndexer.Status = ManualSearchResultIndexerStatus.Error;
                    resultIndexer.Results = 0;
                    resultIndexer.Error = ((IndexerException)t.Exception.InnerException).ToString();
                    indexer = ((IndexerException)t.Exception.InnerException).Indexer;
                }
                else
                {
                    resultIndexer.Status = ManualSearchResultIndexerStatus.Unknown;
                    resultIndexer.Results = 0;
                    resultIndexer.Error = null;
                }

                if (indexer != null)
                {
                    resultIndexer.ID = indexer.Id;
                    resultIndexer.Name = indexer.DisplayName;
                }
                return resultIndexer;
            }).ToList();

            manualResult.Results = tasks.Where(t => t.Status == TaskStatus.RanToCompletion).Where(t => t.Result.Releases.Any()).SelectMany(t =>
            {
                var searchResults = t.Result.Releases;
                var indexer = t.Result.Indexer;
                cacheService.CacheRssResults(indexer, searchResults);

                return searchResults.Select(result =>
                {
                    var item = AutoMapper.Mapper.Map<TrackerCacheResult>(result);
                    item.Tracker = indexer.DisplayName;
                    item.TrackerId = indexer.Id;
                    item.Peers = item.Peers - item.Seeders; // Use peers as leechers

                    return item;
                });
            }).OrderByDescending(d => d.PublishDate).ToList();

            ConfigureCacheResults(manualResult.Results);

            logger.Info(string.Format("Manual search for \"{0}\" on {1} with {2} results.", CurrentQuery.SanitizedSearchTerm, string.Join(", ", manualResult.Indexers.Select(i => i.ID)), manualResult.Results.Count()));
            return Json(manualResult);
        }

        [Route("[action]/{ignored?}")]
        [HttpGet]
        public async Task<IActionResult> Torznab([FromQuery]TorznabRequest request)
        {
            if (string.Equals(CurrentQuery.QueryType, "caps", StringComparison.InvariantCultureIgnoreCase))
            {
                return Content(CurrentIndexer.TorznabCaps.ToXml(), "application/rss+xml", Encoding.UTF8);
            }

            // indexers - returns a list of all included indexers (meta indexers only)
            if (string.Equals(CurrentQuery.QueryType, "indexers", StringComparison.InvariantCultureIgnoreCase))
            {
                if (!(CurrentIndexer is BaseMetaIndexer)) // shouldn't be needed because CanHandleQuery should return false
                {
                    logger.Warn($"A search request with t=indexers from {Request.HttpContext.Connection.RemoteIpAddress} was made but the indexer {CurrentIndexer.DisplayName} isn't a meta indexer.");
                    return GetErrorXML(203, "Function Not Available: this isn't a meta indexer");
                }
                var CurrentBaseMetaIndexer = (BaseMetaIndexer)CurrentIndexer;
                var indexers = CurrentBaseMetaIndexer.Indexers;
                if (string.Equals(request.configured, "true", StringComparison.InvariantCultureIgnoreCase))
                    indexers = indexers.Where(i => i.IsConfigured);
                else if (string.Equals(request.configured, "false", StringComparison.InvariantCultureIgnoreCase))
                    indexers = indexers.Where(i => !i.IsConfigured);

                var xdoc = new XDocument(
                    new XDeclaration("1.0", "UTF-8", null),
                    new XElement("indexers",
                        from i in indexers
                        select new XElement("indexer",
                            new XAttribute("id", i.Id),
                            new XAttribute("configured", i.IsConfigured),
                            new XElement("title", i.DisplayName),
                            new XElement("description", i.DisplayDescription),
                            new XElement("link", i.SiteLink),
                            new XElement("language", i.Language),
                            new XElement("type", i.Type),
                            i.TorznabCaps.GetXDocument().FirstNode
                        )
                    )
                );

                return Content(xdoc.Declaration.ToString() + Environment.NewLine + xdoc.ToString(), "application/xml", Encoding.UTF8);
            }

            if (CurrentQuery.ImdbID != null)
            {
                /* We should allow this (helpful in case of aggregate indexers)
                if (!string.IsNullOrEmpty(CurrentQuery.SearchTerm))
                {
                    logger.Warn($"A search request from {Request.HttpContext.Connection.RemoteIpAddress} was made containing q and imdbid.");
                    return GetErrorXML(201, "Incorrect parameter: please specify either imdbid or q");
                }
                */

                CurrentQuery.ImdbID = ParseUtil.GetFullImdbID(CurrentQuery.ImdbID); // normalize ImdbID
                if (CurrentQuery.ImdbID == null)
                {
                    logger.Warn($"A search request from {Request.HttpContext.Connection.RemoteIpAddress} was made with an invalid imdbid.");
                    return GetErrorXML(201, "Incorrect parameter: invalid imdbid format");
                }

                if (CurrentQuery.IsMovieSearch && !CurrentIndexer.TorznabCaps.MovieSearchImdbAvailable)
                {
                    logger.Warn($"A search request with imdbid from {Request.HttpContext.Connection.RemoteIpAddress} was made but the indexer {CurrentIndexer.DisplayName} doesn't support it.");
                    return GetErrorXML(203, "Function Not Available: imdbid is not supported for movie search by this indexer");
                }

                if (CurrentQuery.IsTVSearch && !CurrentIndexer.TorznabCaps.TvSearchImdbAvailable)
                {
                    logger.Warn($"A search request with imdbid from {Request.HttpContext.Connection.RemoteIpAddress} was made but the indexer {CurrentIndexer.DisplayName} doesn't support it.");
                    return GetErrorXML(203, "Function Not Available: imdbid is not supported for TV search by this indexer");
                }
            }

            if (CurrentQuery.TmdbID != null)
            {
                if (CurrentQuery.IsMovieSearch && !CurrentIndexer.TorznabCaps.MovieSearchTmdbAvailable)
                {
                    logger.Warn($"A search request with tmdbid from {Request.HttpContext.Connection.RemoteIpAddress} was made but the indexer {CurrentIndexer.DisplayName} doesn't support it.");
                    return GetErrorXML(203, "Function Not Available: tmdbid is not supported for movie search by this indexer");
                }
            }

            if (CurrentQuery.TvdbID != null)
            {
                if (CurrentQuery.IsTVSearch && !CurrentIndexer.TorznabCaps.TvSearchAvailable)
                {
                    logger.Warn($"A search request with tvdbid from {Request.HttpContext.Connection.RemoteIpAddress} was made but the indexer {CurrentIndexer.DisplayName} doesn't support it.");
                    return GetErrorXML(203, "Function Not Available: tvdbid is not supported for movie search by this indexer");
                }
            }

            try
            {
                var result = await CurrentIndexer.ResultsForQuery(CurrentQuery);

                // Some trackers do not support multiple category filtering so filter the releases that match manually.
                int? newItemCount = null;

                // Cache non query results
                if (string.IsNullOrEmpty(CurrentQuery.SanitizedSearchTerm))
                {
                    newItemCount = cacheService.GetNewItemCount(CurrentIndexer, result.Releases);
                    cacheService.CacheRssResults(CurrentIndexer, result.Releases);
                }

                // Log info
                var logBuilder = new StringBuilder();
                if (newItemCount != null)
                {
                    logBuilder.AppendFormat("Found {0} ({1} new) releases from {2}", result.Releases.Count(), newItemCount, CurrentIndexer.DisplayName);
                }
                else
                {
                    logBuilder.AppendFormat("Found {0} releases from {1}", result.Releases.Count(), CurrentIndexer.DisplayName);
                }

                if (!string.IsNullOrWhiteSpace(CurrentQuery.SanitizedSearchTerm))
                {
                    logBuilder.AppendFormat(" for: {0}", CurrentQuery.GetQueryString());
                }

                logger.Info(logBuilder.ToString());

                var serverUrl = serverService.GetServerUrl(Request);
                var resultPage = new ResultPage(new ChannelInfo
                {
                    Title = CurrentIndexer.DisplayName,
                    Description = CurrentIndexer.DisplayDescription,
                    Link = new Uri(CurrentIndexer.SiteLink),
                    ImageUrl = new Uri(serverUrl + "logos/" + CurrentIndexer.Id + ".png"),
                    ImageTitle = CurrentIndexer.DisplayName,
                    ImageLink = new Uri(CurrentIndexer.SiteLink),
                    ImageDescription = CurrentIndexer.DisplayName
                });

                var proxiedReleases = result.Releases.Select(r => AutoMapper.Mapper.Map<ReleaseInfo>(r)).Select(r =>
                {
                    r.Link = serverService.ConvertToProxyLink(r.Link, serverUrl, r.Origin.Id, "dl", r.Title);
                    return r;
                });

                resultPage.Releases = proxiedReleases.ToList();

                var xml = resultPage.ToXml(new Uri(serverUrl));
                // Force the return as XML

                return Content(xml, "application/rss+xml", Encoding.UTF8);
            }
            catch (Exception e)
            {
                logger.Error(e);
                return GetErrorXML(900, e.ToString());
            }
        }

        [Route("[action]/{ignored?}")]
        public IActionResult GetErrorXML(int code, string description) => Content(CreateErrorXML(code, description), "application/xml", Encoding.UTF8);

        public static string CreateErrorXML(int code, string description)
        {
            var xdoc = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement("error",
                    new XAttribute("code", code.ToString()),
                    new XAttribute("description", description)
                )
            );
            return xdoc.Declaration + Environment.NewLine + xdoc;
        }

        public static IActionResult GetErrorActionResult(RouteData routeData, HttpStatusCode status, int torznabCode, string description)
        {
            var isTorznab = routeData.Values["action"].ToString().Equals("torznab", StringComparison.OrdinalIgnoreCase);

            if (isTorznab)
            {
                var contentResult = new ContentResult
                {
                    Content = CreateErrorXML(torznabCode, description),
                    ContentType = "application/xml",
                    StatusCode = 200
                };
                return contentResult;
            }
            else
            {
                switch (status)
                {
                    case HttpStatusCode.Unauthorized:
                        return new UnauthorizedResult();
                    case HttpStatusCode.NotFound:
                        return new NotFoundObjectResult(description);
                    case HttpStatusCode.BadRequest:
                        return new BadRequestObjectResult(description);
                    default:
                        return new ContentResult
                        {
                            Content = description,
                            StatusCode = (int)status
                        };
                }
            }
        }

        [Route("[action]/{ignored?}")]
        [HttpGet]
        public async Task<TorrentPotatoResponse> Potato([FromQuery]TorrentPotatoRequest request)
        {
            var result = await CurrentIndexer.ResultsForQuery(CurrentQuery);

            // Cache non query results
            if (string.IsNullOrEmpty(CurrentQuery.SanitizedSearchTerm))
                cacheService.CacheRssResults(CurrentIndexer, result.Releases);

            // Log info
            if (string.IsNullOrWhiteSpace(CurrentQuery.SanitizedSearchTerm))
                logger.Info($"Found {result.Releases.Count()} torrentpotato releases from {CurrentIndexer.DisplayName}");
            else
                logger.Info($"Found {result.Releases.Count()} torrentpotato releases from {CurrentIndexer.DisplayName} for: {CurrentQuery.GetQueryString()}");

            var serverUrl = serverService.GetServerUrl(Request);
            var potatoReleases = result.Releases.Where(r => r.Link != null || r.MagnetUri != null).Select(r =>
            {
                var release = AutoMapper.Mapper.Map<ReleaseInfo>(r);
                release.Link = serverService.ConvertToProxyLink(release.Link, serverUrl, CurrentIndexer.Id, "dl", release.Title);
                // IMPORTANT: We can't use Uri.ToString(), because it generates URLs without URL encode (links with unicode
                // characters are broken). We must use Uri.AbsoluteUri instead that handles encoding correctly
                var item = new TorrentPotatoResponseItem()
                {
                    release_name = release.Title + "[" + CurrentIndexer.DisplayName + "]", // Suffix the indexer so we can see which tracker we are using in CPS as it just says torrentpotato >.>
                    torrent_id = release.Guid.AbsoluteUri, // GUID and (Link or Magnet) are mandatory
                    details_url = release.Details?.AbsoluteUri,
                    download_url = (release.Link != null ? release.Link.AbsoluteUri : release.MagnetUri.AbsoluteUri),
                    imdb_id = release.Imdb.HasValue ? ParseUtil.GetFullImdbID("tt" + release.Imdb) : null,
                    freeleech = (release.DownloadVolumeFactor == 0 ? true : false),
                    type = "movie",
                    size = (long)release.Size / (1024 * 1024), // This is in MB
                    leechers = (release.Peers ?? -1) - (release.Seeders ?? 0),
                    seeders = release.Seeders ?? -1,
                    publish_date = r.PublishDate == DateTime.MinValue ? null : release.PublishDate.ToUniversalTime().ToString("s")
                };
                return item;
            });

            var potatoResponse = new TorrentPotatoResponse()
            {
                results = potatoReleases.ToList()
            };

            return potatoResponse;
        }

        [Route("[action]/{ignored?}")]
        private void ConfigureCacheResults(IEnumerable<TrackerCacheResult> results)
        {
            var serverUrl = serverService.GetServerUrl(Request);
            foreach (var result in results)
            {
                var link = result.Link;
                var file = StringUtil.MakeValidFileName(result.Title, '_', false);
                result.Link = serverService.ConvertToProxyLink(link, serverUrl, result.TrackerId, "dl", file);
                if (!string.IsNullOrWhiteSpace(serverConfig.BlackholeDir))
                {
                    if (result.Link != null)
                        result.BlackholeLink = serverService.ConvertToProxyLink(link, serverUrl, result.TrackerId, "bh", file);
                    else if (result.MagnetUri != null)
                        result.BlackholeLink = serverService.ConvertToProxyLink(result.MagnetUri, serverUrl, result.TrackerId, "bh", file);
                }
            }
        }

    }
}
