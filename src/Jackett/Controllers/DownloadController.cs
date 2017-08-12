using Jackett.Services;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using MonoTorrent.BEncoding;
using Jackett.Utils;

namespace Jackett.Controllers
{
    [AllowAnonymous]
    [JackettAPINoCache]
    public class DownloadController : ApiController
    {
        Logger logger;
        IIndexerManagerService indexerService;
        IServerService serverService;
        IProtectionService protectionService;

        public DownloadController(IIndexerManagerService i, Logger l, IServerService s, IProtectionService ps)
        {
            logger = l;
            indexerService = i;
            serverService = s;
            protectionService = ps;
        }

        [HttpGet]
        public async Task<HttpResponseMessage> Download(string indexerID, string path, string jackett_apikey, string file)
        {
            try
            {
                var indexer = indexerService.GetWebIndexer(indexerID);

                if (!indexer.IsConfigured)
                {
                    logger.Warn(string.Format("Rejected a request to {0} which is unconfigured.", indexer.DisplayName));
                    return Request.CreateResponse(HttpStatusCode.Forbidden, "This indexer is not configured.");
                }

                path = Encoding.UTF8.GetString(HttpServerUtility.UrlTokenDecode(path));
                path = protectionService.UnProtect(path);

                if (serverService.Config.APIKey != jackett_apikey)
                    return new HttpResponseMessage(HttpStatusCode.Unauthorized);

                var target = new Uri(path, UriKind.RelativeOrAbsolute);
                var downloadBytes = await indexer.Download(target);

                // This will fix torrents where the keys are not sorted, and thereby not supported by Sonarr.
                var torrentDictionary = BEncodedDictionary.DecodeTorrent(downloadBytes);
                downloadBytes = torrentDictionary.Encode();

                var result = new HttpResponseMessage(HttpStatusCode.OK);
                result.Content = new ByteArrayContent(downloadBytes);
                result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-bittorrent");
                result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = StringUtil.MakeValidFileName(file, '_', false) // call MakeValidFileName again to avoid any kind of injection attack
                };
                return result;
            }
            catch (Exception e)
            {
                logger.Error(e, "Error downloading " + indexerID + " " + path);
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
        }
    }
}
