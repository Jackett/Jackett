using Autofac;
using Jackett.Models;
using Jackett.Services;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Jackett.Controllers
{
    [RoutePrefix("admin")]
    public class AdminController : ApiController
    {
        private IConfigurationService config;
        private IIndexerManagerService indexerService;
        private IServerService serverService;

        public AdminController(IConfigurationService config, IIndexerManagerService i, IServerService ss)
        {
            this.config = config;
            indexerService = i;
            serverService = ss;
        }

        private async Task<JToken> ReadPostDataJson()
        {
            var content = await Request.Content.ReadAsStringAsync();
            return JObject.Parse(content);
        }

        [Route("get_config_form")]
        [HttpPost]
        public async Task<IHttpActionResult> GetConfigForm()
        {
            var jsonReply = new JObject();
            try
            {
                var postData = await ReadPostDataJson();
                var indexer = indexerService.GetIndexer((string)postData["indexer"]);
                var config = await indexer.GetConfigurationForSetup();
                jsonReply["config"] = config.ToJson();
                jsonReply["name"] = indexer.DisplayName;
                jsonReply["result"] = "success";
            }
            catch (Exception ex)
            {
                jsonReply["result"] = "error";
                jsonReply["error"] = ex.Message;
            }
            return Json(jsonReply);
        }

        [Route("configure_indexer")]
        [HttpPost]
        public async Task<IHttpActionResult> Configure()
        {
            JToken jsonReply = new JObject();
            try
            {
                var postData = await ReadPostDataJson();
                string indexerString = (string)postData["indexer"];
                var indexer = indexerService.GetIndexer((string)postData["indexer"]);
                jsonReply["name"] = indexer.DisplayName;
                await indexer.ApplyConfiguration(postData["config"]);
                await indexerService.TestIndexer((string)postData["indexer"]);
                jsonReply["result"] = "success";
            }
            catch (Exception ex)
            {
                jsonReply["result"] = "error";
                jsonReply["error"] = ex.Message;
                if (ex is ExceptionWithConfigData)
                {
                    jsonReply["config"] = ((ExceptionWithConfigData)ex).ConfigData.ToJson();
                }
            }
            return Json(jsonReply);
        }



        [Route("get_indexers")]
        [HttpGet]
        public IHttpActionResult Indexers()
        {
            var jsonReply = new JObject();
            try
            {
                jsonReply["result"] = "success";
                jsonReply["api_key"] = serverService.Config.APIKey;
                jsonReply["app_version"] = config.GetVersion();
                JArray items = new JArray();

                foreach (var indexer in indexerService.GetAllIndexers())
                {
                    var item = new JObject();
                    item["id"] = indexer.GetType().Name;
                    item["name"] = indexer.DisplayName;
                    item["description"] = indexer.DisplayDescription;
                    item["configured"] = indexer.IsConfigured;
                    item["site_link"] = indexer.SiteLink;
                    items.Add(item);
                }
                jsonReply["items"] = items;
            }
            catch (Exception ex)
            {
                jsonReply["result"] = "error";
                jsonReply["error"] = ex.Message;
            }
            return Json(jsonReply);
        }

        [Route("test_indexer")]
        [HttpPost]
        public async Task<IHttpActionResult> Test()
        {
            JToken jsonReply = new JObject();
            try
            {
                var postData = await ReadPostDataJson();
                string indexerString = (string)postData["indexer"];
                await indexerService.TestIndexer(indexerString);
                jsonReply["name"] = indexerService.GetIndexer(indexerString).DisplayName;
                jsonReply["result"] = "success";
            }
            catch (Exception ex)
            {
                jsonReply["result"] = "error";
                jsonReply["error"] = ex.Message;
            }
            return Json(jsonReply);
        }

        [Route("delete_indexer")]
        [HttpPost]
        public async Task<IHttpActionResult> Delete()
        {
            var jsonReply = new JObject();
            try
            {
                var postData = await ReadPostDataJson();
                string indexerString = (string)postData["indexer"];
                indexerService.DeleteIndexer(indexerString);
            }
            catch (Exception ex)
            {
                jsonReply["result"] = "error";
                jsonReply["error"] = ex.Message;
            }
            return Json(jsonReply);
        }

        [Route("get_jackett_config")]
        [HttpGet]
        public IHttpActionResult GetConfig()
        {
            var jsonReply = new JObject();
            try
            {
                jsonReply["config"] = config.ReadServerSettingsFile();
                jsonReply["result"] = "success";
            }
            catch (CustomException ex)
            {
                jsonReply["result"] = "error";
                jsonReply["error"] = ex.Message;
            }
            catch (Exception ex)
            {
                jsonReply["result"] = "error";
                jsonReply["error"] = ex.Message;
            }
            return Json(jsonReply);
        }

        [Route("apply_jackett_config")]
        [HttpPost]
        public async Task<IHttpActionResult> SetConfig()
        {
            var jsonReply = new JObject();
            try
            {
                var postData = await ReadPostDataJson();
                //  int port = await WebServer.ApplyPortConfiguration(postData);
                jsonReply["result"] = "success";
                // jsonReply["port"] = port;
            }
            catch (Exception ex)
            {
                jsonReply["result"] = "error";
                jsonReply["error"] = ex.Message;
            }
            return Json(jsonReply);
        }


        [Route("jackett_restart")]
        [HttpPost]
        public IHttpActionResult Restart()
        {
            return null;
        }
    }
}

