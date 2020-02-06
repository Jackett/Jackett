using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jackett.Common.Indexers;
using Jackett.Common.Models;
using Jackett.Common.Models.DTO;
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
            if (indexerId.IsNullOrEmptyOrWhitespace())
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

    [Route("api/v2.0/indexers"), ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class IndexerApiController : Controller, IIndexerController
    {
        public IIndexerManagerService IndexerService { get; private set; }
        public IIndexer CurrentIndexer { get; set; }
        private readonly Logger _logger;
        private readonly IServerService _serverService;
        private readonly ICacheService _cacheService;

        public IndexerApiController(IIndexerManagerService indexerManagerService, IServerService ss, ICacheService c,
                                    Logger logger)
        {
            IndexerService = indexerManagerService;
            _serverService = ss;
            _cacheService = c;
            _logger = logger;
        }

        [HttpGet, TypeFilter(typeof(RequiresIndexer)), Route("{indexerId?}/Config")]
        public async Task<IActionResult> ConfigAsync()
        {
            var config = await CurrentIndexer.GetConfigurationForSetup();
            return Ok(config.ToJson(null));
        }

        [HttpPost, Route("{indexerId?}/Config"), TypeFilter(typeof(RequiresIndexer))]
        public async Task<IActionResult> UpdateConfigAsync([FromBody] ConfigItem[] config)
        {
            try
            {
                // HACK
                var jsonString = JsonConvert.SerializeObject(config);
                var json = JToken.Parse(jsonString);
                var configurationResult = await CurrentIndexer.ApplyConfiguration(json);
                if (configurationResult == IndexerConfigurationStatus.RequiresTesting)
                    await IndexerService.TestIndexer(CurrentIndexer.ID);
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

        [HttpGet, Route("")]
        public IEnumerable<Indexer> Indexers()
        {
            var dto = IndexerService.GetAllIndexers().Select(i => new Indexer(i));
            return dto;
        }

        [HttpPost, Route("{indexerid}/[action]"), TypeFilter(typeof(RequiresIndexer))]
        public async Task<IActionResult> TestAsync()
        {
            JToken jsonReply = new JObject();
            try
            {
                await IndexerService.TestIndexer(CurrentIndexer.ID);
                CurrentIndexer.LastError = null;
                return NoContent();
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                if (ex.InnerException != null)
                    msg += $": {ex.InnerException.Message}";
                if (CurrentIndexer != null)
                    CurrentIndexer.LastError = msg;
                throw;
            }
        }

        [HttpDelete, TypeFilter(typeof(RequiresIndexer)), Route("{indexerid}")]
        public void Delete() => IndexerService.DeleteIndexer(CurrentIndexer.ID);

        // TODO
        // This should go to ServerConfigurationController
        [Route("Cache"), HttpGet]
        public List<TrackerCacheResult> Cache()
        {
            var results = _cacheService.GetCachedResults();
            ConfigureCacheResults(results);
            return results;
        }

        private void ConfigureCacheResults(IEnumerable<TrackerCacheResult> results)
        {
            var serverUrl = _serverService.GetServerUrl(Request);
            foreach (var result in results)
            {
                var link = result.Link;
                var file = StringUtil.MakeValidFileName(result.Title, '_', false);
                result.Link = _serverService.ConvertToProxyLink(link, serverUrl, result.TrackerId, "dl", file);
                if (result.Link != null && result.Link.Scheme != "magnet" &&
                    !string.IsNullOrWhiteSpace(_serverService.GetBlackholeDirectory()))
                    result.BlackholeLink = _serverService.ConvertToProxyLink(link, serverUrl, result.TrackerId, "bh", file);
            }
        }
    }
}
