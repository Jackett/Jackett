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
using Newtonsoft.Json;
using NLog;

namespace Jackett.Controllers.V20
{
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

    public class RequiresConfiguredIndexerAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
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

            if (indexer == null)
            {
                indexerController.CurrentIndexer = null;
                actionContext.Response = actionContext.Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Invalid parameter");
                return;
            }

            if (!indexer.IsConfigured)
            {
                indexerController.CurrentIndexer = null;
                actionContext.Response = actionContext.Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Indexer is not configured");
                return;
            }

            indexerController.CurrentIndexer = indexer;
        }
    }

    public class RequiresValidQueryAttribute : RequiresConfiguredIndexerAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            base.OnActionExecuting(actionContext);
            if (actionContext.Response != null)
                return;

            var controller = actionContext.ControllerContext.Controller;
            if (!(controller is IResultController))
                return;

            var resultController = controller as IResultController;

            var query = actionContext.ActionArguments.First().Value;
            var queryType = query.GetType();
            var converter = queryType.GetMethod("ToTorznabQuery", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            if (converter == null)
                actionContext.Response = actionContext.Request.CreateErrorResponse(HttpStatusCode.BadRequest, "");
            var converted = converter.Invoke(null, new object[] { query });
            var torznabQuery = converted as TorznabQuery;
            resultController.CurrentQuery = torznabQuery;

            if (!resultController.CurrentIndexer.CanHandleQuery(resultController.CurrentQuery))
                actionContext.Response = actionContext.Request.CreateErrorResponse(HttpStatusCode.BadRequest, $"{resultController.CurrentIndexer.ID} does not support the requested query.");
        }
    }

    public class JsonResponseAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            base.OnActionExecuted(actionExecutedContext);

            var content = actionExecutedContext.Response.Content as ObjectContent;
            actionExecutedContext.Response.Content = new JsonContent(content.Value);
        }
    }

    public interface IResultController : IIndexerController
    {
        TorznabQuery CurrentQuery { get; set; }
    }

    [AllowAnonymous]
    [JackettAPINoCache]
    [RoutePrefix("api/v2.0/indexers")]
    [RequiresApiKey]
    [RequiresValidQuery]
    public class ResultsController : ApiController, IResultController
    {
        public IIndexerManagerService IndexerService { get; private set; }
        public IIndexer CurrentIndexer { get; set; }
        public TorznabQuery CurrentQuery { get; set; }

        public ResultsController(IIndexerManagerService indexerManagerService, IServerService ss, ICacheService c, Logger logger)
        {
            IndexerService = indexerManagerService;
            serverService = ss;
            cacheService = c;
            this.logger = logger;
        }

        [HttpGet]
        public async Task<Models.DTO.ManualSearchResult> Results([FromUri]Models.DTO.ApiSearch request)
        {
            var trackers = IndexerService.GetAllIndexers().Where(t => t.IsConfigured);
            if (CurrentIndexer.ID != "all")
                trackers = trackers.Where(t => t.ID == CurrentIndexer.ID).ToList();
            trackers = trackers.Where(t => t.IsConfigured && t.CanHandleQuery(CurrentQuery));

            var tasks = trackers.ToList().Select(t => t.ResultsForQuery(CurrentQuery)).ToList();
            var aggregateTask = Task.WhenAll(tasks);

            await aggregateTask;

            var results = tasks.Where(t => t.Status == TaskStatus.RanToCompletion).Where(t => t.Result.Count() > 0).SelectMany(t =>
            {
                var searchResults = t.Result;
                var indexer = searchResults.First().Origin;
                cacheService.CacheRssResults(indexer, searchResults);

                return searchResults.Select(result =>
                {
                    var item = AutoMapper.Mapper.Map<TrackerCacheResult>(result);
                    item.Tracker = indexer.DisplayName;
                    item.TrackerId = indexer.ID;
                    item.Peers = item.Peers - item.Seeders; // Use peers as leechers

                    return item;
                });
            }).OrderByDescending(d => d.PublishDate).ToList();

            ConfigureCacheResults(results);

            var manualResult = new Models.DTO.ManualSearchResult()
            {
                Results = results,
                Indexers = trackers.Select(t => t.DisplayName).ToList()
            };


            if (manualResult.Indexers.Count() == 0)
                manualResult.Indexers = new List<string>() { "None" };

            logger.Info(string.Format("Manual search for \"{0}\" on {1} with {2} results.", CurrentQuery.SanitizedSearchTerm, string.Join(", ", manualResult.Indexers), manualResult.Results.Count()));
            return manualResult;
        }

        [HttpGet]
        public async Task<IHttpActionResult> Torznab([FromUri]Models.DTO.TorznabRequest request)
        {
            if (string.Equals(CurrentQuery.QueryType, "caps", StringComparison.InvariantCultureIgnoreCase))
            {
                return ResponseMessage(new HttpResponseMessage()
                {
                    Content = new StringContent(CurrentIndexer.TorznabCaps.ToXml(), Encoding.UTF8, "application/xml")
                });
            }

            if (CurrentQuery.ImdbID != null)
            {
                if (CurrentQuery.QueryType != "movie")
                {
                    logger.Warn($"A non movie request with an imdbid was made from {Request.GetOwinContext().Request.RemoteIpAddress}.");
                    return GetErrorXML(201, "Incorrect parameter: only movie-search supports the imdbid parameter");
                }

                if (!string.IsNullOrEmpty(CurrentQuery.SearchTerm))
                {
                    logger.Warn($"A movie-search request from {Request.GetOwinContext().Request.RemoteIpAddress} was made contining q and imdbid.");
                    return GetErrorXML(201, "Incorrect parameter: please specify either imdbid or q");
                }

                CurrentQuery.ImdbID = ParseUtil.GetFullImdbID(CurrentQuery.ImdbID); // normalize ImdbID
                if (CurrentQuery.ImdbID == null)
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

            var releases = await CurrentIndexer.ResultsForQuery(CurrentQuery);

            // Some trackers do not support multiple category filtering so filter the releases that match manually.
            int? newItemCount = null;

            // Cache non query results
            if (string.IsNullOrEmpty(CurrentQuery.SanitizedSearchTerm))
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

            if (!string.IsNullOrWhiteSpace(CurrentQuery.SanitizedSearchTerm))
            {
                logBuilder.AppendFormat(" for: {0}", CurrentQuery.GetQueryString());
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
            return ResponseMessage(new HttpResponseMessage()
            {
                Content = new StringContent(xml, Encoding.UTF8, "application/rss+xml")
            });
        }

        public IHttpActionResult GetErrorXML(int code, string description)
        {
            var xdoc = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement("error",
                    new XAttribute("code", code.ToString()),
                    new XAttribute("description", description)
                )
            );

            var xml = xdoc.Declaration.ToString() + Environment.NewLine + xdoc.ToString();

            return ResponseMessage(new HttpResponseMessage()
            {
                Content = new StringContent(xml, Encoding.UTF8, "application/xml")
            });
        }

        [HttpGet]
        [JsonResponse]
        public async Task<Models.DTO.TorrentPotatoResponse> Potato([FromUri]Models.DTO.TorrentPotatoRequest request)
        {
            var releases = await CurrentIndexer.ResultsForQuery(CurrentQuery);

            // Cache non query results
            if (string.IsNullOrEmpty(CurrentQuery.SanitizedSearchTerm))
                cacheService.CacheRssResults(CurrentIndexer, releases);

            // Log info
            if (string.IsNullOrWhiteSpace(CurrentQuery.SanitizedSearchTerm))
                logger.Info($"Found {releases.Count()} torrentpotato releases from {CurrentIndexer.DisplayName}");
            else
                logger.Info($"Found {releases.Count()} torrentpotato releases from {CurrentIndexer.DisplayName} for: {CurrentQuery.GetQueryString()}");

            var serverUrl = string.Format("{0}://{1}:{2}{3}", Request.RequestUri.Scheme, Request.RequestUri.Host, Request.RequestUri.Port, serverService.BasePath());
            var potatoReleases = releases.Where(r => r.Link != null || r.MagnetUri != null).Select(r =>
            {
                var release = AutoMapper.Mapper.Map<ReleaseInfo>(r);
                release.Link = serverService.ConvertToProxyLink(release.Link, serverUrl, CurrentIndexer.ID, "dl", release.Title + ".torrent");
                var item = new Models.DTO.TorrentPotatoResponseItem()
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

            var potatoResponse = new Models.DTO.TorrentPotatoResponse()
            {
                results = potatoReleases.ToList()
            };

            return potatoResponse;
        }

        private void ConfigureCacheResults(IEnumerable<TrackerCacheResult> results)
        {
            var serverUrl = string.Format("{0}://{1}:{2}{3}", Request.RequestUri.Scheme, Request.RequestUri.Host, Request.RequestUri.Port, serverService.BasePath());
            foreach (var result in results)
            {
                var link = result.Link;
                var file = StringUtil.MakeValidFileName(result.Title, '_', false) + ".torrent";
                result.Link = serverService.ConvertToProxyLink(link, serverUrl, result.TrackerId, "dl", file);
                if (result.Link != null && result.Link.Scheme != "magnet" && !string.IsNullOrWhiteSpace(Engine.Server.Config.BlackholeDir))
                    result.BlackholeLink = serverService.ConvertToProxyLink(link, serverUrl, result.TrackerId, "bh", file);

            }
        }

        private Logger logger;
        private IServerService serverService;
        private ICacheService cacheService;
    }
}
