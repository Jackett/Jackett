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

namespace Jackett.Controllers
{
    [AllowAnonymous]
    [JackettAPINoCache]
    public class DownloadController : ApiController
    {
        Logger logger;
        IIndexerManagerService indexerService;
        IServerService serverService;

        public DownloadController(IIndexerManagerService i, Logger l, IServerService s)
        {
            logger = l;
            indexerService = i;
            serverService = s;
        }

        [HttpGet]
        public async Task<HttpResponseMessage> Download(string indexerID, string path, string apikey, string file)
        {
            try
            {
                var indexer = indexerService.GetIndexer(indexerID);

                if (!indexer.IsConfigured)
                {
                    logger.Warn(string.Format("Rejected a request to {0} which is unconfigured.", indexer.DisplayName));
                    return Request.CreateResponse(HttpStatusCode.Forbidden, "This indexer is not configured.");
                }

                path = Encoding.UTF8.GetString(HttpServerUtility.UrlTokenDecode(path));

                if (serverService.Config.APIKey != apikey)
                    return new HttpResponseMessage(HttpStatusCode.Unauthorized);

                var target = new Uri(path, UriKind.RelativeOrAbsolute);
                target = indexer.UncleanLink(target);

                var downloadBytes = await indexer.Download(target);

                // This will fix torrents where the keys are not sorted, and thereby not supported by Sonarr.
                var torrentDictionary = BEncodedDictionary.DecodeTorrent(downloadBytes);
                downloadBytes = torrentDictionary.Encode();

				char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
				for(int i=0;i<file.Count();i++)
					if(invalidChars.Contains(file[i])) {
						file = file.Remove(i, 1).Insert(i, " ");
					}
				
                var result = new HttpResponseMessage(HttpStatusCode.OK);
                result.Content = new ByteArrayContent(downloadBytes);
                result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-bittorrent");
                result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = file
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
