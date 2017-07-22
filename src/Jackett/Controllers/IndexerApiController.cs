using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using AutoMapper;
using Jackett.DTO;
using Jackett.Indexers;
using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Controllers
{
    public interface IIndexerController
    {
        IIndexerManagerService IndexerService { get; }
        IIndexer CurrentIndexer { get; set; }
    }

    public class RequiresIndexer : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            base.OnActionExecuting(actionContext);

            var controller = actionContext.ControllerContext.Controller;
            if (!(controller is IIndexerController))
                return;

            var indexerController = controller as IIndexerController;

            var parameters = actionContext.RequestContext.RouteData.Values;

            if (!parameters.ContainsKey("indexerId"))
            {
                indexerController.CurrentIndexer = null;
                return;
            }

            var indexerId = parameters["indexerId"] as string;
            if (indexerId.IsNullOrEmptyOrWhitespace())
                return;

            var indexerService = indexerController.IndexerService;
            var indexer = indexerService.GetIndexer(indexerId);
            indexerController.CurrentIndexer = indexer;
        }
    }

    [RoutePrefix("Api/Indexers")]
    [JackettAuthorized]
    [JackettAPINoCache]
    public class IndexerApiController : ApiController, IIndexerController
    {
        public IIndexerManagerService IndexerService { get; private set; }
        public IIndexer CurrentIndexer { get; set; }

        public IndexerApiController(IIndexerManagerService indexerManagerService, IServerService ss, ICacheService c, Logger logger)
        {
            IndexerService = indexerManagerService;
            serverService = ss;
            cacheService = c;
            this.logger = logger;
        }

        [HttpGet]
        [RequiresIndexer]
        public async Task<IHttpActionResult> Config()
        {
            var config = await CurrentIndexer.GetConfigurationForSetup();
            return Ok(config.ToJson(null));
        }

        [HttpPost]
        [ActionName("Config")]
        [RequiresIndexer]
        public async Task UpdateConfig([FromBody]ConfigItem[] config)
        {
            try
            {
                // HACK
                var jsonString = JsonConvert.SerializeObject(config);
                var json = JToken.Parse(jsonString);

                var configurationResult = await CurrentIndexer.ApplyConfiguration(json);

                if (configurationResult == IndexerConfigurationStatus.RequiresTesting)
                    await IndexerService.TestIndexer(CurrentIndexer.ID);
            }
            catch
            {
                var baseIndexer = CurrentIndexer as BaseIndexer;
                if (null != baseIndexer)
                    baseIndexer.ResetBaseConfig();
                throw;
            }
        }

        [HttpGet]
        public IEnumerable<DTO.Indexer> Indexers()
        {
            var dto = IndexerService.GetAllIndexers().Select(i => new DTO.Indexer(i));
            return dto;
        }

        [HttpPost]
        [RequiresIndexer]
        public async Task Test()
        {
            JToken jsonReply = new JObject();
            try
            {
                await IndexerService.TestIndexer(CurrentIndexer.ID);
                CurrentIndexer.LastError = null;
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                if (ex.InnerException != null)
                    msg += ": " + ex.InnerException.Message;

                if (CurrentIndexer != null)
                    CurrentIndexer.LastError = msg;

                throw;
            }
        }

        [HttpPost]
        [RequiresIndexer]
        public void Delete()
        {
            IndexerService.DeleteIndexer(CurrentIndexer.ID);
        }

        [HttpGet]
        [RequiresIndexer]
        public ManualSearchResult Results([FromUri]AdminSearch value)
        {
            //var results = new List<TrackerCacheResult>();
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
                    var item = Mapper.Map<TrackerCacheResult>(result);
                    item.Tracker = indexer.DisplayName;
                    item.TrackerId = indexer.ID;
                    item.Peers = item.Peers - item.Seeders; // Use peers as leechers

                    return item;
                });
            }).AsSequential().OrderByDescending(d => d.PublishDate).ToList();

            ConfigureCacheResults(results);

            var manualResult = new ManualSearchResult()
            {
                Results = results,
                Indexers = trackers.Select(t => t.DisplayName).ToList()
            };


            if (manualResult.Indexers.Count() == 0)
                manualResult.Indexers = new List<string>() { "None" };

            logger.Info(string.Format("Manual search for \"{0}\" on {1} with {2} results.", stringQuery.GetQueryString(), string.Join(", ", manualResult.Indexers), manualResult.Results.Count()));
            return manualResult;
        }

        // TODO
        // This should go to ServerConfigurationController
        [Route("Cache")]
        [HttpGet]
        public List<TrackerCacheResult> Cache()
        {
            var results = cacheService.GetCachedResults();
            ConfigureCacheResults(results);
            return results;
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
