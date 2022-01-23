using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jackett.Common.Indexers;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Server.Controllers
{
    public interface IIndexerController
    {
        IIndexerManagerService IndexerService { get; }
        IIndexer CurrentIndexer { get; set; }
    }

    public class RequiresIndexer : IActionFilter
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
                return;
            }

            var indexerId = parameters["indexerId"] as string;
            if (string.IsNullOrWhiteSpace(indexerId))
                return;

            var indexerService = indexerController.IndexerService;
            var indexer = indexerService.GetIndexer(indexerId);
            indexerController.CurrentIndexer = indexer;
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // do something after the action executes
        }
    }

    [Route("api/v2.0/indexers")]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class IndexerApiController : Controller, IIndexerController
    {
        public IIndexerManagerService IndexerService { get; private set; }
        public IIndexer CurrentIndexer { get; set; }
        private readonly Logger logger;
        private readonly IServerService serverService;
        private readonly ICacheService cacheService;

        public IndexerApiController(IIndexerManagerService indexerManagerService, IServerService ss, ICacheService c, Logger logger)
        {
            IndexerService = indexerManagerService;
            serverService = ss;
            cacheService = c;
            this.logger = logger;
        }

        [HttpGet]
        [TypeFilter(typeof(RequiresIndexer))]
        [Route("{indexerId?}/Config")]
        public async Task<IActionResult> Config()
        {
            var config = await CurrentIndexer.GetConfigurationForSetup();
            return Ok(config.ToJson(null));
        }

        [HttpPost]
        [Route("{indexerId?}/Config")]
        [TypeFilter(typeof(RequiresIndexer))]
        public async Task<IActionResult> UpdateConfig([FromBody] Common.Models.DTO.ConfigItem[] config)
        {
            // invalidate cache for this indexer
            cacheService.CleanIndexerCache(CurrentIndexer);

            try
            {
                // HACK
                var jsonString = JsonConvert.SerializeObject(config);
                var json = JToken.Parse(jsonString);

                var configurationResult = await CurrentIndexer.ApplyConfiguration(json);

                if (configurationResult == IndexerConfigurationStatus.RequiresTesting)
                {
                    await IndexerService.TestIndexer(CurrentIndexer.Id);
                }

                return new NoContentResult();
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
        [Route("")]
        public IEnumerable<Common.Models.DTO.Indexer> Indexers([FromQuery(Name = "configured")] bool configured)
        {
            var dto = IndexerService.GetAllIndexers().Select(i => new Common.Models.DTO.Indexer(i));
            dto = configured ? dto.Where(i => i.configured) : dto;
            return dto;
        }

        [HttpPost]
        [Route("{indexerid}/[action]")]
        [TypeFilter(typeof(RequiresIndexer))]
        public async Task<IActionResult> Test()
        {
            JToken jsonReply = new JObject();
            try
            {
                await IndexerService.TestIndexer(CurrentIndexer.Id);
                CurrentIndexer.LastError = null;
                return NoContent();
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

        [HttpDelete]
        [TypeFilter(typeof(RequiresIndexer))]
        [Route("{indexerid}")]
        public void Delete() => IndexerService.DeleteIndexer(CurrentIndexer.Id);

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
            var serverUrl = serverService.GetServerUrl(Request);
            foreach (var result in results)
            {
                var link = result.Link;
                var file = StringUtil.MakeValidFileName(result.Title, '_', false);
                result.Link = serverService.ConvertToProxyLink(link, serverUrl, result.TrackerId, "dl", file);
                result.Poster = serverService.ConvertToProxyLink(result.Poster, serverUrl, result.TrackerId, "img", "poster");
                if (result.Link != null && result.Link.Scheme != "magnet" && !string.IsNullOrWhiteSpace(serverService.GetBlackholeDirectory()))
                    result.BlackholeLink = serverService.ConvertToProxyLink(link, serverUrl, result.TrackerId, "bh", file);
            }
        }

    }
}
