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
    [Route("img/{indexerId}")]
    public class ImageController : Controller
    {
        private readonly ServerConfig _serverConfig;
        private readonly Logger _logger;
        private readonly IIndexerManagerService _indexerService;
        private readonly IProtectionService _protectionService;

        public ImageController(IIndexerManagerService i, Logger l, IProtectionService ps, ServerConfig sConfig)
        {
            _serverConfig = sConfig;
            _logger = l;
            _indexerService = i;
            _protectionService = ps;
        }

        [HttpGet]
        public async Task<IActionResult> DownloadImageAsync(string indexerId, string path, string jackett_apikey, string file)
        {
            try
            {
                if (_serverConfig.APIKey != jackett_apikey)
                    return Unauthorized();

                var indexer = _indexerService.GetWebIndexer(indexerId);
                if (!indexer.IsConfigured)
                {
                    _logger.Warn($"Rejected a request to {indexer.Name} which is unconfigured.");
                    return Forbid("This indexer is not configured.");
                }

                path = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(path));
                path = _protectionService.UnProtect(path);

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
                _logger.Debug($"Error downloading image. " +
                              $"indexer: {indexerId.Replace(Environment.NewLine, "")} " +
                              $"path: {path.Replace(Environment.NewLine, "")}\n{e}");
                return new StatusCodeResult((int)System.Net.HttpStatusCode.InternalServerError);
            }
        }
    }
}
