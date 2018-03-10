using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Jackett.Common.Models.Config;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;

namespace Jackett.Controllers
{
    [AllowAnonymous]
    [JackettAPINoCache]
    public class BlackholeController : ApiController
    {
        private Logger logger;
        private IIndexerManagerService indexerService;
        private readonly ServerConfig serverConfig;
        IProtectionService protectionService;

        public BlackholeController(IIndexerManagerService i, Logger l, ServerConfig config, IProtectionService ps)
        {
            logger = l;
            indexerService = i;
            serverConfig = config;

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

                if (serverConfig.APIKey != jackett_apikey)
                    throw new Exception("Incorrect API key");

                path = Encoding.UTF8.GetString(HttpServerUtility.UrlTokenDecode(path));
                path = protectionService.UnProtect(path);
                var remoteFile = new Uri(path, UriKind.RelativeOrAbsolute);
                var fileExtension = ".torrent";
                var downloadBytes = await indexer.Download(remoteFile);

                // handle magnet URLs
                if (downloadBytes.Length >= 7
                    && downloadBytes[0] == 0x6d // m
                    && downloadBytes[1] == 0x61 // a
                    && downloadBytes[2] == 0x67 // g
                    && downloadBytes[3] == 0x6e // n
                    && downloadBytes[4] == 0x65 // e
                    && downloadBytes[5] == 0x74 // t
                    && downloadBytes[6] == 0x3a // :
                    )
                {
                    fileExtension = ".magnet";
                }

                if (string.IsNullOrWhiteSpace(serverConfig.BlackholeDir))
                {
                    throw new Exception("Blackhole directory not set!");
                }

                if (!Directory.Exists(serverConfig.BlackholeDir))
                {
                    throw new Exception("Blackhole directory does not exist: " + serverConfig.BlackholeDir);
                }

                var fileName = DateTime.Now.Ticks.ToString() + "-" + StringUtil.MakeValidFileName(indexer.DisplayName, '_', false);
                if (string.IsNullOrWhiteSpace(file))
                    fileName += fileExtension;
                else
                    fileName += "-"+StringUtil.MakeValidFileName(file + fileExtension, '_', false); // call MakeValidFileName() again to avoid any possibility of path traversal attacks 

                File.WriteAllBytes(Path.Combine(serverConfig.BlackholeDir, fileName), downloadBytes);
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
