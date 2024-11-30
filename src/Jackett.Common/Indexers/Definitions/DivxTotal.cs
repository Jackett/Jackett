using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Newtonsoft.Json.Linq;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class DivxTotal : IndexerBase
    {
        public override string Id => "divxtotal";
        public override string Name => "DivxTotal";
        public override string Description => "DivxTotal is a SPANISH site for Movies, TV series and Software";
        public override string SiteLink { get; protected set; } = "https://divxtotal.io/";
        public override string[] LegacySiteLinks => new[]
        {
            "https://www.divxtotal.nl/",
            "https://www.divxtotal.ac/",
            "https://www.divxtotal.dev/",
            "https://www.divxtotal.ms/",
            "https://www.divxtotal.fi/",
            "https://www.divxtotal.cat/",
            "https://www.divxtotal.pl/",
            "https://www.divxtotal.wf/",
            "https://www.divxtotal.win/",
            "https://www1.divxtotal.zip/",
            "https://www2.divxtotal.zip/",
            "https://www2.divxtotal.mov/",
            "https://www3.divxtotal.mov/",
            "https://www4.divxtotal.mov/",
            "https://www5.divxtotal.mov/",
        };
        public override string Language => "es-ES";
        public override string Type => "public";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private const string DownloadLink = "/download_tt.php";
        private const int MaxNrOfResults = 100;
        private const int MaxPageLoads = 3;

        private static class DivxTotalCategories
        {
            public static string Peliculas => "peliculas";
            public static string PeliculasHd => "peliculas-hd";
            public static string Peliculas3D => "peliculas-3-d";
            public static string PeliculasDvdr => "peliculas-dvdr";
            public static string Series => "series";
            public static string Programas => "programas";
            public static string Otros => "otros";
        }

        private static class DivxTotalFizeSizes
        {
            public static long Peliculas => 2147483648; // 2 GB
            public static long PeliculasDvdr => 5368709120; // 5 GB
            public static long Series => 536870912; // 512 MB
            public static long Otros => 536870912; // 512 MB
        }

        public DivxTotal(IIndexerConfigurationService configService, WebClient w, Logger l, IProtectionService ps,
                         ICacheService cs)
            : base(configService: configService,
                   client: w,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationData())
        {
            var matchWords = new BoolConfigurationItem("Match words in title") { Value = true };
            configData.AddDynamic("MatchWords", matchWords);

            configData.AddDynamic("flaresolverr", new DisplayInfoConfigurationItem("FlareSolverr", "This site may use Cloudflare DDoS Protection, therefore Jackett requires <a href=\"https://github.com/Jackett/Jackett#configuring-flaresolverr\" target=\"_blank\">FlareSolverr</a> to access it."));
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
                }
            };

            caps.Categories.AddCategoryMapping(DivxTotalCategories.Peliculas, TorznabCatType.MoviesSD, "Peliculas");
            caps.Categories.AddCategoryMapping(DivxTotalCategories.PeliculasHd, TorznabCatType.MoviesHD, "Peliculas HD");
            caps.Categories.AddCategoryMapping(DivxTotalCategories.Peliculas3D, TorznabCatType.Movies3D, "Peliculas 3D");
            caps.Categories.AddCategoryMapping(DivxTotalCategories.PeliculasDvdr, TorznabCatType.MoviesDVD, "Peliculas DVD-r");
            caps.Categories.AddCategoryMapping(DivxTotalCategories.Series, TorznabCatType.TVSD, "Series");
            caps.Categories.AddCategoryMapping(DivxTotalCategories.Programas, TorznabCatType.PC, "Programas");
            caps.Categories.AddCategoryMapping(DivxTotalCategories.Otros, TorznabCatType.OtherMisc, "Otros");

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
            var newQuery = query.Clone();

            var releases = new List<ReleaseInfo>();

            var matchWords = ((BoolConfigurationItem)configData.GetDynamic("MatchWords")).Value;
            matchWords = newQuery.SearchTerm != "" && matchWords;

            // we remove parts from the original query
            newQuery = ParseQuery(newQuery);
            var qc = new NameValueCollection { { "s", newQuery.SearchTerm } };

            var page = 1;
            IHtmlDocument htmlDocument = null;
            do
            {
                var url = SiteLink + "page/" + page + "/?" + qc.GetQueryString();

                string htmlString;
                try
                {
                    htmlString = await LoadWebPageAsync(url);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "DivxTotal: Failed to load url [{0}]: {1}", url, ex.Message);

                    return releases;
                }

                try
                {
                    htmlDocument = ParseHtmlIntoDocument(htmlString);

                    var table = htmlDocument.QuerySelector("table.table");
                    if (table == null)
                        break;

                    var rows = table.QuerySelectorAll("tbody > tr");
                    foreach (var row in rows)
                    {
                        try
                        {
                            var rels = await ParseReleasesAsync(row, newQuery, matchWords);
                            if (rels.Any())
                            {
                                releases.AddRange(rels);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Error($"CardigannIndexer ({Id}): Error while parsing row '{row.ToHtmlPretty()}':\n\n{ex}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    OnParseError(htmlString, ex);
                }

                page++;

            } while (page <= MaxPageLoads &&
                    releases.Count < MaxNrOfResults &&
                    !IsLastPageOfQueryResult(htmlDocument));

            return releases;
        }

        /// <Exception cref="ExceptionWithConfigData" />
        private async Task<string> LoadWebPageAsync(string url)
        {
            var result = await RequestWithCookiesAsync(url);
            return result.Status == HttpStatusCode.OK
                ? result.ContentString
                : throw new ExceptionWithConfigData(result.ContentString, configData);
        }

        private IHtmlDocument ParseHtmlIntoDocument(string htmlContentString)
            => new HtmlParser().ParseDocument(htmlContentString);

        private bool IsLastPageOfQueryResult(IHtmlDocument htmlDocument)
        {
            if (htmlDocument == null)
                return true;

            var nextPageAnchor = htmlDocument.QuerySelector("ul.pagination > li.active + li > a");
            return nextPageAnchor == null;
        }

        public override async Task<byte[]> Download(Uri link)
        {
            // for tv series we already have the link
            var downloadUrl = link.ToString();
            // for other categories we have to do another step
            if (!downloadUrl.Contains(DownloadLink))
            {
                var htmlString = await LoadWebPageAsync(downloadUrl);
                var htmlDocument = ParseHtmlIntoDocument(htmlString);
                downloadUrl = GetDownloadLink(htmlDocument);
            }
            var content = await base.Download(new Uri(downloadUrl));
            return content;
        }

        private async Task<List<ReleaseInfo>> ParseReleasesAsync(IParentNode row, TorznabQuery query, bool matchWords)
        {
            var releases = new List<ReleaseInfo>();

            var anchor = row.QuerySelector("a");
            var title = anchor.TextContent.Trim();

            // match the words in the query with the titles
            if (matchWords && !CheckTitleMatchWords(query.SearchTerm, title))
                return releases;

            var detailsStr = anchor.GetAttribute("href");
            var cat = detailsStr.Split('/')[3];

            // return results only for requested categories
            if (query.Categories.Any() && !MapTorznabCapsToTrackers(query).Contains(cat))
                return releases;

            var publishStr = row.QuerySelectorAll("td")[2].TextContent.Trim();
            var publishDate = TryToParseDate(publishStr, DateTime.Now);
            var sizeStr = row.QuerySelectorAll("td")[3].TextContent.Trim();

            // parsing is different for each category
            if (cat == DivxTotalCategories.Series)
            {
                var seriesReleases = await ParseSeriesReleaseAsync(query, detailsStr, cat, publishDate);
                releases.AddRange(seriesReleases);
            }
            else if (query.Episode == null) // if it's scene series, we don't return other categories
            {
                if (cat == DivxTotalCategories.Peliculas || cat == DivxTotalCategories.PeliculasHd ||
                    cat == DivxTotalCategories.Peliculas3D || cat == DivxTotalCategories.PeliculasDvdr)
                {
                    var movieRelease = ParseMovieRelease(query, title, detailsStr, cat, publishDate, sizeStr);
                    releases.Add(movieRelease);
                }
                else
                {
                    var size = TryToParseSize(sizeStr, DivxTotalFizeSizes.Otros);
                    var release = GenerateRelease(title, detailsStr, detailsStr, cat, publishDate, size);
                    releases.Add(release);
                }
            }

            return releases;
        }

        private async Task<List<ReleaseInfo>> ParseSeriesReleaseAsync(TorznabQuery query, string detailsStr, string cat, DateTime publishDate)
        {
            var seriesReleases = new List<ReleaseInfo>();

            var htmlString = await LoadWebPageAsync(detailsStr);
            var htmlDocument = ParseHtmlIntoDocument(htmlString);

            var tables = htmlDocument.QuerySelectorAll("table.rwd-table");
            foreach (var table in tables)
            {
                var rows = table.QuerySelectorAll("tbody > tr");
                foreach (var row in rows)
                {
                    var anchor = row.QuerySelector("a");
                    var episodeTitle = anchor.TextContent.Trim();

                    // Convert the title to Scene format
                    episodeTitle = ParseDivxTotalSeriesTitle(episodeTitle, query);

                    // if the original query was in scene format, we filter the results to match episode
                    // query.Episode != null means scene title
                    if (query.Episode != null && !episodeTitle.Contains(query.GetEpisodeSearchString()))
                        continue;

                    var downloadLink = GetDownloadLink(row);

                    seriesReleases.Add(GenerateRelease(episodeTitle, detailsStr, downloadLink, cat, publishDate, DivxTotalFizeSizes.Series));
                }
            }

            return seriesReleases;
        }

        private ReleaseInfo ParseMovieRelease(TorznabQuery query, string title, string detailsStr, string cat, DateTime publishDate, string sizeStr)
        {
            // parse tags in title, we need to put the year after the real title (before the tags)
            // La Maldicion ( HD-CAM)
            // Mascotas 2 4K (1080p) (DUAL)
            // Dragon Ball Super: Broly [2018] [DVD9] [PAL] [Español]
            var tags = "";
            var queryMatches = Regex.Matches(title, @"[\[\(]([^\]\)]+)[\]\)]", RegexOptions.IgnoreCase);
            foreach (Match m in queryMatches)
            {
                tags += " " + m.Groups[1].Value.Trim();
                title = title.Replace(m.Groups[0].Value, "");
            }
            title = title.Trim();
            title = Regex.Replace(title, " 4K$", "", RegexOptions.IgnoreCase);

            // add the year
            title = query.Year != null ? title + " " + query.Year : title;

            // add the tags
            title += tags.ToUpper();

            // add suffix and calculate the size
            var size = TryToParseSize(sizeStr, DivxTotalFizeSizes.Peliculas);
            if (cat == DivxTotalCategories.Peliculas)
                title += " SPANISH DVDRip XviD";
            else if (cat == DivxTotalCategories.PeliculasHd || cat == DivxTotalCategories.Peliculas3D)
                title += " SPANISH BDRip x264";
            else if (cat == DivxTotalCategories.PeliculasDvdr)
            {
                title += " SPANISH DVDR";
                size = TryToParseSize(sizeStr, DivxTotalFizeSizes.PeliculasDvdr);
            }
            else
                throw new Exception("Unknown category " + cat);

            var movieRelease = GenerateRelease(title, detailsStr, detailsStr, cat, publishDate, size);
            return movieRelease;
        }

        private ReleaseInfo GenerateRelease(string title, string detailsStr, string downloadLink, string cat, DateTime publishDate, long size)
        {
            var link = new Uri(downloadLink);
            var details = new Uri(detailsStr);
            var release = new ReleaseInfo
            {
                Title = title,
                Details = details,
                Link = link,
                Guid = link,
                Category = MapTrackerCatToNewznab(cat),
                PublishDate = publishDate,
                Size = size,
                Files = 1,
                Seeders = 1,
                Peers = 2,
                DownloadVolumeFactor = 0,
                UploadVolumeFactor = 1
            };
            return release;
        }

        private static string GetDownloadLink(IParentNode dom) =>
            dom.QuerySelector($"a[href*=\"{DownloadLink}\"]")?.GetAttribute("href");

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

        private static string ParseDivxTotalSeriesTitle(string episodeTitle, TorznabQuery query)
        {
            // episodeTitle = American Horror Story6x04
            var newTitle = episodeTitle;
            try
            {
                var r = new Regex("(([0-9]+)x([0-9]+)[^0-9]*)$", RegexOptions.IgnoreCase);
                var m = r.Match(newTitle);
                if (m.Success)
                {
                    var season = "S" + m.Groups[2].Value.PadLeft(2, '0');
                    var episode = "E" + m.Groups[3].Value.PadLeft(2, '0');
                    // if the original query was in scene format, we have to put the year back
                    // query.Episode != null means scene title
                    var year = query.Episode != null && query.Year != null ? " " + query.Year : "";
                    newTitle = newTitle.Replace(m.Groups[1].Value, year + " " + season + episode);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            newTitle += " SPANISH SDTV XviD";
            // return American Horror Story S06E04 SPANISH SDTV XviD
            return newTitle;
        }

        private static DateTime TryToParseDate(string dateToParse, DateTime dateDefault)
        {
            try
            {
                return DateTime.ParseExact(dateToParse, "dd-MM-yyyy", CultureInfo.InvariantCulture);
            }
            catch
            {
                return dateDefault;
            }
        }

        private static long TryToParseSize(string sizeToParse, long sizeDefault)
        {
            try
            {
                var parsedSize = ParseUtil.GetBytes(sizeToParse);
                return parsedSize > 0 ? parsedSize : sizeDefault;
            }
            catch
            {
                return sizeDefault;
            }
        }
    }
}
