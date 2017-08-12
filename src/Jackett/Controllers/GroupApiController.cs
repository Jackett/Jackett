using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using AutoMapper;
using Jackett.Indexers;
using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Controllers.V20
{
    [RoutePrefix("api/v2.0/groups")]
    [JackettAuthorized]
    [JackettAPINoCache]
    public class GroupApiController : ApiController, IIndexerController
    {
        public IIndexerManagerService IndexerService { get; private set; }
        public IIndexer CurrentIndexer { get; set; }

        public GroupApiController(IIndexerManagerService indexerManagerService, IServerService ss, Logger logger)
        {
            IndexerService = indexerManagerService;
            serverService = ss;
            this.logger = logger;
        }

        //[HttpGet]
        //[RequiresIndexer]
        //public async Task<IHttpActionResult> Config()
        //{
        //    var config = await CurrentIndexer.GetConfigurationForSetup();
        //    return Ok(config.ToJson(null));
        //}

        //[HttpPost]
        //[ActionName("Config")]
        //[RequiresIndexer]
        //public async Task UpdateConfig([FromBody]Models.DTO.ConfigItem[] config)
        //{
        //    try
        //    {
        //        // HACK
        //        var jsonString = JsonConvert.SerializeObject(config);
        //        var json = JToken.Parse(jsonString);

        //        var configurationResult = await CurrentIndexer.ApplyConfiguration(json);

        //        if (configurationResult == IndexerConfigurationStatus.RequiresTesting)
        //            await IndexerService.TestIndexer(CurrentIndexer.ID);
        //    }
        //    catch
        //    {
        //        var baseIndexer = CurrentIndexer as BaseIndexer;
        //        if (null != baseIndexer)
        //            baseIndexer.ResetBaseConfig();
        //        throw;
        //    }
        //}

        [HttpGet]
        [Route("")]
        public IEnumerable<Models.DTO.IndexerGroup> Groups()
        {
            var dto = IndexerService.Groups.Select(i => new Models.DTO.IndexerGroup(i));
            return dto;
        }

        //[HttpPost]
        //[RequiresIndexer]
        //public async Task Test()
        //{
        //    JToken jsonReply = new JObject();
        //    try
        //    {
        //        await IndexerService.TestIndexer(CurrentIndexer.ID);
        //        CurrentIndexer.LastError = null;
        //    }
        //    catch (Exception ex)
        //    {
        //        var msg = ex.Message;
        //        if (ex.InnerException != null)
        //            msg += ": " + ex.InnerException.Message;

        //        if (CurrentIndexer != null)
        //            CurrentIndexer.LastError = msg;

        //        throw;
        //    }
        //}

        [HttpDelete]
        [Route("{groupId}")]
        public void Delete(string groupId)
        {
            IndexerService.DeleteGroup(groupId);
        }

        [HttpPost]
        [Route("{groupId}")]
        public void Create(string groupId, [FromBody]IEnumerable<string> indexerIds)
        {
            IndexerService.CreateGroup(groupId, indexerIds);
        }

        //// TODO
        //// This should go to ServerConfigurationController
        //[Route("Cache")]
        //[HttpGet]
        //public List<TrackerCacheResult> Cache()
        //{
        //    var results = cacheService.GetCachedResults();
        //    ConfigureCacheResults(results);
        //    return results;
        //}

        //private void ConfigureCacheResults(IEnumerable<TrackerCacheResult> results)
        //{
        //    var serverUrl = string.Format("{0}://{1}:{2}{3}", Request.RequestUri.Scheme, Request.RequestUri.Host, Request.RequestUri.Port, serverService.BasePath());
        //    foreach (var result in results)
        //    {
        //        var link = result.Link;
        //        var file = StringUtil.MakeValidFileName(result.Title, '_', false) + ".torrent";
        //        result.Link = serverService.ConvertToProxyLink(link, serverUrl, result.TrackerId, "dl", file);
        //        if (result.Link != null && result.Link.Scheme != "magnet" && !string.IsNullOrWhiteSpace(Engine.Server.Config.BlackholeDir))
        //            result.BlackholeLink = serverService.ConvertToProxyLink(link, serverUrl, result.TrackerId, "bh", file);

        //    }
        //}

        private Logger logger;
        private IServerService serverService;
        private ICacheService cacheService;
    }
}
