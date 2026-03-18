using System;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
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

        public DownloadController(IIndexerManagerService indexerService, Logger logger, IProtectionService protectionService, ServerConfig serverConfig)
        {
            _serverConfig = serverConfig;
            _logger = logger;
            _indexerService = indexerService;
            _protectionService = protectionService;
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

                path = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(path));
                path = _protectionService.UnProtect(path);

                var target = new Uri(path, UriKind.RelativeOrAbsolute);
                var downloadBytes = await indexer.Download(target);

                // handle magnet links
                if (downloadBytes.Length >= 7 &&
                    downloadBytes[0] == (byte)'m' &&
                    downloadBytes[1] == (byte)'a' &&
                    downloadBytes[2] == (byte)'g' &&
                    downloadBytes[3] == (byte)'n' &&
                    downloadBytes[4] == (byte)'e' &&
                    downloadBytes[5] == (byte)'t' &&
                    downloadBytes[6] == (byte)':')
                {
                    var magnetUrl = Encoding.UTF8.GetString(downloadBytes);
                    return Redirect(magnetUrl);
                }

                // This will fix torrents where the keys are not sorted, and thereby not supported by Sonarr.
                // Fix torrents with unsorted top-level keys
                byte[] sortedDownloadBytes;
                try
                {
                    if (downloadBytes.Length > 0 && downloadBytes[0] == (byte)'d')
                    {
                        int i = 1;
                        var items = new List<(byte[] key, byte[] value)>();

                        while (i < downloadBytes.Length && downloadBytes[i] != (byte)'e')
                        {
                            // Read key
                            int colon = Array.IndexOf(downloadBytes, (byte)':', i);
                            if (colon == -1) break;

                            int keyLen = int.Parse(Encoding.ASCII.GetString(downloadBytes, i, colon - i));
                            int keyStart = colon + 1;
                            int keyEnd = keyStart + keyLen;
                            if (keyEnd > downloadBytes.Length) break;

                            var key = downloadBytes[keyStart..keyEnd]; 
                            i = keyEnd;

                            // Read value
                            int valStart = i;
                            i = SkipBencodeElement(downloadBytes, i);
                            var val = downloadBytes[valStart..i]; 

                            items.Add((key, val));
                        }

                        // Rebuild sorted top-level dictionary
                        sortedDownloadBytes = new List<byte> { (byte)'d' }
                            .Concat(items
                                .OrderBy(it => Encoding.ASCII.GetString(it.key))
                                .SelectMany(kv => Encoding.ASCII.GetBytes(kv.key.Length + ":")
                                                    .Concat(kv.key)
                                                    .Concat(kv.value)))
                            .Concat(new byte[] { (byte)'e' })
                            .ToArray();
                    }
                    else
                    {
                        var parser = new BencodeParser();
                        var torrentDictionary = parser.Parse(downloadBytes);
                        sortedDownloadBytes = torrentDictionary.EncodeAsBytes();
                    }
                }
                catch (Exception e)
                {
                    _logger.Error(indexer.Encoding.GetString(downloadBytes));
                    throw new Exception("BencodeParser failed", e);
                }

                var fileName = StringUtil.MakeValidFileName(file, '_', false) + ".torrent";
                return File(sortedDownloadBytes, "application/x-bittorrent", fileName);
            }
            catch (Exception e)
            {
                _logger.Error($"Error downloading. indexer: {indexerId.Replace(Environment.NewLine, "")} path: {path.Replace(Environment.NewLine, "")}\n{e}");
                return NotFound();
            }

            int SkipBencodeElement(byte[] data, int index)
            {
                if (index >= data.Length) return index;

                byte c = data[index];

                if (c == (byte)'i')
                    return Array.IndexOf(data, (byte)'e', index) + 1;

                if (c == (byte)'l' || c == (byte)'d')
                {
                    index++;
                    while (index < data.Length && data[index] != (byte)'e')
                        index = SkipBencodeElement(data, index);
                    return index + 1;
                }

                if (c >= (byte)'0' && c <= (byte)'9')
                {
                    int colon = Array.IndexOf(data, (byte)':', index);
                    int len = int.Parse(Encoding.ASCII.GetString(data, index, colon - index));
                    return colon + 1 + len;
                }

                return index + 1;
            }
        }
    }
}
