using System;
using System.Collections.Generic;
using System.Linq;
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

        public DownloadController(
            IIndexerManagerService i,
            Logger l,
            IProtectionService ps,
            ServerConfig sConfig)
        {
            _serverConfig = sConfig;
            _logger = l;
            _indexerService = i;
            _protectionService = ps;
        }

        [HttpGet]
        public async Task<IActionResult> DownloadAsync(
            string indexerId,
            string path,
            string jackett_apikey,
            string file)
        {
            try
            {
                if (_serverConfig.APIKey != jackett_apikey)
                    return Unauthorized();

                var indexer = _indexerService.GetWebIndexer(indexerId);

                if (!indexer.IsConfigured)
                {
                    _logger.Warn(
                        $"Rejected a request to {indexer.Name} which is unconfigured.");
                    return Forbid("This indexer is not configured.");
                }

                path = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(path));
                path = _protectionService.UnProtect(path);

                var target = new Uri(path, UriKind.RelativeOrAbsolute);
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

                            int keyLen = int.Parse(
                                Encoding.ASCII.GetString(downloadBytes, i, colon - i));
                            int keyStart = colon + 1;
                            int keyEnd = keyStart + keyLen;
                            if (keyEnd > downloadBytes.Length) break;

                            var key = new byte[keyEnd - keyStart];
                            Array.Copy(downloadBytes, keyStart, key, 0, keyEnd - keyStart);
                            i = keyEnd;

                            // Read value
                            int valStart = i;
                            i = SkipBencodeElement(downloadBytes, i);
                            var val = new byte[i - valStart];
                            Array.Copy(downloadBytes, valStart, val, 0, i - valStart);

                            items.Add((key, val));
                        }

                        // Rebuild sorted top-level dictionary
                        var list = new List<byte> { (byte)'d' };
                        foreach (var kv in items.OrderBy(it => Encoding.ASCII.GetString(it.key)))
                        {
                            var keyBytes = Encoding.ASCII.GetBytes(kv.key.Length + ":");
                            list.AddRange(keyBytes);
                            list.AddRange(kv.key);
                            list.AddRange(kv.value);
                        }
                        list.Add((byte)'e');
                        sortedDownloadBytes = list.ToArray();
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
                    var content = indexer.Encoding.GetString(downloadBytes);
                    _logger.Error(content);
                    throw new Exception("BencodeParser failed", e);
                }

                int SkipBencodeElement(byte[] data, int index)
                {
                    if (index >= data.Length) return index;

                    var c = data[index];

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

                var fileName = StringUtil.MakeValidFileName(file, '_', false)
                                + ".torrent"; // call MakeValidFileName again to avoid any kind of injection attack
                return File(sortedDownloadBytes, "application/x-bittorrent", fileName);
            }
            catch (Exception e)
            {
                _logger.Error(
                    $"Error downloading. " +
                    $"indexer: {indexerId.Replace(Environment.NewLine, "")} " +
                    $"path: {path.Replace(Environment.NewLine, "")}\n{e}");
                return NotFound();
            }
        }
    }
}
