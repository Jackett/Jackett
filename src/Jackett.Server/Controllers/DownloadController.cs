using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using BencodeNET.Objects;
using BencodeNET.Parsing;
using Jackett.Common.Models.Config;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Server.ActionFilters;
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

                if (indexer == null || !indexer.IsConfigured)
                    return Forbid();

                path = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(path));
                path = _protectionService.UnProtect(path);

                var target = new Uri(path, UriKind.RelativeOrAbsolute);
                var downloadBytes = await indexer.Download(target);

                if (downloadBytes.Length >= 7 &&
                    downloadBytes[0] == 0x6d &&
                    downloadBytes[1] == 0x61 &&
                    downloadBytes[2] == 0x67 &&
                    downloadBytes[3] == 0x6e &&
                    downloadBytes[4] == 0x65 &&
                    downloadBytes[5] == 0x74 &&
                    downloadBytes[6] == 0x3a)
                {
                    var magnetUrl = Encoding.UTF8.GetString(downloadBytes);
                    return Redirect(magnetUrl);
                }

                byte[] sortedDownloadBytes;

                try
                {
                    if (downloadBytes.Length > 0 && downloadBytes[0] == (byte)'d')
                    {
                        int i = 1;
                        var items = new List<(byte[] key, byte[] value)>();

                        while (i < downloadBytes.Length && downloadBytes[i] != (byte)'e')
                        {
                            int colon = Array.IndexOf(downloadBytes, (byte)':', i);
                            if (colon == -1) break;

                            if (!int.TryParse(Encoding.ASCII.GetString(downloadBytes, i, colon - i), out int keyLen))
                                break;

                            int keyStart = colon + 1;
                            int keyEnd = keyStart + keyLen;
                            if (keyEnd > downloadBytes.Length) break;

                            var key = downloadBytes[keyStart..keyEnd];
                            i = keyEnd;

                            int valStart = i;
                            i = SkipBencodeElement(downloadBytes, i);
                            if (i <= valStart || i > downloadBytes.Length) break;

                            var val = downloadBytes[valStart..i];
                            items.Add((key, val));
                        }

                        sortedDownloadBytes = new List<byte> { (byte)'d' }
                            .Concat(items
                                .OrderBy(it => Encoding.ASCII.GetString(it.key))
                                .SelectMany(kv =>
                                    Encoding.ASCII.GetBytes(kv.key.Length + ":")
                                        .Concat(kv.key)
                                        .Concat(kv.value)
                                )
                            )
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
                catch
                {
                    sortedDownloadBytes = downloadBytes;
                }

                int SkipBencodeElement(byte[] data, int index)
                {
                    if (index >= data.Length) return index;

                    var c = data[index];

                    if (c == (byte)'i')
                    {
                        int end = Array.IndexOf(data, (byte)'e', index);
                        return end == -1
                            ? data.Length
                            : end + 1;
                    }

                    if (c == (byte)'l' || c == (byte)'d')
                    {
                        index++;
                        while (index < data.Length && data[index] != (byte)'e')
                            index = SkipBencodeElement(data, index);
                        return index < data.Length ? index + 1 : data.Length;
                    }

                    if (c >= (byte)'0' && c <= (byte)'9')
                    {
                        int colon = Array.IndexOf(data, (byte)':', index);
                        if (colon == -1) return data.Length;

                        if (!int.TryParse(Encoding.ASCII.GetString(data, index, colon - index), out int len))
                            return data.Length;

                        return Math.Min(colon + 1 + len, data.Length);
                    }

                    return index + 1;
                }

                var fileName = StringUtil.MakeValidFileName(file ?? "download", '_', false) + ".torrent";
                return File(sortedDownloadBytes, "application/x-bittorrent", fileName);
            }
            catch
            {
                return NotFound();
            }
        }
    }
}
