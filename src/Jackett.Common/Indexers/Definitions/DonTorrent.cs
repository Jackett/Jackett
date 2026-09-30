using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
    public class DonTorrent : IndexerBase
    {
        public override string Id => "dontorrent";
        public override string[] Replaces => new[] { "todotorrents" };
        public override string Name => "DonTorrent";
        public override string Description => "DonTorrent is a SPANISH Public tracker for MOVIES / TV / GENERAL";
        // in the event the redirect is inactive https://t.me/s/dontorrent should have the latest working domain
        public override string SiteLink { get; protected set; } = "https://dontorrent.moi/";
        public override string[] AlternativeSiteLinks => new[]
        {
            "https://dontorrent.moi/",
        };
        public override string[] LegacySiteLinks => new[]
        {
            "https://todotorrents.org/",
            "https://tomadivx.net/",
            "https://seriesblanco.one/",
            "https://dontorrent.ch/", // parking page with JavaScript redirect
            "https://dontorrent.haus/",
            "https://dontorrent.news/",
            "https://dontorrent.institute/",
            "https://dontorrent.jetzt/",
            "https://dontorrent.loan/",
            "https://dontorrent.graphics/",
            "https://dontorrent.international/",
            "https://dontorrent.irish/",
            "https://dontorrent.lighting/",
            "https://dontorrent.istanbul/",
            "https://dontorrent.onl/",
            "https://dontorrent.kids/",
            "https://dontorrent.kiwi/",
            "https://dontorrent.live/",
            "https://dontorrent.phd/",
            "https://dontorrent.gripe/", // no longer compatible, switched to JS download
            "https://dontorrent.promo/", // no longer compatible, switched to JS download
            "https://dontorrent.gift/", // no longer compatible, switched to JS download
            "https://dontorrent.cfd/", // no longer compatible, switched to JS download
            "https://verdetorrent.com/", // redirects to https://privtr.ee/@DonTorrent
            "https://naranjatorrent.com/", // redirects to https://privtr.ee/@DonTorrent
        };
        public override string Language => "es-ES";
        public override string Type => "public";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private static class DonTorrentCatType
        {
            public static string Pelicula => "pelicula";
            public static string Serie => "serie";
            public static string Documental => "documental";
        }

        private const string NewTorrentsUrl = "ultimos";
        private const string SearchUrl = "buscar/";

        private static Dictionary<string, string> CategoriesMap => new Dictionary<string, string>
            {
                { "/pelicula/", DonTorrentCatType.Pelicula },
                { "/serie/", DonTorrentCatType.Serie },
                { "/documental/", DonTorrentCatType.Documental },
            };

        public DonTorrent(IIndexerConfigurationService configService, WebClient w, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(configService: configService,
                   client: w,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationData())
        {
            // avoid CLoudflare too many requests limiter
            webclient.requestDelay = 2.1;

            var matchWords = new BoolConfigurationItem("Match words in title") { Value = true };
            configData.AddDynamic("MatchWords", matchWords);
        }

        private TorznabCapabilities SetCapabilities()
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
                },

                // Raw search shows better results
                SupportsRawSearch = true
            };

            caps.Categories.AddCategoryMapping(DonTorrentCatType.Pelicula, TorznabCatType.Movies, "Movies");
            caps.Categories.AddCategoryMapping(DonTorrentCatType.Serie, TorznabCatType.TV, "TV");
            caps.Categories.AddCategoryMapping(DonTorrentCatType.Documental, TorznabCatType.TVDocumentary, "TV/Documentary");

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
            var matchWords = ((BoolConfigurationItem)configData.GetDynamic("MatchWords")).Value;
            matchWords = query.SearchTerm != "" && matchWords;

            // we remove parts from the original query
            query = ParseQuery(query);

            var releases = string.IsNullOrEmpty(query.SearchTerm) ?
                await PerformQueryNewestAsync(query) :
                await PerformQuerySearchAsync(query, matchWords);

            return releases;
        }

        public override async Task<byte[]> Download(Uri link)
        {
            var downloadLink = "";
            var cleanLink = link.ToString().TrimEnd('/');

            var lastSlash = cleanLink.LastIndexOf('/');
            if (lastSlash > 0)
            {
                var contentIdStr = cleanLink.Substring(lastSlash + 1);
                var aux = cleanLink.Substring(0, lastSlash);
                
                var secondLastSlash = aux.LastIndexOf('/');
                if (secondLastSlash > 0)
                {
                    var tabla = aux.Substring(secondLastSlash + 1);                    
                    var rawHref = aux.Substring(0, secondLastSlash);
                    if (!string.IsNullOrEmpty(contentIdStr) && !string.IsNullOrEmpty(tabla) && int.TryParse(contentIdStr, out var contentId))
                    {
                        // Protected downloads challenge solver
                        var protectedUrl = await GetProtectedDownloadUrlAsync(contentId, tabla);
                        if (!string.IsNullOrEmpty(protectedUrl))
                            downloadLink = protectedUrl;
                        else
                            downloadLink = rawHref.StartsWith("//") ? "https:" + rawHref : rawHref;

                    }
                }
            }

            return await base.Download(new Uri(downloadLink));
        }

        private async Task<List<ReleaseInfo>> PerformQueryNewestAsync(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            var url = SiteLink + NewTorrentsUrl;

            var result = await RequestWithCookiesAsync(url);

            if (result.Status != HttpStatusCode.OK)
            {
                throw new ExceptionWithConfigData(result.ContentString, configData);
            }

            try
            {
                var searchResultParser = new HtmlParser();
                using var doc = await searchResultParser.ParseDocumentAsync(result.ContentString);

                var rows = doc.QuerySelector("div.seccion#ultimos_torrents > div.card > div.card-body > div");

                var parsedDetailsLink = new List<string>();
                string rowTitle = null;
                string rowDetailsLink = null;
                string rowPublishDate = null;
                string rowQuality = null;

                foreach (var row in rows.Children)
                {
                    if (row.TagName.Equals("DIV"))
                    {
                        //div class="h5 text-dark">PELÍCULAS:</div>
                        continue;
                    }

                    //<span class="text-muted">2022-01-12</span>
                    //<a href='pelicula/24797/Halloween-Kills' class="text-primary">Halloween Kills</a>
                    //<span class="text-muted">(MicroHD-1080p)</span>

                    if (row.TagName.Equals("A"))
                    {
                        rowTitle = row.TextContent;
                        rowDetailsLink = SiteLink + row.GetAttribute("href");
                    }

                    if (row.TagName.Equals("SPAN"))
                    {
                        if (DateTime.TryParse(row.TextContent, out var publishDate))
                        {
                            rowPublishDate = publishDate.ToString();
                        }

                        //quality
                        if (Regex.IsMatch(row.TextContent, "([()])"))
                        {
                            rowQuality = row.TextContent;
                        }
                    }

                    if (row.TagName.Equals("BR"))
                    {
                        // we add parsed items to rowDetailsLink to avoid duplicates in the newest torrents list results
                        if (!parsedDetailsLink.Contains(rowDetailsLink) && rowTitle != null)
                        {
                            var cat = GetCategoryFromURL(rowDetailsLink);

                            switch (cat)
                            {
                                case "pelicula":
                                case "serie":
                                case "documental":
                                    await ParseReleaseAsync(releases, rowDetailsLink, rowTitle, cat, rowQuality, query, false);
                                    parsedDetailsLink.Add(rowDetailsLink);
                                    break;
                            }

                            // clean the current row
                            rowTitle = null;
                            rowDetailsLink = null;
                            rowPublishDate = null;
                            rowQuality = null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnParseError(result.ContentString, ex);
            }

            return releases;
        }

        private async Task<List<ReleaseInfo>> PerformQuerySearchAsync(TorznabQuery query, bool matchWords)
        {
            // Found release result
            var releases = new List<ReleaseInfo>();

            // Search params
            var searchTerm = query.SearchTerm;
            var url = SiteLink + SearchUrl;

            // Search results
            int page = 1;
            int totalResults = -1;
            int processedResults = 0;
            bool endOfSearch = false;

            if (searchTerm.Length >= 2)
            {
                while (!endOfSearch && (processedResults < totalResults || totalResults == -1))
                {
                    // Perform the search query using POST
                    var formData = new Dictionary<string, string>
                    {
                        { "valor", searchTerm},
                        { "Buscar", "Buscar"},
                        { "p", page.ToString() }
                    };

                    var result = await RequestWithCookiesAsync(url: SiteLink + "buscar", method: RequestType.POST, referer: SiteLink, data: formData);

                    if (result.Status != HttpStatusCode.OK)
                        throw new ExceptionWithConfigData(result.ContentString, configData);

                    try
                    {
                        var searchResultParser = new HtmlParser();
                        using var doc = await searchResultParser.ParseDocumentAsync(result.ContentString);

                        var rows = doc.QuerySelectorAll("div.seccion#buscador > div.card > div.card-body > p");

                        if (rows.Length == 0)
                        {
                            endOfSearch = true;

                        }
                        else if (page == 1 && rows.First().TextContent.Contains("Introduce alguna palabra para buscar con al menos 2 letras."))
                        {
                            endOfSearch = true;

                        }
                        else
                        {
                            if (page == 1)
                            {
                                var leadBTagElements = doc.QuerySelectorAll("div.seccion#buscador > div.card > div.card-body > p.lead > b");

                                if (leadBTagElements.Length < 2 || !int.TryParse(leadBTagElements[1].TextContent, out totalResults) || totalResults <= 0)
                                    endOfSearch = true;
                            }

                            if (!endOfSearch)
                            {
                                var validRows = rows.Skip(2).ToList();
                                if (validRows.Count > 0)
                                {
                                    foreach (var row in validRows)
                                    {
                                        processedResults++;

                                        //href=/pelicula/6981/Saga-Spiderman
                                        var anchor = row.QuerySelector("p > span > a");

                                        if (anchor != null)
                                        {
                                            var link = string.Format("{0}{1}", SiteLink.TrimEnd('/'), anchor.GetAttribute("href"));
                                            var title = anchor.TextContent;
                                            var cat = GetCategoryFromURL(link);
                                            var quality = row.QuerySelector("p > span > span").TextContent.Trim('(', ')').Replace('-', '.');

                                            await ParseReleaseAsync(releases, link, title, cat, quality, query, matchWords);
                                        }
                                    }
                                }
                                // Stop pagination if all items are processed or no more rows are returned
                                if (processedResults >= totalResults || validRows.Count == 0)
                                {
                                    endOfSearch = true;
                                }
                                else
                                {
                                    page++;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        OnParseError(result.ContentString, ex);
                    }
                }
            }

            return releases;
        }

        private async Task ParseReleaseAsync(ICollection<ReleaseInfo> releases, string link, string title, string category, string quality, TorznabQuery query, bool matchWords)
        {
            // Remove trailing dot if there's one.
            title = title.Trim();
            if (title.EndsWith("."))
                title = title.Remove(title.Length - 1).Trim();

            //There's no public publishDate
            //var publishDate = TryToParseDate(publishStr, DateTime.Now);

            // return results only for requested categories
            if (query.Categories.Any() || query.Categories.Contains(MapTrackerCatToNewznab(category).First()))
            {
                if (matchWords && CheckTitleMatchWords(query.SearchTerm, title))
                {
                    switch (category)
                    {
                        case "pelicula":
                            await ParseMovieReleaseAsync(releases, link, query, title, quality, category);
                            break;
                        case "documental":
                        case "serie":
                            await ParseSeriesReleaseAsync(releases, link, query, title, quality, category);
                            break;
                    }
                }
            }
        }

        private async Task ParseMovieReleaseAsync(ICollection<ReleaseInfo> releases, string link, TorznabQuery query, string title, string quality, string category)
        {
            var cleanReleaseTitle = CleanReleaseTitle(title);
            var tags = ProcessTags(title);
            var lang = ".SPANISH";
            var downloadLink = link;
            var size = 0L;

            var result = await RequestWithCookiesAsync(link);

            if (result.Status != HttpStatusCode.OK)
            {
                throw new ExceptionWithConfigData(result.ContentString, configData);
            }

            var searchResultParser = new HtmlParser();
            using var doc = await searchResultParser.ParseDocumentAsync(result.ContentString);

            var year = doc.QuerySelector("div.d-inline-block.ml-2 > p.m-1 > a")?.TextContent.Trim();

            // add the year
            if (year.IsNotNullOrWhiteSpace() && Regex.IsMatch(year!, @"^((?:19|20)\d{2})$"))
            {
                year = '.' + year;

            }
            else
            {
                year = "";

            }

            var info = doc.QuerySelectorAll("div.descargar > div.card > div.card-body")[0];

            var downloadBtn = info.QuerySelector("a.protected-download") ?? info.QuerySelector("div.text-center a");

            if (downloadBtn != null)
            {
                var contentIdStr = downloadBtn.GetAttribute("data-content-id");
                var tabla = downloadBtn.GetAttribute("data-tabla");

                if (!string.IsNullOrEmpty(contentIdStr) && !string.IsNullOrEmpty(tabla) && int.TryParse(contentIdStr, out var contentId))
                        downloadLink = link + '/' + tabla + '/' + contentIdStr;
                
            }

            var moreinfo = info.QuerySelectorAll("div.text-center > div.d-inline-block");

            // guess size
            if (moreinfo.Length == 2)
            {
                size = ParseUtil.GetBytes(moreinfo[1].QuerySelector("p").TextContent);
            }
            else
            {
                size = GuessSize(quality, category);
            }

            var release = GenerateRelease(cleanReleaseTitle + year + '.' + quality + tags + lang, link, downloadLink, category, DateTime.Now, size);

            releases.Add(release);
        }

        private async Task ParseSeriesReleaseAsync(ICollection<ReleaseInfo> releases, string link, TorznabQuery query, string title, string quality, string category)
        {
            var cleanReleaseTitle = CleanReleaseTitle(title);
            var tags = ProcessTags(title);
            var lang = ".SPANISH";
            var season = ParseSeriesSeason(title);

            var result = await RequestWithCookiesAsync(link);
            if (result.Status != HttpStatusCode.OK)
                throw new ExceptionWithConfigData(result.ContentString, configData);

            var searchResultParser = new HtmlParser();
            using var doc = await searchResultParser.ParseDocumentAsync(result.ContentString);

            var data = doc.QuerySelectorAll("div.descargar > div.card > div.card-body > div.d-inline-block > table.table > tbody > tr");

            foreach (var row in data)
            {
                var info = row.QuerySelectorAll("td")[0].TextContent.Trim();
                var episodeNumber = ParseSeriesEpisodeNumber(info);
                var episodeTitle = ParseSeriesEpisodeTitle(info);
                var episodePublishDate = DateTime.TryParseExact(
                    row.QuerySelectorAll("td")[2].TextContent.Trim(),
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedDate) ? parsedDate : DateTime.Now;
                var size = GuessSize(quality, category);
                size *= GetEpisodeCountFromTitle(episodeNumber);

                var downloadLink = "";
                var downloadBtn = row.QuerySelector("td > a.protected-download");

                if (downloadBtn != null)
                {
                    var contentIdStr = downloadBtn.GetAttribute("data-content-id");
                    var tabla = downloadBtn.GetAttribute("data-tabla");
                    var contentId = -1;

                    if (!string.IsNullOrEmpty(contentIdStr) && !string.IsNullOrEmpty(tabla) && int.TryParse(contentIdStr, out contentId))
                        downloadLink = link + '/' + tabla + '/' + contentIdStr;
                }

                // if the original query was in scene format, we filter the results to match episode
                // query.Episode != null means scene title
                if (query.Episode != null && !episodeTitle.Contains(query.GetEpisodeSearchString()))
                    continue;

                var release = new ReleaseInfo { };

                if (episodeTitle.IsNullOrWhiteSpace())
                {
                    release = GenerateRelease(cleanReleaseTitle + '.' + season + episodeNumber + '.' + quality + tags + lang, link, downloadLink, category, episodePublishDate, size);
                }
                else
                {
                    release = GenerateRelease(cleanReleaseTitle + '.' + season + episodeNumber + '.' + episodeTitle + '.' + quality + tags + lang, link, downloadLink, category, episodePublishDate, size);
                }

                releases.Add(release);
            }
        }

        private static string ParseSeriesSeason(string title)
        {
            var result = "";

            var seasonPattern = @"^(.*?)\s*-\s*(\d+)[ªº]?\s+Temporada";
            var miniseriePattern = @"^(.*?)\s*-\s*Miniserie";

            var match = Regex.Match(title, seasonPattern);

            if (match.Success)
            {
                int seasonNumber = int.Parse(match.Groups[2].Value);
                result = $"S{seasonNumber:D2}";

            }
            else if (Regex.Match(title, miniseriePattern).Success)
            {
                result = "S01";

            }

            return result;
        }

        private static string ParseSeriesEpisodeNumber(string episodeTitle)
        {
            var result = "";

            var pattern = @"\d+x(\d+)(?:\s*(?:-|al)\s*(?:\d+x)?(\d+))?";
            var match = Regex.Match(episodeTitle, pattern);
            if (match.Success)
            {
                int epStart = int.Parse(match.Groups[1].Value);

                if (match.Groups[2].Success)
                {
                    int epEnd = int.Parse(match.Groups[2].Value);
                    result = $"E{epStart:D2}-E{epEnd:D2}";

                }
                else
                {
                    result = $"E{epStart:D2}";

                }
            }

            return result;
        }

        private static string ParseSeriesEpisodeTitle(string episodeTitle)
        {
            var result = "";

            string pattern = @"\d+x(\d+)(?:\s*(?:-|al)\s*(?:\d+x)?(\d+))?";
            var match = Regex.Match(episodeTitle, pattern);

            if (match.Success)
            {
                int endIndex = match.Index + match.Length;
                if (endIndex < episodeTitle.Length)
                {
                    string remain = episodeTitle.Substring(endIndex).Trim();

                    if (remain.StartsWith("-"))
                    {
                        string rawTitle = remain.Substring(1).Trim();

                        var titleName = rawTitle.TrimEnd('…', '.', ' ');

                        if (!string.IsNullOrEmpty(titleName))
                            result = titleName.Replace(" ", ".");

                    }
                }
            }

            return result;
        }

        private ReleaseInfo GenerateRelease(string title, string link, string downloadLink, string cat,
                                            DateTime publishDate, long size)
        {
            var dl = new Uri(downloadLink);
            var _link = new Uri(link);
            var release = new ReleaseInfo
            {
                Title = title,
                Details = _link,
                Link = dl,
                Guid = dl,
                Category = MapTrackerCatToNewznab(cat),
                PublishDate = publishDate,
                Size = size,
                Seeders = 1,
                Peers = 2,
                DownloadVolumeFactor = 0,
                UploadVolumeFactor = 1
            };
            return release;
        }

        private static bool CheckTitleMatchWords(string queryStr, string title)
        {
            // this code split the words, remove words with 2 letters or less, remove accents and lowercase
            var queryMatches = Regex.Matches(queryStr, @"\b[\w']*\b");
            var queryWords = from m in queryMatches.Cast<Match>()
                             where !string.IsNullOrEmpty(m.Value) && m.Value.Length > 2
                             select Encoding.UTF8.GetString(Encoding.GetEncoding("ISO-8859-8").GetBytes(m.Value.ToLower()));

            var titleMatches = Regex.Matches(title, @"\b[\w']*\b");
            var titleWords = from m in titleMatches.Cast<Match>()
                             where !string.IsNullOrEmpty(m.Value) && m.Value.Length > 2
                             select Encoding.UTF8.GetString(Encoding.GetEncoding("ISO-8859-8").GetBytes(m.Value.ToLower()));
            titleWords = titleWords.ToArray();

            return queryWords.All(word => titleWords.Contains(word));
        }

        private static TorznabQuery ParseQuery(TorznabQuery query)
        {
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


        public static int GetEpisodeCountFromTitle(string title)
        {
            var matches = Regex.Matches(title, "E[0-9+]");
            var count = matches.Count;
            if (count == 0)
                return 0; //no episodes in title

            //eg E1-E9
            if (count == 2)
            {
                var first = title.Substring(matches[0].Index, matches[1].Index - matches[0].Index - 1);
                var last = title.Substring(matches[1].Index, 3); //"Exx"
                if (first.StartsWith("E") && last.StartsWith("E"))
                {
                    var first_ep = int.Parse(first.Substring(1, 2));
                    var last_ep = int.Parse(last.Substring(1, 2));

                    return last_ep - first_ep + 1; //E01-E03 -> 3 episodes
                }
            }

            return count;
        }

        public static string GetCategoryFromURL(string url)
        {
            return CategoriesMap
                .Where(categoryMap => url.Contains(categoryMap.Key))
                .Select(categoryMap => categoryMap.Value)
                .FirstOrDefault();
        }


        private static string CleanReleaseTitle(string title)
        {
            var result = "";
            var seasonPattern = @"^(.*?)\s*-\s*(\d+)[ªº]?\s+Temporada";
            var miniseriePattern = @"^(.*?)\s*-\s*Miniserie";

            var match = Regex.Match(title, seasonPattern);
            if (match.Success)
                result = match.Groups[1].Value.Trim();


            match = Regex.Match(title, miniseriePattern);
            if (result == "" && match.Success)
                result = match.Groups[1].Value.Trim();


            if (result == "")
                result = title;

            var index = result.IndexOfAny(new char[] { '(', '[' });
            result = index >= 0 ? result.Substring(0, index) : result;

            result = Regex.Replace(result.Trim(), @"[\s:\-\._]+", ".");
            result = result.Trim('.');

            return result;
        }

        private static long GuessSize(string quality, string category)
        {
            var size = 0L;
            var qualityL = quality?.ToLowerInvariant() ?? string.Empty;

            switch (category?.ToLowerInvariant())
            {
                case "pelicula":
                    switch (qualityL)
                    {
                        case "fullbluray":
                            size = 48318382080L; // 45 GB
                            break;

                        case "4k":
                            size = 21474836480L; // 20 GB
                            break;

                        case "bdremux":
                        case "bdremux.1080p":
                            size = 21474836480L; // 20 GB
                            break;

                        case "bluray":
                        case "bluray.1080p":
                            size = 9663676416L;  // 9 GB
                            break;

                        case "microhd":
                        case "microhd.1080p":
                            size = 4831838208L;  // 4.5 GB
                            break;

                        case "bluray.720p":
                            size = 4294967296L;  // 4 GB
                            break;

                        case "microhd.720p":
                        case "hdrip":
                            size = 2147483648L;  // 2 GB
                            break;

                        case "dvdrip":
                        case "hdtv":
                            size = 1503238554L;  // 1.4 GB
                            break;

                        case "screener":
                        case "scr":
                            size = 1073741824L;  // 1 GB
                            break;

                        case "cam":
                        case "telesync":
                        case "ts":
                        case "tc":
                            size = 734003200L;   // 700 MB
                            break;

                        default:
                            size = 524288000L;   // 500 MB
                            break;
                    }
                    break;

                case "documental":
                case "serie":
                    switch (qualityL)
                    {
                        case "fullbluray":
                            size = 10737418240L; // 10 GB
                            break;

                        case "4k":
                            size = 10737418240L; // 10 GB
                            break;

                        case "bdremux":
                        case "bdremux.1080p":
                            size = 8589934592L;  // 8 GB
                            break;

                        case "bluray":
                        case "bluray.1080p":
                            size = 3221225472L;  // 3 GB
                            break;

                        case "microhd":
                        case "microhd.1080p":
                            size = 1610612736L;  // 1.5 GB
                            break;

                        case "bluray.720p":
                            size = 2147483648L;  // 2 GB
                            break;

                        case "microhd.720p":
                        case "hdtv.720p":
                        case "hdrip":
                            size = 1073741824L;  // 1 GB
                            break;

                        case "dvdrip":
                        case "hdtv":
                            size = 536870912L;   // 500 MB
                            break;

                        case "screener":
                        case "scr":
                            size = 419430400L;   // 400 MB
                            break;

                        case "cam":
                        case "telesync":
                        case "ts":
                        case "tc":
                            size = 314572800L;   // 300 MB
                            break;

                        default:
                            size = 524288000L;   // 500 MB
                            break;
                    }
                    break;
            }

            return size;
        }

        private static string ProcessTags(string title)
        {
            var tags = "";
            var queryMatches = Regex.Matches(title, @"[\[\(]([^\]\)]+)[\]\)]", RegexOptions.IgnoreCase);

            foreach (Match m in queryMatches)
            {
                var tag = m.Groups[1].Value.Trim().ToUpper();

                if (tag.ToLower().Equals("v. extendida"))
                    tags += ".Directors.Cut";
                else if (tag.ToLower().Equals("subs. integrados"))
                    tags = ".HC";
                else
                    tags += "." + tag;
            }

            return tags;
        }

        private async Task<string> GetProtectedDownloadUrlAsync(int contentId, string contentType)
        {
            var apiUrl = SiteLink.TrimEnd('/') + "/api_validate_pow.php";
            var headers = new Dictionary<string, string> { { "Content-Type", "application/json; charset=utf-8" } };

            var generateData = new Dictionary<string, string>
            {
                { "action", "generate" },
                { "content_id", contentId.ToString() },
                { "tabla", contentType }
            };
            var generateJson = JsonConvert.SerializeObject(generateData);

            var genResponse = await RequestWithCookiesAsync(apiUrl, method: RequestType.POST, rawbody: generateJson);
            if (genResponse.Status != HttpStatusCode.OK)
                return null;

            var genResult = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(genResponse.ContentString);
            if (genResult?["success"]?.Value<bool>() != true)
                return null;

            string challenge = genResult["challenge"]?.ToString();
            if (string.IsNullOrEmpty(challenge))
                return null;

            long nonce = ComputeProofOfWork(challenge, difficulty: 3);

            var validateData = new Dictionary<string, string>
            {
                { "action", "validate" },
                { "challenge", challenge },
                { "nonce", nonce.ToString() }
            };

            generateJson = JsonConvert.SerializeObject(validateData);
            var valResponse = await RequestWithCookiesAsync(apiUrl, method: RequestType.POST, rawbody: generateJson);
            if (valResponse.Status != HttpStatusCode.OK)
                return null;

            var valResult = JsonConvert.DeserializeObject<JObject>(valResponse.ContentString);
            if (valResult?["success"]?.Value<bool>() != true)
                return null;

            var relativeUrl = valResult["download_url"]?.ToString();
            if (string.IsNullOrEmpty(relativeUrl))
                return null;

            relativeUrl = relativeUrl.Replace("\\/", "/");
            if (relativeUrl.StartsWith("//"))
            {
                relativeUrl = "https:" + relativeUrl;
            }

            return relativeUrl;
        }

        private long ComputeProofOfWork(string challenge, int difficulty = 3)
        {
            long nonce = 0;
            string target = new string('0', difficulty);

            using (var sha256 = SHA256.Create())
            {
                while (true)
                {
                    string text = challenge + nonce;
                    byte[] bytes = Encoding.UTF8.GetBytes(text);
                    byte[] hashBytes = sha256.ComputeHash(bytes);

                    StringBuilder sb = new StringBuilder();
                    foreach (byte b in hashBytes)
                    {
                        sb.Append(b.ToString("x2"));
                    }
                    string hashHex = sb.ToString();

                    if (hashHex.StartsWith(target))
                    {
                        return nonce;
                    }

                    nonce++;
                }
            }
        }
    }
}
