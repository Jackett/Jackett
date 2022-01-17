using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jackett.Common.Models.Config;
using Jackett.Common.Services.Interfaces;
using Jackett.Server.ActionFilters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using NLog;

namespace Jackett.Server.Controllers
{
    [AllowAnonymous]
    [DownloadActionFilter]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    [Route("img/{indexerID}")]
    public class ImageController : Controller
    {
        private readonly ServerConfig serverConfig;
        private readonly Logger logger;
        private readonly IIndexerManagerService indexerService;
        private readonly IProtectionService protectionService;

        public ImageController(IIndexerManagerService i, Logger l, IProtectionService ps, ServerConfig sConfig)
        {
            serverConfig = sConfig;
            logger = l;
            indexerService = i;
            protectionService = ps;
        }

        [HttpGet]
        public async Task<IActionResult> DownloadImage(string indexerID, string path, string jackett_apikey, string file)
        {
            try
            {
                if (serverConfig.APIKey != jackett_apikey)
                    return Unauthorized();

                var indexer = indexerService.GetWebIndexer(indexerID);
                if (!indexer.IsConfigured)
                {
                    logger.Warn($"Rejected a request to {indexer.DisplayName} which is unconfigured.");
                    return Forbid("This indexer is not configured.");
                }

                path = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(path));
                path = protectionService.UnProtect(path);

                var target = new Uri(path, UriKind.RelativeOrAbsolute);
                var response = await indexer.DownloadImage(target);

                if (response.Status != System.Net.HttpStatusCode.OK &&
                    response.Status != System.Net.HttpStatusCode.Continue &&
                    response.Status != System.Net.HttpStatusCode.PartialContent)
                    return new StatusCodeResult((int)response.Status);

                var contentType = response.Headers.ContainsKey("content-type") ?
                    response.Headers["content-type"].First() :
                    "image/jpeg";
                return File(response.ContentBytes, contentType);
            }
            catch (Exception e)
            {
                logger.Debug($"Error downloading image. indexer: {indexerID} path: {path}\n{e}");
                return new StatusCodeResult((int)System.Net.HttpStatusCode.InternalServerError);
            }
        }
    }
}
