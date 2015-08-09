using Jackett.Services;
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
    public class BlackholeController : ApiController
    {
        private Logger logger;
        private IIndexerManagerService indexerService;

        public BlackholeController(IIndexerManagerService i, Logger l)
        {
            logger = l;
            indexerService = i;
        }

        [HttpGet]
        public async Task<IHttpActionResult> Blackhole(string indexerID, string path)
        {

            var jsonReply = new JObject();
            try
            {
                var indexer = indexerService.GetIndexer(indexerID);
                if (!indexer.IsConfigured)
                {
                    logger.Warn(string.Format("Rejected a request to {0} which is unconfigured.", indexer.DisplayName));
                    throw new Exception("This indexer is not configured.");
                }

                var remoteFile = Encoding.UTF8.GetString(HttpServerUtility.UrlTokenDecode(path));
                var downloadBytes = await indexer.Download(new Uri(remoteFile, UriKind.RelativeOrAbsolute));

                if (string.IsNullOrWhiteSpace(Engine.Server.Config.BlackholeDir))
                {
                    throw new Exception("Blackhole directory not set!");
                }

                if (!Directory.Exists(Engine.Server.Config.BlackholeDir))
                {
                    throw new Exception("Blackhole directory does not exist: " + Engine.Server.Config.BlackholeDir);
                }

                var fileName = DateTime.Now.Ticks + ".torrent";
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
