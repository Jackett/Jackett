using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Jackett.Common.Extensions;
using Jackett.Common.Helpers;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class Wolfmax4K : IndexerBase
    {
        public override string Id => "wolfmax4k";
        public override string Name => "Wolfmax 4k";
        public override string Description => "Wolfmax 4k is a SPANISH public tracker for MOVIES / TV";
        public override string SiteLink { get; protected set; } = "https://wolfmax4k.com/";
        public override string Language => "es-ES";
        public override string Type => "public";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private const string TorrentLinkEncryptionKey = "fee631d2cffda38a78b96ee6d2dfb43a";

        private static Dictionary<string, long> EstimatedSizeByCategory => new Dictionary<string, long>
        {
            { Wolfmax4KCatType.Pelicula, 2.Gigabytes() },
            { Wolfmax4KCatType.Pelicula720, 5.Gigabytes() },
            { Wolfmax4KCatType.Pelicula1080, 15.Gigabytes() },
            { Wolfmax4KCatType.Pelicula4K, 30.Gigabytes() },
            { Wolfmax4KCatType.Serie, 512.Megabytes() },
            { Wolfmax4KCatType.Serie720, 1.Gigabytes() },
            { Wolfmax4KCatType.Serie1080, 3.Gigabytes() },
            { Wolfmax4KCatType.Serie4K, 8.Gigabytes() }
        };

        public Wolfmax4K(IIndexerConfigurationService configService, WebClient w, Logger l, IProtectionService ps,
                         ICacheService cs)
            : base(configService: configService,
                   client: w,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationData())
        {
            // avoid Cloudflare too many requests limiter
            webclient.requestDelay = 2.1;
        }

        private static TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                },
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q
                }
            };
            caps.Categories.AddCategoryMapping(Wolfmax4KCatType.Pelicula, TorznabCatType.MoviesSD, "Peliculas");
            caps.Categories.AddCategoryMapping(Wolfmax4KCatType.Pelicula720, TorznabCatType.Movies, "Peliculas 720p");
            caps.Categories.AddCategoryMapping(Wolfmax4KCatType.Pelicula1080, TorznabCatType.MoviesHD, "Peliculas 1080p");
            caps.Categories.AddCategoryMapping(Wolfmax4KCatType.Pelicula4K, TorznabCatType.MoviesUHD, "Peliculas 4k");

            caps.Categories.AddCategoryMapping(Wolfmax4KCatType.Serie, TorznabCatType.TVSD, "Series");
            caps.Categories.AddCategoryMapping(Wolfmax4KCatType.Serie720, TorznabCatType.TV, "Series 720p");
            caps.Categories.AddCategoryMapping(Wolfmax4KCatType.Serie1080, TorznabCatType.TVHD, "Series 1080p");
            caps.Categories.AddCategoryMapping(Wolfmax4KCatType.Serie4K, TorznabCatType.TVUHD, "Series 4k");

            return caps;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var releases = await PerformQuery(new TorznabQuery());

            await ConfigureIfOK(string.Empty, releases.Any(), () =>
                                    throw new Exception("Could not find releases from this URL"));

            return IndexerConfigurationStatus.Completed;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            var searchToken = await GetSearchTokenAsync();

            query = SanitizeTorznabQuery(query);

            var maxPages = query.SearchTerm.IsNullOrWhiteSpace() ? 1 : 3;

            for (var i = 1; i <= maxPages; i++)
            {
                var result = await DoSeachAsync(query, searchToken, i);
                try
                {
                    // Parse results
                    var htmlParser = new HtmlParser();
                    using var doc = htmlParser.ParseDocument(result.ContentString);
                    var items = doc.QuerySelectorAll("#form-busqavanzada .card.card-movie");
                    releases.AddRange(items.Select(elm => ExtractReleaseInfo(elm, query)).ToList()
                                           .Where(x => x != null));

                    // Check if has more pages
                    var activePageElement = doc.QuerySelector(".btnpg.active");
                    var nextPageElement = activePageElement?.NextElementSibling;
                    if (activePageElement == null || nextPageElement == null ||
                        activePageElement.GetAttribute("data-pg") == nextPageElement.GetAttribute("data-pg"))
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    OnParseError(result.ContentString, ex);
                }
            }

            return releases;
        }

        public override async Task<byte[]> Download(Uri link)
        {
            var wmPage = await RequestWithCookiesAndRetryAsync(link.ToString(), emulateBrowser: false);
            var wmDoc = new HtmlParser().ParseDocument(wmPage.ContentString);
            var enlacitoUrl = wmDoc.QuerySelector(".app-message a")?.GetAttribute("href");

            var enlacitoPage = await RequestWithCookiesAndRetryAsync(enlacitoUrl, emulateBrowser: false, referer: SiteLink);
            var enlacitoDoc = new HtmlParser().ParseDocument(enlacitoPage.ContentString);
            var enlacitoFormUrl = enlacitoDoc.QuerySelector("form").GetAttribute("action");
            var enlacitoFormLinkser = enlacitoDoc.QuerySelector("input[name=\"linkser\"]").GetAttribute("value");


            var body = new Dictionary<string, string>
            {
                { "linkser", enlacitoFormLinkser }
            };
            var enlacito2Page = await RequestWithCookiesAndRetryAsync(enlacitoFormUrl, data: body, method: RequestType.POST, emulateBrowser: false);
            var regex = new Regex("var link_out = \"(.*)\"");
            var v = regex.Match(enlacito2Page.ContentString);

            var linkOut = v.Groups[1].ToString();
            var slink = Encoding.UTF8.GetString(Convert.FromBase64String(linkOut));
            var ulink = OpenSSLDecrypt(slink, TorrentLinkEncryptionKey);

            var result = await RequestWithCookiesAndRetryAsync(ulink, emulateBrowser: false);
            return result.ContentBytes;
        }

        private async Task<string> GetSearchTokenAsync()
        {
            var resultIdx = await RequestWithCookiesAndRetryAsync(SiteLink, emulateBrowser: false);
            var htmlParser = new HtmlParser();
            using var myDoc = htmlParser.ParseDocument(resultIdx.ContentString);
            return myDoc.QuerySelector("input[name='token']")?.GetAttribute("value");
        }

        private async Task<WebResult> DoSeachAsync(TorznabQuery query, string searchToken, int page = 1)
        {
            var body = new Dictionary<string, string>
            {
                // wolfmax category&quality search is broken, do not use
                { "_ACTION", "buscar" },
                { "token", searchToken },
                { "q", query.SearchTerm },
                { "pgb", page.ToString() }
            };

            var result = await RequestWithCookiesAndRetryAsync(SiteLink + "buscar", data: body, method: RequestType.POST, emulateBrowser: false);
            if (result.Status != HttpStatusCode.OK)
                throw new ExceptionWithConfigData(result.ContentString, configData);
            return result;
        }

        private static TorznabQuery SanitizeTorznabQuery(TorznabQuery query)
        {
            // Taken from Dontorrent.cs
            // Eg. Marco.Polo.2014.S02E08

            // the season/episode part is already parsed by Jackett
            // query.SanitizedSearchTerm = Marco.Polo.2014.
            // query.Season = 2
            // query.Episode = 8
            var searchTerm = query.SanitizedSearchTerm;

            // replace punctuation symbols with spaces
            // searchTerm = Marco Polo 2014
            searchTerm = Regex.Replace(searchTerm, @"[-._\(\)@/\\\[\]\+\%]", " ");
            searchTerm = Regex.Replace(searchTerm, @"\s+", " ");
            searchTerm = searchTerm.Trim();

            // we parse the year and remove it from search
            // searchTerm = Marco Polo
            // query.Year = 2014
            var r = new Regex("([ ]+([0-9]{4}))$", RegexOptions.IgnoreCase);
            var m = r.Match(searchTerm);
            if (m.Success)
            {
                query.Year = int.Parse(m.Groups[2].Value);
                searchTerm = searchTerm.Replace(m.Groups[1].Value, "");
            }

            // remove some words
            searchTerm = Regex.Replace(searchTerm, @"\b(espa[ñn]ol|spanish|castellano|spa)\b", "", RegexOptions.IgnoreCase);

            query.SearchTerm = searchTerm;
            return query;
        }

        private ReleaseInfo ExtractReleaseInfo(IElement cardElement, TorznabQuery query)
        {
            // https://wolfmax4k.com/descargar/peliculas-castellano/bebe-made-in-china-2020-/blurayrip-ac3-5-1/
            // https://wolfmax4k.com/descargar/la-sala-de-torturas-chinas/
            // https://wolfmax4k.com/pelicula/el-hombre-de-chinatown/
            // https://wolfmax4k.com/descargar/documentales/misterios-de-china/temporada-1/capitulo-03/
            // https://wolfmax4k.com/descargar/serie/the-legend-of-vox-machina/temporada-2/capitulo-11/
            // https://wolfmax4k.com/descargar/peliculas-x264-mkv/los-tesoros-del-mar-de-china-1987-/bluray-microhd/
            // https://wolfmax4k.com/descargar/serie/the-legend-of-vox-machina/temporada-1/capitulo-04-al-06/
            // https://wolfmax4k.com/descargar/serie-en-hd/top-boy/temporada-3/capitulo-02/
            // https://wolfmax4k.com/descargar/programas-tv/la-isla-de-las-tentaciones/temporada-7/capitulo-10/
            // https://wolfmax4k.com/descargar/serie-1080p/historial-delictivo/temporada-1/capitulo-02/
            // https://wolfmax4k.com/descargar-pelicula/avatar-v-extendida/bluray-1080p/

            var quality = cardElement.QuerySelector(".quality")?.Text().Trim();
            if (quality.IsNullOrWhiteSpace())
            {
                // Some torrents has no quality.
                // Ignored it because they are torrents that are not well categorized
                // as this game https://wolfmax4k.com/juego/james-cameronavatar/
                return null;
            }

            var link = new Uri(new Uri(SiteLink), cardElement.GetAttribute("href"));
            var title = ParseTitle(cardElement) + " SPANISH " + quality;
            var episodes = GetEpisodesFromTitle(title);
            var wolfmaxCategory = ParseCategory(cardElement);

            var releaseInfo = new ReleaseInfo
            {
                Title = title,
                Link = link,
                Details = link,
                Guid = link,
                Category = MapTrackerCatToNewznab(wolfmaxCategory),
                PublishDate = DateTime.Now,
                Size = EstimatedSizeByCategory[wolfmaxCategory] * Math.Max(episodes.Count, 1),
                Seeders = 1,
                Peers = 2,
                DownloadVolumeFactor = 0,
                UploadVolumeFactor = 1
            };

            // Filter by category
            if (query.Categories.Any() && !query.Categories.Intersect(releaseInfo.Category).Any())
            {
                return null;
            }

            // Filter by Season
            if (query.Season != 0 && !releaseInfo.Title.Contains("S" + query.Season.ToString("D2")))
            {
                return null;
            }

            // Filter by Episode
            if (int.TryParse(query.Episode, out var episode) && episodes.Any() && !episodes.Contains(episode))
            {
                return null;
            }

            return releaseInfo;
        }

        private string ParseTitle(IElement cardElement)
        {
            var title = cardElement.QuerySelector(".title")?.Text();
            title = Regex.Replace(title, @"(\- )?Temp\.\s+?\d+?", "").Trim();
            var seasonEpisode = ParseSeasonAndEpisode(cardElement);
            if (seasonEpisode.IsNotNullOrWhiteSpace())
            {
                title += " " + seasonEpisode;
            }

            return title;
        }

        private string ParseCategory(IElement cardElement)
        {
            // If the url contains "/serie" or contains "/temporada-" & "/capitulo-" it's a tv show
            // If not it's a movie
            var link = cardElement.GetAttribute("href");
            var quality = cardElement.QuerySelector(".quality")?.Text();
            var isTvShow = link.Contains("/serie") || (link.Contains("/temporada-") && link.Contains("/capitulo-"));

            string wolfmaxCat;
            if (isTvShow)
            {
                if (quality.Contains("720"))
                {
                    wolfmaxCat = Wolfmax4KCatType.Serie720;
                }
                else if (quality.Contains("1080"))
                {
                    wolfmaxCat = Wolfmax4KCatType.Serie1080;
                }
                else if (quality.ToLower().Contains("4k"))
                {
                    wolfmaxCat = Wolfmax4KCatType.Serie4K;
                }
                else
                {
                    wolfmaxCat = Wolfmax4KCatType.Serie;
                }
            }
            else
            {
                if (quality.Contains("720"))
                {
                    wolfmaxCat = Wolfmax4KCatType.Pelicula720;
                }
                else if (quality.Contains("1080"))
                {
                    wolfmaxCat = Wolfmax4KCatType.Pelicula1080;
                }
                else if (quality.ToLower().Contains("4k"))
                {
                    wolfmaxCat = Wolfmax4KCatType.Pelicula4K;
                }
                else
                {
                    wolfmaxCat = Wolfmax4KCatType.Pelicula;
                }
            }

            return wolfmaxCat;
        }

        private string ParseSeasonAndEpisode(IElement cardElement)
        {
            var link = cardElement.GetAttribute("href");
            var result = "";

            var matchSeason = new Regex(@"/temporada-(\d+)").Match(link);
            if (matchSeason.Success)
            {
                result += "S" + matchSeason.Groups[1].Value.PadLeft(2, '0');
            }

            var matchEpisode = new Regex(@"/capitulo-(\d+)(-al-(\d+))?/").Match(link);
            if (matchEpisode.Success)
            {
                result += "E" + matchEpisode.Groups[1].Value.PadLeft(2, '0');
                if (matchEpisode.Groups[3].Value.IsNotNullOrWhiteSpace())
                {
                    result += "-E" + matchEpisode.Groups[3].Value.PadLeft(2, '0');
                }
            }

            return result;
        }

        private List<int> GetEpisodesFromTitle(string title)
        {
            var vals = new Regex(@"E(\d+)").Matches(title).Cast<Match>().Select(m => int.Parse(m.Groups[1].Value)).ToList();

            if (vals.Count == 1)
            {
                return new List<int> { vals[0] };
            }

            if (vals.Count == 2)
            {
                return Enumerable.Range(vals[0], vals[1] - vals[0] + 1).ToList();
            }

            return new List<int>();
        }

        // Thanks to https://stackoverflow.com/a/5454692/2078070 !!!
        private string OpenSSLDecrypt(string encrypted, string passphrase)
        {
            // base 64 decode
            var encryptedBytesWithSalt = Convert.FromBase64String(encrypted);

            // extract salt (first 8 bytes of encrypted)
            var salt = new byte[8];
            var encryptedBytes = new byte[encryptedBytesWithSalt.Length - salt.Length - 8];
            Buffer.BlockCopy(encryptedBytesWithSalt, 8, salt, 0, salt.Length);
            Buffer.BlockCopy(encryptedBytesWithSalt, salt.Length + 8, encryptedBytes, 0, encryptedBytes.Length);

            // get key and iv
            DeriveKeyAndIV(passphrase, salt, out var key, out var iv);

            return DecryptStringFromBytesAes(encryptedBytes, key, iv);
        }

        private void DeriveKeyAndIV(string passphrase, byte[] salt, out byte[] key, out byte[] iv)
        {
            // generate key and iv
            var concatenatedHashes = new List<byte>(48);

            var password = Encoding.UTF8.GetBytes(passphrase);
            var currentHash = Array.Empty<byte>();
            var md5 = MD5.Create();
            var enoughBytesForKey = false;

            // See http://www.openssl.org/docs/crypto/EVP_BytesToKey.html#KEY_DERIVATION_ALGORITHM
            while (!enoughBytesForKey)
            {
                var preHashLength = currentHash.Length + password.Length + salt.Length;
                var preHash = new byte[preHashLength];

                Buffer.BlockCopy(currentHash, 0, preHash, 0, currentHash.Length);
                Buffer.BlockCopy(password, 0, preHash, currentHash.Length, password.Length);
                Buffer.BlockCopy(salt, 0, preHash, currentHash.Length + password.Length, salt.Length);

                currentHash = md5.ComputeHash(preHash);
                concatenatedHashes.AddRange(currentHash);

                if (concatenatedHashes.Count >= 48)
                {
                    enoughBytesForKey = true;
                }
            }

            key = new byte[32];
            iv = new byte[16];
            concatenatedHashes.CopyTo(0, key, 0, 32);
            concatenatedHashes.CopyTo(32, iv, 0, 16);

            md5.Clear();
            md5 = null;
        }

        private string DecryptStringFromBytesAes(byte[] cipherText, byte[] key, byte[] iv)
        {
            if (cipherText == null || cipherText.Length <= 0)
            {
                throw new ArgumentNullException(nameof(cipherText));
            }

            if (key == null || key.Length <= 0)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (iv == null || iv.Length <= 0)
            {
                throw new ArgumentNullException(nameof(iv));
            }

            // Declare the RijndaelManaged object
            // used to decrypt the data.
            RijndaelManaged aesAlg = null;

            // Declare the string used to hold
            // the decrypted text.
            string plaintext;

            try
            {
                // Create a RijndaelManaged object
                // with the specified key and IV.
                aesAlg = new RijndaelManaged { Mode = CipherMode.CBC, KeySize = 256, BlockSize = 128, Key = key, IV = iv };

                // Create a decrytor to perform the stream transform.
                var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                // Create the streams used for decryption.
                using (var msDecrypt = new MemoryStream(cipherText))
                {
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (var srDecrypt = new StreamReader(csDecrypt))
                        {
                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                            srDecrypt.Close();
                        }
                    }
                }
            }
            finally
            {
                // Clear the RijndaelManaged object.
                aesAlg?.Clear();
            }

            return plaintext;
        }
    }

    internal static class Wolfmax4KCatType
    {
        public static string Pelicula => "pelicula";
        public static string Pelicula720 => "pelicula720";
        public static string Pelicula1080 => "pelicula1080";
        public static string Pelicula4K => "pelicula4k";
        public static string Serie => "serie";
        public static string Serie720 => "serie720";
        public static string Serie1080 => "serie1080";
        public static string Serie4K => "serie4k";
    }

}
