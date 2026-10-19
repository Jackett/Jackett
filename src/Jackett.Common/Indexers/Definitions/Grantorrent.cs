using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
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
using Newtonsoft.Json.Linq;
using NLog;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class Grantorrent : IndexerBase
    {
        public override string Id => "grantorrent";
        public override string Name => "Grantorrent";
        public override string Description => "Grantorrent is a SPANISH Public site for MOVIES / TV";

        public override string SiteLink { get; protected set; } = "https://grantorrent.foo/";
        public override string Language => "es-ES";
        public override string Type => "public";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        // The data-src attribute of a.linktorrent decodes to a URL whose host is dead
        // (e.g. dl.grantorrent.wf, NXDOMAIN). The functional torrent URL is always
        // https://grantorrent.foo/files + the path of the decoded URL (A1 facts #18-#23).
        private const string FilesHost = "grantorrent.foo";
        private const string FilesBase = "https://grantorrent.foo/files";

        public Grantorrent(IIndexerConfigurationService configService, WebClient w, Logger l, IProtectionService ps,
                           ICacheService cs)
            : base(configService: configService,
                   client: w,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationData())
        {
            webclient.requestDelay = 0.5;
            webclient.EmulateBrowser = true;
        }

        private static TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q
                },
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q
                }
            };
            // the 18 sidebar categories of the site are genres, not Torznab categories (A1 fact #14),
            // so the mappings are based on the quality of the release (A1 §7.4)
            caps.Categories.AddCategoryMapping(GrantorrentCatType.Pelicula, TorznabCatType.Movies, "Peliculas");
            caps.Categories.AddCategoryMapping(GrantorrentCatType.PeliculaSD, TorznabCatType.MoviesSD, "Peliculas SD");
            caps.Categories.AddCategoryMapping(GrantorrentCatType.PeliculaHD, TorznabCatType.MoviesHD, "Peliculas HD");
            caps.Categories.AddCategoryMapping(GrantorrentCatType.PeliculaUHD, TorznabCatType.MoviesUHD, "Peliculas UHD");

            caps.Categories.AddCategoryMapping(GrantorrentCatType.Serie, TorznabCatType.TV, "Series");
            caps.Categories.AddCategoryMapping(GrantorrentCatType.SerieSD, TorznabCatType.TVSD, "Series SD");
            caps.Categories.AddCategoryMapping(GrantorrentCatType.SerieHD, TorznabCatType.TVHD, "Series HD");
            caps.Categories.AddCategoryMapping(GrantorrentCatType.SerieUHD, TorznabCatType.TVUHD, "Series UHD");

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

            // without a search term the site returns the latest releases (home page)
            var searchUrl = query.SearchTerm.IsNullOrWhiteSpace()
                ? SiteLink
                : SiteLink + "?s=" + Uri.EscapeDataString(query.SearchTerm);

            var result = await RequestWithCookiesAndRetryAsync(searchUrl, referer: SiteLink);
            if (result.Status != HttpStatusCode.OK)
                throw new ExceptionWithConfigData(result.ContentString, configData);

            try
            {
                var parser = new HtmlParser();
                using var dom = parser.ParseDocument(result.ContentString);

                var releases = new List<ReleaseInfo>();
                foreach (var card in dom.QuerySelectorAll("div[x-data*='showDetail']"))
                {
                    var cardInfo = ParseCard(card);
                    if (cardInfo == null)
                        continue;

                    // the search page only has the movie name; the quality/size/language
                    // are only available in the detail page options table, so the detail
                    // page is fetched per card and one release is emitted per download
                    // option (accepted Jackett pattern: MejorTorrent, DonTorrent, AniDUB).
                    var detailReleases = await ParseDetailReleases(cardInfo, query);
                    releases.AddRange(detailReleases);
                }

                return releases;
            }
            catch (Exception ex)
            {
                OnParseError(result.ContentString, ex);
            }

            return new List<ReleaseInfo>();
        }

        private CardInfo ParseCard(IElement card)
        {
            // <a href="https://grantorrent.foo/<slug>/" x-on:mouseover="showDetail = false" ...>
            // (the colon of the attribute name must be escaped in the CSS selector)
            var detailsPath = card.QuerySelector("a[x-on\\:mouseover]")?.GetAttribute("href");
            if (detailsPath.IsNullOrWhiteSpace())
                return null;

            var details = new Uri(new Uri(SiteLink), detailsPath);

            // <p class="text-sm font-medium ...">TITLE</p> (fallback: img[alt])
            var title = card.QuerySelector("p.text-sm.font-medium")?.TextContent.Trim();
            if (title.IsNullOrWhiteSpace())
                title = card.QuerySelector("img")?.GetAttribute("alt")?.Trim();
            if (title.IsNullOrWhiteSpace())
                return null;

            // <p class="text-xs font-medium ...">DD/MM/YYYY</p>
            var publishDate = ParseDate(card.QuerySelector("p.text-xs.font-medium")?.TextContent);

            // <span>QUALITY</span> in the top overlay (optional, absent in most cards);
            // it must be scoped to the overlay because older cards also contain
            // genre <span>s in the bottom section
            var quality = card.QuerySelector("div.top-0 span")?.TextContent.Trim();

            // <img class="w-full rounded-xl" src="..." alt="TITLE">
            var poster = card.QuerySelector("img")?.GetAttribute("src");

            return new CardInfo(title, details, publishDate, poster, quality);
        }

        /// <summary>
        /// Fetches the detail page of a card and emits one release per download option
        /// (row of the options table). Rows without a data-src attribute are skipped
        /// (they are not downloadable).
        /// </summary>
        private async Task<List<ReleaseInfo>> ParseDetailReleases(CardInfo card, TorznabQuery query)
        {
            var detailPage = await RequestWithCookiesAndRetryAsync(card.Details.AbsoluteUri, referer: SiteLink);
            if (detailPage.Status != HttpStatusCode.OK)
            {
                logger.Warn("Fetching {0} returned HTTP {1}", card.Details, (int)detailPage.Status);
                return new List<ReleaseInfo>();
            }

            var parser = new HtmlParser();
            using var dom = parser.ParseDocument(detailPage.ContentString);

            // the <h1> of the detail page has the movie title with the year
            // (e.g. "Buscando a Dory (2016)"). The year is REQUIRED in the release
            // title: Radarr's movie-title parser only matches titles that contain a
            // 4-digit year, so without it the release is rejected with
            // "Unable to parse release". Falls back to the year from the search
            // query when the heading has none.
            var year = ParseYearFromHeading(dom.QuerySelector("h1")?.TextContent);
            if (!year.HasValue && query.Year > 0)
                year = query.Year;

            var releases = new List<ReleaseInfo>();
            var rows = dom.QuerySelectorAll("tr[wire\\:key]");
            for (var i = 0; i < rows.Length; i++)
            {
                var release = ParseOptionRow(card, rows[i], i, year);
                if (release != null)
                    releases.Add(release);
            }

            return releases;
        }

        /// <summary>
        /// Parses a single row of the options table into a release. Returns null when the
        /// row has no data-src attribute (not downloadable) or too few cells.
        /// </summary>
        private ReleaseInfo ParseOptionRow(CardInfo card, IElement row, int optionIndex, int? year)
        {
            var downloadButton = row.QuerySelector("a.linktorrent");
            var dataSrc = downloadButton?.GetAttribute("data-src");
            if (dataSrc.IsNullOrWhiteSpace())
                return null; // skip rows without data-src (not downloadable)

            // <td> cells: Idioma | Formato | Calidad | Peso | Opcion 1
            var cells = row.QuerySelectorAll("td");
            if (cells.Length < 4)
                return null;

            var language = ParseLanguage(cells[0]);
            var formato = cells[1].TextContent.Trim();
            var calidad = cells[2].TextContent.Trim();
            var peso = cells[3].TextContent.Trim();

            var quality = MapQuality(GetQuality(formato, calidad));
            var size = ParseSize(peso);
            if (size <= 0)
                size = EstimateSize(quality);

            // the title includes the year, the quality and the language so
            // Radarr/Sonarr can parse it (e.g. "Buscando a Dory (2016) DVDRip SPANISH").
            // The year is required (without it Radarr rejects the release with
            // "Unable to parse release") and the language token is required to pass
            // the language profile (otherwise Radarr falls back to the movie's
            // original language, e.g. English for "Finding Dory").
            var title = card.Title;
            if (year.HasValue && !HasYear(title))
                title += " (" + year.Value + ")";
            if (quality.IsNotNullOrWhiteSpace())
                title += " " + quality;
            if (language.IsNotNullOrWhiteSpace())
                title += " " + language;

            // the Link encodes the option index (?opt=N) so Download knows which row
            // to resolve (accepted pattern: DonTorrent encodes the option in the URL)
            var link = new Uri(card.Details.AbsoluteUri + "?opt=" + optionIndex);

            var release = new ReleaseInfo
            {
                Title = title,
                Details = card.Details,
                Link = link,
                Guid = link,
                Category = MapTrackerCatToNewznab(ParseCategory(card.Title, quality)),
                PublishDate = card.PublishDate ?? DateTime.Now,
                Size = size,
                Seeders = 1,
                Peers = 2,
                DownloadVolumeFactor = 0,
                UploadVolumeFactor = 1
            };

            // the poster is normally an absolute URL; a relative (or otherwise invalid) src
            // must not throw, or the UriFormatException would propagate to the catch in
            // PerformQuery and drop every release of the page (M2). A relative poster is
            // resolved against the site link (as the detail URL is).
            if (card.Poster.IsNotNullOrWhiteSpace())
            {
                if (Uri.TryCreate(card.Poster, UriKind.Absolute, out var posterUri))
                    release.Poster = posterUri;
                else if (Uri.TryCreate(new Uri(SiteLink), card.Poster, out var resolvedPoster))
                    release.Poster = resolvedPoster;
            }

            return release;
        }

        public override async Task<byte[]> Download(Uri link)
        {
            // the Link encodes the option index (?opt=N) so we know which row of the
            // options table to resolve (the user picked a specific release/option)
            var optionIndex = ParseOptionIndex(link);
            var detailUrl = StripQueryString(link);

            var detailPage = await RequestWithCookiesAndRetryAsync(detailUrl.AbsoluteUri, referer: SiteLink);
            if (detailPage.Status != HttpStatusCode.OK)
                throw new Exception($"Failed to load the detail page {detailUrl} (HTTP {(int)detailPage.Status})");

            var parser = new HtmlParser();
            using var dom = parser.ParseDocument(detailPage.ContentString);

            // find the row at the option index (rows are <tr wire:key=episode-N>)
            var rows = dom.QuerySelectorAll("tr[wire\\:key]");
            if (optionIndex < 0 || optionIndex >= rows.Length)
                throw new Exception($"Could not find the download option {optionIndex} in {detailUrl}");

            var row = rows[optionIndex];

            // <a href="https://super-enlace.com/..." data-src=<base64> class="linktorrent ...">
            var downloadButton = row.QuerySelector("a.linktorrent");
            var dataSrc = downloadButton?.GetAttribute("data-src");
            if (dataSrc.IsNullOrWhiteSpace())
                throw new Exception($"Could not find the download button (a.linktorrent with data-src) in {detailUrl} (option {optionIndex})");

            string decodedUrl;
            try
            {
                decodedUrl = Encoding.UTF8.GetString(Convert.FromBase64String(dataSrc));
            }
            catch (FormatException ex)
            {
                throw new Exception($"The data-src attribute of the download button in {detailUrl} is not valid base64", ex);
            }

            string torrentUrl;
            try
            {
                torrentUrl = ToFunctionalTorrentUrl(decodedUrl);
            }
            catch (UriFormatException ex)
            {
                throw new Exception($"The data-src attribute of the download button in {detailUrl} does not decode to a valid URL", ex);
            }

            var torrent = await RequestWithCookiesAndRetryAsync(torrentUrl, referer: SiteLink);
            if (torrent.Status == HttpStatusCode.OK)
                return torrent.ContentBytes;

            // the primary URL (data-src) did not respond: fall back to the third-party
            // shortener in the button href (super-enlace.com/s.php?i=<5x base64 + ROT13>),
            // which resolves to the functional torrent URL (A1 fact #20). The decoding is
            // wrapped in try/catch so a broken fallback never masks the primary error.
            string fallbackUrl = null;
            try
            {
                fallbackUrl = DecodeSuperEnlace(downloadButton.GetAttribute("href"));
            }
            catch
            {
                // a broken fallback must not mask the primary error
            }

            if (fallbackUrl == null)
                throw new Exception($"Failed to download the torrent {torrentUrl} (HTTP {(int)torrent.Status})");

            var fallback = await RequestWithCookiesAndRetryAsync(fallbackUrl, referer: SiteLink);
            if (fallback.Status == HttpStatusCode.OK)
                return fallback.ContentBytes;

            throw new Exception($"Failed to download the torrent: primary URL {torrentUrl} (HTTP {(int)torrent.Status}) " +
                                $"and super-enlace fallback {fallbackUrl} (HTTP {(int)fallback.Status})");
        }

        /// <summary>
        /// Card information extracted from the search page (before fetching the detail page).
        /// It is a plain class (not a C# 9 record) because Jackett.Common also targets
        /// netstandard2.0, which does not provide System.Runtime.CompilerServices.IsExternalInit.
        /// </summary>
        private sealed class CardInfo
        {
            public CardInfo(string title, Uri details, DateTime? publishDate, string poster, string overlayQuality)
            {
                Title = title;
                Details = details;
                PublishDate = publishDate;
                Poster = poster;
                OverlayQuality = overlayQuality;
            }

            public string Title { get; }
            public Uri Details { get; }
            public DateTime? PublishDate { get; }
            public string Poster { get; }
            public string OverlayQuality { get; }
        }

        /// <summary>
        /// Parses the option index from the Link query string (?opt=N). Defaults to 0 when
        /// the query string is absent or invalid (e.g. a plain detail URL).
        /// </summary>
        private static int ParseOptionIndex(Uri link)
        {
            var query = link.Query.TrimStart('?');
            foreach (var pair in query.Split('&'))
            {
                // Split('=', 2) is not available on netstandard2.0, so split on the first '=' manually.
                var eqIndex = pair.IndexOf('=');
                if (eqIndex > 0 && pair.Substring(0, eqIndex) == "opt"
                    && int.TryParse(pair.Substring(eqIndex + 1), out var index))
                    return index;
            }
            return 0;
        }

        /// <summary>
        /// Strips the query string from a URI (e.g. https://site/slug/?opt=2 -> https://site/slug/).
        /// </summary>
        private static Uri StripQueryString(Uri link)
        {
            var builder = new UriBuilder(link) { Query = "" };
            return builder.Uri;
        }

        /// <summary>
        /// Determines the quality of a row. The "Formato" column has the real quality
        /// (e.g. HDRip, MicroHD-1080p, BluRay-1080p, BDRemux-1080p, 3D); the "Calidad"
        /// column is usually empty. When "Formato" is a container (e.g. MKV) or empty,
        /// the "Calidad" column is used as a fallback.
        /// </summary>
        private static string GetQuality(string formato, string calidad)
        {
            formato = (formato ?? "").Trim();
            calidad = (calidad ?? "").Trim();

            if (formato.IsNotNullOrWhiteSpace() && LooksLikeQuality(formato))
                return formato;
            if (calidad.IsNotNullOrWhiteSpace())
                return calidad;
            return formato;
        }

        private static bool LooksLikeQuality(string value)
        {
            return Regex.IsMatch(value,
                @"(?i)(hdrip|dvdrip|bdrip|bluray|bdremux|microhd|webrip|hdtv|1080p|720p|2160p|4k|3d|cam|ts|tc|avc|x264|x265)");
        }

        /// <summary>
        /// Extracts the release year from the detail page heading. The heading may have
        /// the year as a 4-digit number (e.g. "Buscando a Dory (2016)") or as part of a
        /// date (e.g. "Buscando a Dory (06/22/16.)" -> 2016, or "06/22/2016" -> 2016).
        /// Returns null when the heading has no recognizable year.
        /// </summary>
        private static int? ParseYearFromHeading(string heading)
        {
            if (heading.IsNullOrWhiteSpace())
                return null;

            // a 4-digit year (e.g. "Buscando a Dory (2016)")
            var match = Regex.Match(heading, @"\b(19|20)\d{2}\b");
            if (match.Success)
                return int.Parse(match.Value);

            // a 2- or 4-digit year in a date context (e.g. "06/22/16." or "06/22/2016")
            match = Regex.Match(heading, @"\d{1,2}/\d{1,2}/(\d{2,4})");
            if (match.Success)
            {
                var yearStr = match.Groups[1].Value;
                if (yearStr.Length == 2)
                {
                    if (int.TryParse(yearStr, out var yy))
                        return 2000 + yy;
                }
                else if (int.TryParse(yearStr, out var yyyy))
                {
                    return yyyy;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns true when the title already contains a 4-digit year (so the year is
        /// not added twice).
        /// </summary>
        private static bool HasYear(string title)
        {
            return Regex.IsMatch(title ?? "", @"\b(19|20)\d{2}\b");
        }

        /// <summary>
        /// Parses the language from the "Idioma" cell (the img title/alt, e.g.
        /// "Español (Castellano)") and maps it to a Radarr-recognized language token
        /// (e.g. "SPANISH"). Returns null when the language is empty or not recognized.
        /// </summary>
        private static string ParseLanguage(IElement cell)
        {
            var img = cell.QuerySelector("img");
            var language = img?.GetAttribute("title") ?? img?.GetAttribute("alt") ?? cell.TextContent;
            language = (language ?? "").Trim();

            if (language.IsNullOrWhiteSpace())
                return null;

            if (Regex.IsMatch(language, @"(?i)(espa[ñn]ol|castellano|latino|spanish)"))
                return "SPANISH";
            if (Regex.IsMatch(language, @"(?i)(ingl[eé]s|english)"))
                return "ENGLISH";

            return null;
        }

        /// <summary>
        /// Maps the quality to a Radarr-recognized quality token. Radarr does not
        /// recognize "HDRip" (it is a DVD rip, so it is mapped to "DVDRip") nor "3D"
        /// (it is typically a 1080p 3D Blu-ray, so it is mapped to "BluRay-1080p 3D").
        /// Other qualities are kept as-is (e.g. "BluRay-1080p", "BDRemux-1080p").
        /// </summary>
        private static string MapQuality(string quality)
        {
            if (quality.IsNullOrWhiteSpace())
                return quality;

            var q = quality.Trim();

            if (Regex.IsMatch(q, @"^hdrip$", RegexOptions.IgnoreCase))
                return "DVDRip";
            if (Regex.IsMatch(q, @"^3d$", RegexOptions.IgnoreCase))
                return "BluRay-1080p 3D";

            return q;
        }

        /// <summary>
        /// Parses the "Peso" column (e.g. "1.73 Gb.", "19.4 Gb.", "1.80GB.") into bytes.
        /// Returns 0 when the value is empty or cannot be parsed.
        /// </summary>
        private static long ParseSize(string peso)
        {
            peso = (peso ?? "").Trim();
            if (peso.IsNullOrWhiteSpace())
                return 0;

            var match = Regex.Match(peso, @"([\d.]+)\s*(Gb|GB|MB|KB|B)\.?", RegexOptions.IgnoreCase);
            if (!match.Success)
                return 0;

            if (!double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                return 0;

            return match.Groups[2].Value.ToUpperInvariant() switch
            {
                "GB" => (long)(value * 1024 * 1024 * 1024),
                "MB" => (long)(value * 1024 * 1024),
                "KB" => (long)(value * 1024),
                "B" => (long)value,
                _ => 0
            };
        }

        /// <summary>
        /// The data-src attribute decodes to a URL whose host is dead (e.g. dl.grantorrent.wf, NXDOMAIN).
        /// The functional torrent URL is https://grantorrent.foo/files + the path of the decoded URL.
        /// Defensive transformation (A1 H1/H2): if the decoded URL already points to grantorrent.foo
        /// with a /files/ path it is used as-is, otherwise the path is rebuilt on the functional host,
        /// so a change of the dead host does not break the download.
        /// </summary>
        public static string ToFunctionalTorrentUrl(string decodedUrl)
        {
            var uri = new Uri(decodedUrl);
            if (uri.Host == FilesHost && uri.AbsolutePath.StartsWith("/files/", StringComparison.Ordinal))
                return uri.AbsoluteUri;

            return FilesBase + uri.AbsolutePath;
        }

        /// <summary>
        /// Decodes the href of the download button when it is the third-party shortener
        /// super-enlace.com (s.php?i=<...>&st=gtn): the i parameter is 5 layers of base64
        /// followed by ROT13 and resolves to the functional torrent URL (A1 fact #20).
        /// Returns null when the href is not a super-enlace link or the decoding fails
        /// (invalid base64, invalid final URL), so a broken fallback never masks the
        /// primary error.
        /// </summary>
        public static string DecodeSuperEnlace(string href)
        {
            if (href.IsNullOrWhiteSpace())
                return null;

            Uri shortener;
            try
            {
                shortener = new Uri(href);
            }
            catch (UriFormatException)
            {
                return null;
            }

            if (shortener.Host != "super-enlace.com")
                return null;

            // Split('=', 2) is not available on netstandard2.0, so split on the first '=' manually.
            string iParamValue = null;
            foreach (var pair in shortener.Query.TrimStart('?').Split('&'))
            {
                var eqIndex = pair.IndexOf('=');
                if (eqIndex > 0 && pair.Substring(0, eqIndex) == "i")
                {
                    iParamValue = pair.Substring(eqIndex + 1);
                    break;
                }
            }
            if (iParamValue == null)
                return null;

            // 5 layers of base64 followed by ROT13 (A1 fact #20). Any failure returns
            // null so a broken fallback never masks the primary error.
            try
            {
                var current = iParamValue;
                for (var layer = 0; layer < 5; layer++)
                    current = Encoding.UTF8.GetString(Convert.FromBase64String(current));

                var decoded = Rot13(current);
                return Uri.TryCreate(decoded, UriKind.Absolute, out var functionalUrl)
                    ? functionalUrl.AbsoluteUri
                    : null;
            }
            catch
            {
                return null;
            }
        }

        private static string Rot13(string input)
        {
            var result = new StringBuilder(input.Length);
            foreach (var c in input)
            {
                if (c >= 'a' && c <= 'z')
                    result.Append((char)('a' + (c - 'a' + 13) % 26));
                else if (c >= 'A' && c <= 'Z')
                    result.Append((char)('A' + (c - 'A' + 13) % 26));
                else
                    result.Append(c);
            }
            return result.ToString();
        }

        private static TorznabQuery SanitizeTorznabQuery(TorznabQuery query)
        {
            // the site does a plain WordPress search (?s=), so punctuation and
            // release-group style tokens are removed from the term (as in Wolfmax4K)
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
            searchTerm = Regex.Replace(searchTerm, @"\s{2,}", " ").Trim();

            query.SearchTerm = searchTerm;
            return query;
        }

        private static string ParseCategory(string title, string quality)
        {
            // the site does not expose a reliable content category in the URL (A1 H7):
            // the 18 sidebar categories are genres, not Torznab categories.
            // TV/movie heuristic: SxxEyy / "Cap." / "Temporada" in the title.
            var isTvShow = Regex.IsMatch(title, @"S\d{1,2}E\d{1,3}")
                           || Regex.IsMatch(title, @"Cap\.?\s*\d+", RegexOptions.IgnoreCase)
                           || title.IndexOf("Temporada", StringComparison.OrdinalIgnoreCase) >= 0;

            var q = (quality ?? "").ToLowerInvariant();
            var tier =
                q.Contains("2160") || q.Contains("4k") ? "uhd" :
                q.Contains("720") || q.Contains("1080") || q.Contains("bluray") ? "hd" :
                q.Contains("dvdrip") || q.Contains("hdrip") ? "sd" :
                "default";

            return (isTvShow, tier) switch
            {
                (true, "sd") => GrantorrentCatType.SerieSD,
                (true, "hd") => GrantorrentCatType.SerieHD,
                (true, "uhd") => GrantorrentCatType.SerieUHD,
                (true, _) => GrantorrentCatType.Serie,
                (false, "sd") => GrantorrentCatType.PeliculaSD,
                (false, "hd") => GrantorrentCatType.PeliculaHD,
                (false, "uhd") => GrantorrentCatType.PeliculaUHD,
                (false, _) => GrantorrentCatType.Pelicula
            };
        }

        private static long EstimateSize(string quality)
        {
            // the size is not present in the search results (A1 fact #11),
            // it is estimated by quality (A1 §7.5)
            var q = (quality ?? "").Trim().ToLowerInvariant();
            return q switch
            {
                "dvdrip" => 4.Gigabytes(),
                "hdrip" => 1.5.Gigabytes(),
                "720p" => 1.5.Gigabytes(),
                "1080p" => 2.5.Gigabytes(),
                "bluray-rip" => 2.5.Gigabytes(),
                "2160p" => 8.Gigabytes(),
                "4k" => 8.Gigabytes(),
                _ => 2.Gigabytes()
            };
        }

        private static DateTime? ParseDate(string date)
        {
            // DD/MM/YYYY, e.g. 28/02/2024 (A1 fact #9)
            return DateTime.TryParseExact(date?.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture,
                                           DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed)
                ? parsed
                : null;
        }
    }

    internal static class GrantorrentCatType
    {
        public static string Pelicula => "pelicula";
        public static string PeliculaSD => "pelicula_sd";
        public static string PeliculaHD => "pelicula_hd";
        public static string PeliculaUHD => "pelicula_uhd";
        public static string Serie => "serie";
        public static string SerieSD => "serie_sd";
        public static string SerieHD => "serie_hd";
        public static string SerieUHD => "serie_uhd";
    }
}
