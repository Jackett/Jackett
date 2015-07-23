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

namespace Jackett.Controllers
{
    [AllowAnonymous]
    public class DownloadController : ApiController
    {
        private Logger logger;
        private IIndexerManagerService indexerService;

        public DownloadController(IIndexerManagerService i, Logger l)
        {
            logger = l;
            indexerService = i;
        }

        [HttpGet]
        public async Task<HttpResponseMessage> Download(string indexerID, string path)
        {
            try
            {
                var indexer = indexerService.GetIndexer(indexerID);
                var remoteFile = Encoding.UTF8.GetString(HttpServerUtility.UrlTokenDecode(path));
                var downloadBytes = await indexer.Download(new Uri(remoteFile));

                var result = new HttpResponseMessage(HttpStatusCode.OK);
                result.Content = new ByteArrayContent(downloadBytes);
                result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-bittorrent");
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
