using System;
using System.Text;
using System.Threading.Tasks;
using BencodeNET.Objects;
using BencodeNET.Parsing;
using Jackett.Common.Models.Config;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
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
    [Route("dl/{indexerId}")]
    public class DownloadController : Controller
    {
        private readonly ServerConfig _serverConfig;
        private readonly Logger _logger;
        private readonly IIndexerManagerService _indexerService;
        private readonly IProtectionService _protectionService;

        public DownloadController(IIndexerManagerService i, Logger l, IProtectionService ps, ServerConfig sConfig)
        {
            _serverConfig = sConfig;
            _logger = l;
            _indexerService = i;
            _protectionService = ps;
        }

        [HttpGet]
        public async Task<IActionResult> DownloadAsync(string indexerId, string path, string jackett_apikey, string file)
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

                _logger.Info($"Downloading on {path}...");

                path = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(path));
                path = _protectionService.UnProtect(path);

                var target = new Uri(path, UriKind.RelativeOrAbsolute);

                _logger.Info($"Downloading on target {target}...");

                var downloadBytes = await indexer.Download(target);

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
                    var magnetUrl = Encoding.UTF8.GetString(downloadBytes);
                    return Redirect(magnetUrl);
                }

                // This will fix torrents where the keys are not sorted, and thereby not supported by Sonarr.
                byte[] sortedDownloadBytes;
                try
                {
                    var parser = new BencodeParser();
                    var torrentDictionary = parser.Parse(downloadBytes);
                    sortedDownloadBytes = torrentDictionary.EncodeAsBytes();
                }
                catch (Exception e)
                {
                    var content = indexer.Encoding.GetString(downloadBytes);
                    _logger.Error(content);
                    throw new Exception("BencodeParser failed", e);
                }

                var fileName = StringUtil.MakeValidFileName(file, '_', false) + ".torrent"; // call MakeValidFileName again to avoid any kind of injection attack

                return File(sortedDownloadBytes, "application/x-bittorrent", fileName);
            }
            catch (Exception e)
            {
                _logger.Error(e);
                _logger.Error($"Error downloading. " +
                              $"indexer: {indexerId.Replace(Environment.NewLine, "")} " +
                              $"path: {path.Replace(Environment.NewLine, "")}\n{e}");
                return NotFound();
            }
        }
    }
}
