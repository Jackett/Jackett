using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Jackett.Common.Extensions;
using Jackett.Common.Helpers;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class Wolfmax4K : IndexerBase
    {
        public override string Id => "wolfmax4k";
        public override string Name => "Wolfmax 4k";
        public override string Description => "Wolfmax 4k is a SPANISH Public site for MOVIES / TV";

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
            configData.AddDynamic("flaresolverr", new DisplayInfoConfigurationItem("FlareSolverr", "This site may use Cloudflare DDoS Protection, therefore Jackett requires <a href=\"https://github.com/Jackett/Jackett#configuring-flaresolverr\" target=\"_blank\">FlareSolverr</a> to access it."));
            // avoid Cloudflare too many requests limiter
            webclient.requestDelay = 2.1;
            webclient.EmulateBrowser = false;
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
            query = SanitizeTorznabQuery(query);

            // without search term the site returns the latest releases
            var searchUrl = query.SearchTerm.IsNullOrWhiteSpace()
                ? SiteLink + "ultimos"
                : SiteLink + "buscar?q=" + Uri.EscapeDataString(query.SearchTerm);

            var result = await RequestWithCookiesAndRetryAsync(searchUrl, referer: SiteLink);
            if (result.Status != HttpStatusCode.OK)
                throw new ExceptionWithConfigData(result.ContentString, configData);

            try
            {
                var parser = new HtmlParser();
                using var dom = parser.ParseDocument(result.ContentString);

                var items = new List<Wolfmax4KItem>();
                foreach (var card in dom.QuerySelectorAll("article.wolf-card"))
                    items.AddRange(await ParseCardAsync(card, query));

                return items.Select(item => ExtractReleaseInfo(item, query)).ToList()
                            .Where(x => x != null);
            }
            catch (Exception ex)
            {
                OnParseError(result.ContentString, ex);
            }

            return new List<ReleaseInfo>();
        }

        private async Task<IEnumerable<Wolfmax4KItem>> ParseCardAsync(IElement card, TorznabQuery query)
        {
            // <a class="wolf-card-main" href="/serie/86a2xh">Preacher - 1ª Temporada</a>
            var mainLink = card.QuerySelector("a.wolf-card-main");
            var detailsPath = mainLink?.GetAttribute("href");
            if (detailsPath.IsNullOrWhiteSpace())
                return Enumerable.Empty<Wolfmax4KItem>();

            var title = mainLink.TextContent.Trim().TrimEnd('.');
            var imagePath = card.QuerySelector(".wolf-card-poster img")?.GetAttribute("data-original");
            var image = imagePath.IsNotNullOrWhiteSpace() ? new Uri(new Uri(SiteLink), imagePath).AbsoluteUri : null;
            // <time datetime="2018-07-03">03/07/2018</time>
            var publishDate = ParseDate(card.QuerySelector(".wolf-card-date time")?.GetAttribute("datetime"), "yyyy-MM-dd");

            // the card of a tv show only has the last episode, all of them are in the details page
            if (!detailsPath.StartsWith("/pelicula/") && query.SearchTerm.IsNotNullOrWhiteSpace())
            {
                // each details page is one more request, skip the ones that will be filtered anyway
                var cardSeason = Regex.Match(title, @"(\d+)ª\s+Temporada");
                if (query.IsMovieSearch ||
                    (query.Season.HasValue && cardSeason.Success && int.Parse(cardSeason.Groups[1].Value) != query.Season))
                    return Enumerable.Empty<Wolfmax4KItem>();

                return await ParseEpisodesAsync(detailsPath, title, image);
            }

            // <li class="wolf-card-file">
            //   <a class="wolf-card-format" href="/serie/episodio/86a2xh"><strong>Episodio 1x10 -</strong><span>HDTV</span></a>
            //   <span class="wolf-card-size">614,84 MB</span>
            //   <button class="protected-download" data-content-code="pkghcn" data-tabla="series">
            // movies only have the quality: <strong>4K</strong>
            return card.QuerySelectorAll("li.wolf-card-file").Select(file =>
            {
                var format = file.QuerySelector(".wolf-card-format");
                var button = file.QuerySelector("button.protected-download");
                return new Wolfmax4KItem
                {
                    Title = title,
                    DetailsPath = format?.GetAttribute("href") ?? detailsPath,
                    EpisodeText = format?.QuerySelector("strong")?.TextContent,
                    Quality = (format?.QuerySelector("span") ?? format?.QuerySelector("strong"))?.TextContent.Trim(),
                    Size = file.QuerySelector(".wolf-card-size")?.TextContent,
                    PublishDate = publishDate,
                    Image = image,
                    ContentCode = button?.GetAttribute("data-content-code"),
                    Tabla = button?.GetAttribute("data-tabla")
                };
            }).ToList();
        }

        private async Task<IEnumerable<Wolfmax4KItem>> ParseEpisodesAsync(string detailsPath, string title, string image)
        {
            var result = await RequestWithCookiesAndRetryAsync(new Uri(new Uri(SiteLink), detailsPath).AbsoluteUri, referer: SiteLink);
            var parser = new HtmlParser();
            using var dom = parser.ParseDocument(result.ContentString);

            // <div class="wolf-episode">
            //   <a href="/serie/episodio/8fmmgy">1x01 -</a>
            //   <span class="wolf-episode-date">30/12/2016</span>
            //   <span class="wolf-episode-format">HDTV<span class="wolf-episode-size">731,27 MB</span></span>
            //   <button class="protected-download" data-content-code="pje4r3" data-tabla="series">
            return dom.QuerySelectorAll("div.wolf-episode").Select(episode =>
            {
                var link = episode.QuerySelector("a");
                var button = episode.QuerySelector("button.protected-download");
                return new Wolfmax4KItem
                {
                    Title = title,
                    DetailsPath = link?.GetAttribute("href") ?? detailsPath,
                    EpisodeText = link?.TextContent,
                    // the first child is the quality text, the second one the size
                    Quality = episode.QuerySelector(".wolf-episode-format")?.FirstChild?.TextContent.Trim(),
                    Size = episode.QuerySelector(".wolf-episode-size")?.TextContent,
                    PublishDate = ParseDate(episode.QuerySelector(".wolf-episode-date")?.TextContent, "dd/MM/yyyy"),
                    Image = image,
                    ContentCode = button?.GetAttribute("data-content-code"),
                    Tabla = button?.GetAttribute("data-tabla")
                };
            }).ToList();
        }

        public override async Task<byte[]> Download(Uri link)
        {
            var parameters = HttpUtility.ParseQueryString(link.Query);

            var generate = await DownloadApiRequestAsync(new
            {
                action = "generate",
                code = parameters["code"],
                tabla = parameters["tabla"]
            });
            var challenge = generate.Value<string>("challenge");

            var validate = await DownloadApiRequestAsync(new
            {
                action = "validate",
                challenge,
                nonce = ComputeProofOfWork(challenge)
            });

            // download_url is protocol relative, eg: //wolfmax4k.com/torrents/peliculas/xxx.torrent
            var torrentUrl = new Uri(new Uri(SiteLink), validate.Value<string>("download_url"));
            if (!validate.Value<bool>("external"))
            {
                var torrent = await RequestWithCookiesAndRetryAsync(torrentUrl.AbsoluteUri, referer: SiteLink);
                return torrent.ContentBytes;
            }

            // external links go through the link protector, eg: https://enlacito.com/s.php?i=xxx
            var enlacitoUrl = torrentUrl.AbsoluteUri;
            var enlacitoPage = await RequestWithCookiesAndRetryAsync(enlacitoUrl, referer: SiteLink);

            var enlacitoHtmlParser = new HtmlParser();
            var enlacitoDoc = await enlacitoHtmlParser.ParseDocumentAsync(enlacitoPage.ContentString);
            var enlacitoFormUrl = enlacitoDoc.QuerySelector("form").GetAttribute("action");
            var enlacitoFormLinkser = enlacitoDoc.QuerySelector("input[name=\"linkser\"]").GetAttribute("value");
            var enlacitoFormFlow = enlacitoDoc.QuerySelector("input[name=\"flow\"]").GetAttribute("value");

            var body = new Dictionary<string, string>
            {
                { "linkser", enlacitoFormLinkser },
                { "flow", enlacitoFormFlow }
            };
            var enlacito2Page = await RequestWithCookiesAndRetryAsync(enlacitoFormUrl, data: body, method: RequestType.POST);
            var regex = new Regex("var link_out = \"(.*)\"");
            var v = regex.Match(enlacito2Page.ContentString);

            var linkOut = v.Groups[1].ToString();
            var slink = Encoding.UTF8.GetString(Convert.FromBase64String(linkOut));
            var ulink = await OpenSSLDecryptAsync(slink, TorrentLinkEncryptionKey);

            var result = await RequestWithCookiesAndRetryAsync(ulink);
            return result.ContentBytes;
        }

        private async Task<JObject> DownloadApiRequestAsync(object body)
        {
            // no retries, each challenge can only be validated once
            var result = await RequestWithCookiesAsync(
                SiteLink + "api/descargas", method: RequestType.POST, referer: SiteLink,
                headers: new Dictionary<string, string> { { "Content-Type", "application/json" } },
                rawbody: JsonConvert.SerializeObject(body));

            var json = JObject.Parse(result.ContentString);
            if (json.Value<bool>("success"))
                return json;

            throw new Exception(json.Value<string>("status") switch
            {
                "limit_exceeded" => "Error, the download limit of the site has been reached, try again later.",
                "captcha_required" => "Error, the site is asking for a captcha, download the torrent from the site.",
                _ => $"Error, the download could not be generated: {json.Value<string>("error")}"
            });
        }

        private static int ComputeProofOfWork(string challenge)
        {
            // same as the site javascript: the sha256 of challenge + nonce must start with "000" in hex
            using var sha256 = SHA256.Create();
            for (var nonce = 0; ; nonce++)
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(challenge + nonce));
                if (hash[0] == 0 && hash[1] < 0x10)
                    return nonce;
            }
        }

        // Thanks to https://stackoverflow.com/a/5454692/2078070 !!!
        private async Task<string> OpenSSLDecryptAsync(string encrypted, string passphrase)
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

            return await DecryptStringFromBytesAesAsync(encryptedBytes, key, iv);
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

        private static async Task<string> DecryptStringFromBytesAesAsync(byte[] cipherText, byte[] key, byte[] iv)
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

            using var aesAlg = Aes.Create();
            aesAlg.Key = key;
            aesAlg.IV = iv;

            var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            using var msDecrypt = new MemoryStream(cipherText);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);

            return await srDecrypt.ReadToEndAsync();
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

        private ReleaseInfo ExtractReleaseInfo(Wolfmax4KItem item, TorznabQuery query)
        {
            // https://wolfmax4k.com/pelicula/5dw2j7
            // https://wolfmax4k.com/serie/episodio/8fmmgy
            // https://wolfmax4k.com/documental/episodio/48jcn5

            var torrentName = item.Title;
            var guid = item.DetailsPath;
            var quality = item.Quality;
            var image = item.Image;

            if (torrentName.IsNullOrWhiteSpace() || guid.IsNullOrWhiteSpace() || quality.IsNullOrWhiteSpace() ||
                item.ContentCode.IsNullOrWhiteSpace())
            {
                // Some torrents has no quality.
                // Ignored it because they are torrents that are not well categorized
                // as this game https://wolfmax4k.com/juego/james-cameronavatar/
                return null;
            }

            quality = ParseQuality(quality);
            var details = new Uri(new Uri(SiteLink), guid);
            // the torrent url is resolved by the download api of the site, see Download()
            var link = new Uri(new Uri(SiteLink), $"api/descargas?tabla={item.Tabla}&code={item.ContentCode}");
            var title = ParseTitle(torrentName, item.EpisodeText, quality);
            var episodes = GetEpisodesFromTitle(title);
            var wolfmaxCategory = ParseCategory(torrentName, guid, quality);

            var releaseInfo = new ReleaseInfo
            {
                Title = title,
                Link = link,
                Details = details,
                Guid = link,
                Category = MapTrackerCatToNewznab(wolfmaxCategory),
                PublishDate = item.PublishDate ?? DateTime.Now,
                Size = item.Size.IsNotNullOrWhiteSpace()
                    ? ParseUtil.GetBytes(item.Size)
                    : EstimatedSizeByCategory[wolfmaxCategory] * Math.Max(episodes.Count, 1),
                Seeders = 1,
                Peers = 2,
                DownloadVolumeFactor = 0,
                UploadVolumeFactor = 1
            };

            if (image.IsNotNullOrWhiteSpace() && !image.Contains("/no-imagen.jpg"))
                releaseInfo.Poster = new Uri(image);

            // Filter by category
            if (query.Categories.Any() && !query.Categories.Intersect(releaseInfo.Category).Any())
            {
                return null;
            }

            // Filter by Season
            if (query.Season.HasValue && !releaseInfo.Title.Contains("S" + query.Season.Value.ToString("D2")))
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

        private string ParseTitle(string torrentName, string episodeText, string quality)
        {
            var title = Regex.Replace(torrentName, @"(\- )?\(?\d+ª Temporada\)?", "");
            title = Regex.Replace(title, @"\[(Esp|Spanish)\]", "", RegexOptions.IgnoreCase);
            title = Regex.Replace(title, @"\(?wolfmax4k\.com\)?", "", RegexOptions.IgnoreCase);

            var seasonEpisode = ParseSeasonAndEpisode(episodeText);
            if (seasonEpisode.IsNotNullOrWhiteSpace())
            {
                // only replace Cap. if it could be parsed
                title = Regex.Replace(title, @"\[Cap\.(\s+)?(\d+)\]", "").Trim();
                title += " " + seasonEpisode;
            }

            // remove the "quality" from the torrentName and
            // adds it from the "quality" field of the api
            title = Regex.Replace(title, @"\s*\[(.*)(HDTV|Bluray|4k|DVDRIP|\d{3,4}p)(.*)\]", "",
                                  RegexOptions.IgnoreCase);

            title = title + " [" + quality + "] SPANISH";

            return title.Trim();
        }

        private string ParseCategory(string torrentName, string guid, string quality)
        {
            // If the url contains "/serie" or "/episodio/"
            // or contains "Cap." in the torrentName it's a tv show
            // If not it's a movie
            var isTvShow = guid.Contains("/serie") || guid.Contains("/episodio/") ||
                           Regex.IsMatch(torrentName, @"Cap\.(\s+)?(\d+)", RegexOptions.IgnoreCase);

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
                else if (quality.ToLower().Contains("4k") || quality.ToLower().Contains("2160p"))
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
                else if (quality.ToLower().Contains("4k") || quality.ToLower().Contains("2160p"))
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

        private string ParseQuality(string quality)
        {
            return quality switch
            {
                "4K" => "2160p",
                "4KWebrip" => "WEBRip-2160p",
                _ => quality
            };
        }

        private string ParseSeasonAndEpisode(string episodeText)
        {
            // Episodio 1x10 - / 2x01 al 06. / 4x07
            var match = Regex.Match(episodeText ?? "", @"(\d+)x(\d+)(\s*al\s*(\d+))?", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return "";
            }

            var result = "S" + match.Groups[1].Value.PadLeft(2, '0') + "E" + match.Groups[2].Value.PadLeft(2, '0');
            if (match.Groups[4].Success)
            {
                result += "-E" + match.Groups[4].Value.PadLeft(2, '0');
            }

            return result;
        }

        private static DateTime? ParseDate(string date, string format) =>
            DateTime.TryParseExact(date?.Trim(), format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
                ? parsed
                : null;

        private List<int> GetEpisodesFromTitle(string title)
        {
            var vals = new Regex(@"E(\d+)").Matches(title).Cast<Match>().Select(m => int.Parse(m.Groups[1].Value)).ToList();

            if (vals.Count == 1)
            {
                return new List<int> { vals[0] };
            }

            if (vals.Count == 2 && vals[1] > vals[0])
            {
                return Enumerable.Range(vals[0], vals[1] - vals[0] + 1).ToList();
            }

            return new List<int>();
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

    internal sealed record Wolfmax4KItem
    {
        public string Title { get; set; }
        public string DetailsPath { get; set; }
        public string EpisodeText { get; set; }
        public string Quality { get; set; }
        public string Size { get; set; }
        public DateTime? PublishDate { get; set; }
        public string Image { get; set; }
        public string ContentCode { get; set; }
        public string Tabla { get; set; }
    }
}
