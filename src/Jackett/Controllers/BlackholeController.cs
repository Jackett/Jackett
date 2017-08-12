using Jackett.Services;
using Jackett.Utils;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace Jackett.Controllers
{
    [AllowAnonymous]
    [JackettAPINoCache]
    public class BlackholeController : ApiController
    {
        private Logger logger;
        private IIndexerManagerService indexerService;
        IServerService serverService;
        IProtectionService protectionService;

        public BlackholeController(IIndexerManagerService i, Logger l, IServerService s, IProtectionService ps)
        {
            logger = l;
            indexerService = i;
            serverService = s;
            protectionService = ps;
        }

        [HttpGet]
        public async Task<IHttpActionResult> Blackhole(string indexerID, string path, string jackett_apikey, string file)
        {

            var jsonReply = new JObject();
            try
            {
                var indexer = indexerService.GetWebIndexer(indexerID);
                if (!indexer.IsConfigured)
                {
                    logger.Warn(string.Format("Rejected a request to {0} which is unconfigured.", indexer.DisplayName));
                    throw new Exception("This indexer is not configured.");
                }

                if (serverService.Config.APIKey != jackett_apikey)
                    throw new Exception("Incorrect API key");

                path = Encoding.UTF8.GetString(HttpServerUtility.UrlTokenDecode(path));
                path = protectionService.UnProtect(path);
                var remoteFile = new Uri(path, UriKind.RelativeOrAbsolute);
                var downloadBytes = await indexer.Download(remoteFile);

                if (string.IsNullOrWhiteSpace(Engine.Server.Config.BlackholeDir))
                {
                    throw new Exception("Blackhole directory not set!");
                }

                if (!Directory.Exists(Engine.Server.Config.BlackholeDir))
                {
                    throw new Exception("Blackhole directory does not exist: " + Engine.Server.Config.BlackholeDir);
                }

                var fileName = DateTime.Now.Ticks.ToString() + "-" + StringUtil.MakeValidFileName(indexer.DisplayName, '_', false);
                if (string.IsNullOrWhiteSpace(file))
                    fileName += ".torrent";
                else
                    fileName += "-"+StringUtil.MakeValidFileName(file, '_', false); // call MakeValidFileName() again to avoid any possibility of path traversal attacks 

                File.WriteAllBytes(Path.Combine(Engine.Server.Config.BlackholeDir, fileName), downloadBytes);
                jsonReply["result"] = "success";
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error downloading to blackhole " + indexerID + " " + path);
                jsonReply["result"] = "error";
                jsonReply["error"] = ex.Message;
            }

            return Json(jsonReply);
        }
    }
}
