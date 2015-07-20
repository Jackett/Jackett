using Autofac;
using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Results;
using System.Web.Security;

namespace Jackett.Controllers
{
    [RoutePrefix("admin")]
    [JackettAuthorized]
    public class AdminController : ApiController
    {
        private IConfigurationService config;
        private IIndexerManagerService indexerService;
        private IServerService serverService;
        private ISecuityService securityService;

        public AdminController(IConfigurationService config, IIndexerManagerService i, IServerService ss, ISecuityService s)
        {
            this.config = config;
            indexerService = i;
            serverService = ss;
            securityService = s;
        }

        private async Task<JToken> ReadPostDataJson()
        {
            var content = await Request.Content.ReadAsStringAsync();
            return JObject.Parse(content);
        }


        private HttpResponseMessage GetFile(string path)
        {
            var result = new HttpResponseMessage(HttpStatusCode.OK);
            var mappedPath = Path.Combine(config.GetContentFolder(), path);
            var stream = new FileStream(mappedPath, FileMode.Open);
            result.Content = new StreamContent(stream);
            result.Content.Headers.ContentType =
                new MediaTypeHeaderValue(MimeMapping.GetMimeMapping(mappedPath));
            
            return result;
        }

        [HttpGet]
        [AllowAnonymous]
        public RedirectResult Logout()
        {
            var ctx = Request.GetOwinContext();
            var authManager = ctx.Authentication;
            authManager.SignOut("ApplicationCookie");
            return Redirect("/Admin/Dashboard");
        }

        [HttpGet]
        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> Dashboard()
        {
            if(Request.RequestUri.Query!=null && Request.RequestUri.Query.Contains("logout"))
            {
                var file = GetFile("login.html");
                securityService.Logout(file);
                return file;
            }


            if (securityService.CheckAuthorised(Request))
            {
                return GetFile("index.html");

            } else
            {
                var formData = await Request.Content.ReadAsFormDataAsync();
                
                if (formData!=null && securityService.HashPassword(formData["password"]) == serverService.Config.AdminPassword)
                {
                    var file = GetFile("index.html");
                    securityService.Login(file);
                    return file;
                } else
                {
                    return GetFile("login.html");
                }
            }
        }

        [Route("set_admin_password")]
        [HttpPost]
        public async Task<IHttpActionResult> SetAdminPassword()
        {
            var jsonReply = new JObject();
            try
            {
                var postData = await ReadPostDataJson();
                var password = (string)postData["password"];
                if (string.IsNullOrEmpty(password))
                {
                    serverService.Config.AdminPassword = string.Empty;
                }
                else
                {
                    serverService.Config.AdminPassword = securityService.HashPassword(password);
                }

                serverService.SaveConfig();
                jsonReply["result"] = "success";
            }
            catch (Exception ex)
            {
                jsonReply["result"] = "error";
                jsonReply["error"] = ex.Message;
            }
            return Json(jsonReply);
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
                JArray items = new JArray();

                foreach (var indexer in indexerService.GetAllIndexers())
                {
                    var item = new JObject();
                    item["id"] = indexer.ID;
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
                var cfg = new JObject();
                cfg["port"] = serverService.Config.Port;
                cfg["api_key"] = serverService.Config.APIKey;
                cfg["password"] = string.IsNullOrEmpty(serverService.Config.AdminPassword )? string.Empty:serverService.Config.AdminPassword.Substring(0,10);

                jsonReply["config"] = cfg;
                jsonReply["app_version"] = config.GetVersion();
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

