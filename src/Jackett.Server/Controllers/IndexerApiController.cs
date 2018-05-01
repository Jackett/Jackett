using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using Jackett.Common;
using Jackett.Common.Indexers;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
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

    public class RequiresIndexerAttribute : ActionFilterAttribute
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

    [RoutePrefix("api/v2.0/indexers")]
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
        public async Task UpdateConfig([FromBody]Common.Models.DTO.ConfigItem[] config)
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
        [Route("")]
        public IEnumerable<Common.Models.DTO.Indexer> Indexers()
        {
            var dto = IndexerService.GetAllIndexers().Select(i => new Common.Models.DTO.Indexer(i));
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

        [HttpDelete]
        [RequiresIndexer]
        [Route("{indexerId}")]
        public void Delete()
        {
            IndexerService.DeleteIndexer(CurrentIndexer.ID);
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
            var serverUrl = serverService.GetServerUrl(Request);
            foreach (var result in results)
            {
                var link = result.Link;
                var file = StringUtil.MakeValidFileName(result.Title, '_', false);
                result.Link = serverService.ConvertToProxyLink(link, serverUrl, result.TrackerId, "dl", file);
                if (result.Link != null && result.Link.Scheme != "magnet" && !string.IsNullOrWhiteSpace(Engine.ServerConfig.BlackholeDir))
                    result.BlackholeLink = serverService.ConvertToProxyLink(link, serverUrl, result.TrackerId, "bh", file);

            }
        }

        private Logger logger;
        private IServerService serverService;
        private ICacheService cacheService;
    }
}
