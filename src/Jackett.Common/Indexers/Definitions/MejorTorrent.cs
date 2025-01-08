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
using AngleSharp.Html.Parser;
using Jackett.Common.Helpers;
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
    public class MejorTorrent : IndexerBase
    {
        public override string Id => "mejortorrent";
        public override string Name => "MejorTorrent";
        public override string Description => "MejorTorrent - Hay veces que un torrent viene mejor! :)";
        public override string SiteLink { get; protected set; } = "https://www27.mejortorrent.eu/";
        public override string[] LegacySiteLinks => new[]
        {
            "https://www12.mejortorrent.rip/",
            "https://www13.mejortorrent.rip/",
            "https://www14.mejortorrent.rip/",
            "https://www15.mejortorrent.rip/",
            "https://www16.mejortorrent.rip/",
            "https://www17.mejortorrent.zip/",
            "https://www18.mejortorrent.zip/",
            "https://www19.mejortorrent.zip/",
            "https://www20.mejortorrent.zip/",
            "https://www21.mejortorrent.zip/",
            "https://www22.mejortorrent.zip/",
             "https://www23.mejortorrent.zip/",
             "https://www24.mejortorrent.zip/",
             "https://www25.mejortorrent.zip/",
             "https://www26.mejortorrent.eu/",
        };
        public override string Language => "es-ES";
        public override string Type => "public";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private static class MejorTorrentCatType
        {
            public static string Pelicula => "Película";
            public static string Serie => "Serie";
            public static string SerieHd => "SerieHD"; // this category is created, doesn't exist in the site
            public static string Musica => "Música";
            public static string Otro => "Otro";
        }

        private const string NewTorrentsUrl = "torrents";
        private const string SearchUrl = "busqueda/page/";

        private const int PagesToSearch = 3;

        public MejorTorrent(IIndexerConfigurationService configService, WebClient w, Logger l, IProtectionService ps, ICacheService cs)
            : base(configService: configService,
                   client: w,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationData())
        {
            var matchWords = new BoolConfigurationItem("Match words in title") { Value = true };
            configData.AddDynamic("MatchWords", matchWords);

            // Uncomment to enable FlareSolverr in the future
            //configData.AddDynamic("flaresolverr", new DisplayInfoConfigurationItem("FlareSolverr", "This site may use Cloudflare DDoS Protection, therefore Jackett requires <a href=\"https://github.com/Jackett/Jackett#configuring-flaresolverr\" target=\"_blank\">FlareSolverr</a> to access it."));
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
                MusicSearchParams = new List<MusicSearchParam>
                {
                    MusicSearchParam.Q
                },
                SupportsRawSearch = true
            };

            caps.Categories.AddCategoryMapping(MejorTorrentCatType.Pelicula, TorznabCatType.Movies, "Pelicula");
            caps.Categories.AddCategoryMapping(MejorTorrentCatType.Serie, TorznabCatType.TVSD, "Serie");
            caps.Categories.AddCategoryMapping(MejorTorrentCatType.SerieHd, TorznabCatType.TVHD, "Serie HD");
            caps.Categories.AddCategoryMapping(MejorTorrentCatType.Musica, TorznabCatType.Audio, "Musica");
            // Other category is disabled because we have problems parsing documentaries
            //caps.Categories.AddCategoryMapping(MejorTorrentCatType.Otro, TorznabCatType.Other, "Otro");

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
                await PerformQueryNewest(query) :
                await PerformQuerySearch(query, matchWords);

            return releases;
        }

        public override async Task<byte[]> Download(Uri link)
        {
            var downloadUrl = link.ToString();
            var content = await base.Download(new Uri(downloadUrl));
            return content;
        }

        private async Task<List<ReleaseInfo>> PerformQueryNewest(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var url = SiteLink + NewTorrentsUrl;
            var result = await RequestWithCookiesAsync(url);
            if (result.Status != HttpStatusCode.OK)
            {
                if (result.Status == HttpStatusCode.InternalServerError)
                {
                    throw new ExceptionWithConfigData("HTTP 500 Internal Server Error", configData);
                }
                else
                {
                    throw new ExceptionWithConfigData(result.ContentString, configData);
                }
            }
            try
            {
                var searchResultParser = new HtmlParser();
                using var doc = searchResultParser.ParseDocument(result.ContentString);

                var container = doc.QuerySelector(".gap-y-3 > div:nth-child(1) > div:nth-child(1)");
                var parsedDetailsLink = new List<string>();
                string rowTitle = null;
                string rowDetailsLink = null;
                string rowPublishDate = null;
                string rowQuality = null;

                foreach (var row in container.Children)
                {
                    rowPublishDate = row.Children[0].TextContent;
                    rowQuality = row.Children[1].Children[0].Children[0].TextContent;
                    rowTitle = row.Children[1].Children[0].TextContent.Replace(rowQuality, String.Empty).Trim();
                    rowDetailsLink = row.Children[1].GetAttribute("href");
                    // we add parsed items to rowDetailsLink to avoid duplicates in newest torrents
                    // list results
                    if (!parsedDetailsLink.Contains(rowDetailsLink))
                    {
                        await ParseRelease(releases, rowTitle, rowDetailsLink, null,
                            rowPublishDate, rowQuality, query, false);
                        parsedDetailsLink.Add(rowDetailsLink);
                    }
                    // clean the current row
                    rowTitle = null;
                    rowDetailsLink = null;
                    rowPublishDate = null;
                    rowQuality = null;
                }
            }
            catch (Exception ex)
            {
                OnParseError(result.ContentString, ex);
                throw ex;
            }

            return releases;
        }

        private async Task<List<ReleaseInfo>> PerformQuerySearch(TorznabQuery query, bool matchWords)
        {
            var releases = new List<ReleaseInfo>();
            var qc = new NameValueCollection { { "q", query.SearchTerm } };

            // We search in the first "PagesToSearch" pages
            for (int i = 1; i <= PagesToSearch; i++)
            {
                var url = SiteLink + SearchUrl + i + "?" + qc.GetQueryString();
                var result = await RequestWithCookiesAsync(url);
                if (result.Status != HttpStatusCode.OK)
                    if (result.Status == HttpStatusCode.InternalServerError)
                    {
                        throw new ExceptionWithConfigData("HTTP 500 Internal Server Error", configData);
                    }
                    else
                    {
                        throw new ExceptionWithConfigData(result.ContentString, configData);
                    }
                try
                {
                    var searchResultParser = new HtmlParser();
                    using var doc = searchResultParser.ParseDocument(result.ContentString);

                    var table = doc.QuerySelector(".w-11\\/12");
                    // check the search term is valid
                    if (table?.QuerySelector("div.flex-row:nth-child(1)") != null)
                    {
                        // check there are results
                        var rows = table.Children;
                        if (rows is { Length: > 0 })
                            foreach (var row in rows)
                            {
                                var rowQuality = row.Children[0].Children[0].Children[0].TextContent;
                                var rowTitle = row.Children[0].Children[0].TextContent.Replace(rowQuality, String.Empty).Trim();
                                var rowDetailsLink = row.Children[0].GetAttribute("href");
                                var rowMejortorrentCat = row.Children[1].TextContent;
                                await ParseRelease(releases, rowTitle, rowDetailsLink, rowMejortorrentCat,
                                    null, rowQuality, query, matchWords);
                            }
                    }
                    else
                    {
                        i = PagesToSearch;
                    }
                }
                catch (Exception ex)
                {
                    OnParseError(result.ContentString, ex);
                }

            }

            return releases;
        }

        private async Task ParseRelease(ICollection<ReleaseInfo> releases, string title, string detailsStr,
            string mejortorrentCat, string publishStr, string quality, TorznabQuery query, bool matchWords)
        {
            // Remove trailing dot. Eg Harry Potter Y La Orden Del Fénix.
            title = title.Trim();
            if (title.EndsWith("."))
                title = title.Remove(title.Length - 1).Trim();

            var cat = GetMejortorrentCategory(mejortorrentCat, detailsStr, title, quality);
            if (cat == MejorTorrentCatType.Otro)
                return; // skip releases from this category

            var categories = MapTrackerCatToNewznab(cat);
            var publishDate = TryToParseDate(publishStr, DateTime.Now);

            // return results only for requested categories
            if (query.Categories.Any() && !query.Categories.Contains(categories.First()))
                return;

            // match the words in the query with the titles
            if (matchWords && !CheckTitleMatchWords(query.SearchTerm, title))
                return;

            // parsing is different for each category
            if (cat == MejorTorrentCatType.Serie || cat == MejorTorrentCatType.SerieHd)
                await ParseSeriesRelease(releases, query, title, detailsStr, cat, publishDate, quality);
            else if (query.Episode == null) // if it's scene series, we don't return other categories
            {
                if (cat == MejorTorrentCatType.Pelicula)
                    await ParseMovieRelease(releases, query, title, detailsStr, cat, publishDate, quality);
                else
                {
                    var release = GenerateRelease(title, detailsStr, detailsStr, cat, publishDate, 100.Megabytes());
                    releases.Add(release);
                }
            }
        }

        private async Task ParseSeriesRelease(ICollection<ReleaseInfo> releases, TorznabQuery query, string title,
            string detailsStr, string cat, DateTime publishDate, string quality)
        {
            var result = await RequestWithCookiesAsync(detailsStr);
            if (result.Status != HttpStatusCode.OK)
                throw new ExceptionWithConfigData(result.ContentString, configData);

            var searchResultParser = new HtmlParser();
            using var doc = searchResultParser.ParseDocument(result.ContentString);

            var rows = doc.QuerySelectorAll("tr.border");
            quality = CleanQuality(quality);
            ParseTags(title, quality);
            foreach (var row in rows)
            {
                var episodeTitle = row.Children[1].TextContent.Replace("\n", String.Empty);
                var downloadLink = row.Children.Last().Children[0].GetAttribute("href");
                var episodePublishStr = row.Children[2].TextContent.Replace("\n", String.Empty);
                var episodePublish = TryToParseDate(episodePublishStr, publishDate);

                // Convert the title to Scene format
                episodeTitle = ParseMejorTorrentSeriesTitle(title, episodeTitle, quality, query);

                // if the original query was in scene format, we filter the results to match episode
                // query.Episode != null means scene title
                if (query.Episode != null && !episodeTitle.Contains(query.GetEpisodeSearchString()))
                    continue;

                // guess size
                var size = 512.Megabytes();
                if (title.ToLower().Contains("720p"))
                    size = 1.Gigabytes();

                var release = GenerateRelease(episodeTitle, detailsStr, downloadLink, cat, episodePublish, size);
                releases.Add(release);
            }

        }

        private async Task ParseMovieRelease(ICollection<ReleaseInfo> releases, TorznabQuery query, string title,
            string detailsStr, string cat, DateTime publishDate, string quality)
        {

            var result = await RequestWithCookiesAsync(detailsStr);
            if (result.Status != HttpStatusCode.OK)
                throw new ExceptionWithConfigData(result.ContentString, configData);

            var searchResultParser = new HtmlParser();
            using var doc = searchResultParser.ParseDocument(result.ContentString);

            var downloadLink = doc.QuerySelector(".ml-2").GetAttribute("href");



            // clean quality
            quality = CleanQuality(quality);

            // add the year
            var detailsYear = doc.QuerySelector("div.py-4:nth-child(2) > p:nth-child(2) > a:nth-child(2)").TextContent;
            if (detailsYear != null)
            {
                title = title + " " + detailsYear;
            }
            else
            {
                title = query.Year != null ? title + " " + query.Year : title;

            }

            ParseTags(title, quality);

            // add spanish
            title += " SPANISH";

            // add quality
            if (quality != null)
                title += " " + quality;

            // guess size 1.5 GB

            var size = GuessSize(title, 1610612736L);

            var release = GenerateRelease(title, detailsStr, downloadLink, cat, publishDate, size);
            releases.Add(release);
        }

        private ReleaseInfo GenerateRelease(string title, string detailsStr, string downloadLink, string cat,
                                            DateTime publishDate, long size)
        {
            if (downloadLink.StartsWith("/"))
            {
                downloadLink = SiteLink + downloadLink.Substring(1);
            }
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

        private TorznabQuery ParseQuery(TorznabQuery query)
        {
            // Eg. Doctor.Who.2005.(Доктор.Кто).S02E08

            // the season/episode part is already parsed by Jackett
            // query.GetQueryString = Doctor.Who.2005.(Доктор.Кто).S02E08
            // query.Season = 2
            // query.Episode = 8
            var searchTerm = query.GetQueryString();
            // remove the season/episode from the query as MejorTorrent only wants the series name
            searchTerm = Regex.Replace(searchTerm, @"[S|s]\d+[E|e]\d+", "");

            // Server returns a 500 error if a UTF character higher than \u00FF (ÿ) is included,
            // so we need to strip them
            // searchTerm = Doctor Who 2005
            searchTerm = Regex.Replace(searchTerm, @"[^\u0001-\u00FF]+", " ");
            searchTerm = Regex.Replace(searchTerm, @"\s+", " ");
            searchTerm = searchTerm.Trim();

            // we parse the year and remove it from search
            // searchTerm = Doctor Who
            // query.Year = 2005
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

        private static string ParseMejorTorrentSeriesTitle(string title, string episodeTitle, string quality, TorznabQuery query)
        {
            // parse title
            // title = The Mandalorian - 1ª Temporada
            // title = The Mandalorian - 1ª Temporada [720p]
            // title = Grace and Frankie - 5ª Temporada [720p]: 5x08 al 5x13.
            var newTitle = title.Split(new[] { " - " }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
            // newTitle = The Mandalorian

            // parse episode title
            var newEpisodeTitle = episodeTitle.Trim();
            // episodeTitle = 5x08 al 5x13.
            // episodeTitle = 2x01 - 2x02 - 2x03.
            var matches = Regex.Matches(newEpisodeTitle, "([0-9]+)x([0-9]+)", RegexOptions.IgnoreCase);
            if (matches.Count > 1)
            {
                newEpisodeTitle = "";
                foreach (Match m in matches)
                    if (newEpisodeTitle.Equals(""))
                        newEpisodeTitle += "S" + m.Groups[1].Value.PadLeft(2, '0')
                                               + "E" + m.Groups[2].Value.PadLeft(2, '0');
                    else
                        newEpisodeTitle += "-E" + m.Groups[2].Value.PadLeft(2, '0');
                // newEpisodeTitle = S05E08-E13
                // newEpisodeTitle = S02E01-E02-E03
            }
            else
            {
                // episodeTitle = 1x04 - 05.
                var m = Regex.Match(newEpisodeTitle, "^([0-9]+)x([0-9]+)[^0-9]+([0-9]+)[.]?$", RegexOptions.IgnoreCase);
                if (m.Success)
                    newEpisodeTitle = "S" + m.Groups[1].Value.PadLeft(2, '0')
                                          + "E" + m.Groups[2].Value.PadLeft(2, '0') + "-"
                                          + "E" + m.Groups[3].Value.PadLeft(2, '0');
                // newEpisodeTitle = S01E04-E05
                else
                {
                    // episodeTitle = 1x02
                    // episodeTitle = 1x02 -
                    // episodeTitle = 1x08 -​ CONTRASEÑA: WWW.​PCTNEW ORG bebe
                    m = Regex.Match(newEpisodeTitle, "^([0-9]+)x([0-9]+)(.*)$", RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        newEpisodeTitle = "S" + m.Groups[1].Value.PadLeft(2, '0')
                                              + "E" + m.Groups[2].Value.PadLeft(2, '0');
                        // newEpisodeTitle = S01E02
                        if (!m.Groups[3].Value.Equals(""))
                            newEpisodeTitle += " " + m.Groups[3].Value.Replace(" -", "").Trim();
                        // newEpisodeTitle = S01E08 CONTRASEÑA: WWW.​PCTNEW ORG bebe
                    }
                }
            }

            // if the original query was in scene format, we have to put the year back
            // query.Episode != null means scene title
            var year = query.Episode != null && query.Year != null ? " " + query.Year : "";
            newTitle += year + " " + newEpisodeTitle;

            // add quality
            if (quality != null)
                newTitle += " SPANISH " + quality;

            else if (title.ToLower().Contains("[720p]"))
                newTitle += " SPANISH 720p HDTV x264";

            else
                newTitle += " SPANISH SDTV XviD";


            // return The Mandalorian S01E04 SPANISH 720p HDTV x264
            return newTitle;
        }

        private static string GetMejortorrentCategory(string mejortorrentCat, string detailsStr, string title, string quality)
        {
            // get root category
            var cat = MejorTorrentCatType.Otro;
            if (mejortorrentCat == null)
            {
                if (detailsStr.Contains("pelicula"))
                    cat = MejorTorrentCatType.Pelicula;
                else if (detailsStr.Contains("serie"))
                    cat = MejorTorrentCatType.Serie;
                else if (detailsStr.Contains("musica"))
                    cat = MejorTorrentCatType.Musica;
            }
            else if (mejortorrentCat.Equals(MejorTorrentCatType.Pelicula) ||
                     mejortorrentCat.Equals(MejorTorrentCatType.Serie) ||
                     mejortorrentCat.Equals(MejorTorrentCatType.Musica))
                cat = mejortorrentCat;

            else if (mejortorrentCat.Equals("peliculas"))
                cat = MejorTorrentCatType.Pelicula;

            else if (mejortorrentCat.Equals("series") || mejortorrentCat.Equals("documentales"))
                cat = MejorTorrentCatType.Serie;


            // hack to separate SD & HD series
            if (cat.Equals(MejorTorrentCatType.Serie))
            {
                if (title.ToLower().Contains("720p") ||
                    title.ToLower().Contains("1080p") ||
                    quality.ToLower().Contains("720p") ||
                    quality.ToLower().Contains("1080p"))
                    cat = MejorTorrentCatType.SerieHd;

            }

            return cat;
        }

        private void ParseTags(string title, string quality)
        {
            title = title.Trim();

            // parse tags in title, we need to put the year after the real title (before the tags)
            // Harry Potter And The Deathly Hallows: Part 1 [subs. Integrados]
            var tags = "";
            var queryMatches = Regex.Matches(title, @"[\[\(]([^\]\)]+)[\]\)]", RegexOptions.IgnoreCase);
            foreach (Match m in queryMatches)
            {
                var tag = m.Groups[1].Value.Trim().ToUpper();
                if (tag.Equals("4K")) // Fix 4K quality. Eg Harry Potter Y La Orden Del Fénix [4k]
                    quality = "(UHD 4K 2160p)";
                else if (tag.Equals("FULLBLURAY")) // Fix 4K quality. Eg Harry Potter Y El Cáliz De Fuego (fullbluray)
                    quality = "(COMPLETE BLURAY)";
                else // Add the tag to the title
                    tags += " " + tag;
                title = title.Replace(m.Groups[0].Value, "");
            }
            title += tags;

        }

        private long GuessSize(string title, long initialQuality)
        {
            var size = initialQuality;
            if (title.ToLower().Contains("microhd"))
                size = 7.Gigabytes();
            else if (title.ToLower().Contains("complete bluray") || title.ToLower().Contains("2160p"))
                size = 50.Gigabytes();
            else if (title.ToLower().Contains("bluray"))
                size = 16.Gigabytes();
            else if (title.ToLower().Contains("bdremux"))
                size = 20.Gigabytes();

            return size;
        }

        private static string CleanQuality(string quality)
        {
            if (quality != null)
            {
                var queryMatch = Regex.Match(quality, @"[\[\(]([^\]\)]+)[\]\)]", RegexOptions.IgnoreCase);
                if (queryMatch.Success)
                    quality = queryMatch.Groups[1].Value;
                quality = quality.Trim().Replace("-", " ");
                quality = Regex.Replace(quality, "HDRip", "BDRip", RegexOptions.IgnoreCase); // fix for Radarr
            }
            return quality;
        }

        private static DateTime TryToParseDate(string dateToParse, DateTime dateDefault)
        {
            try
            {
                return DateTime.ParseExact(dateToParse, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
            catch
            {
                // ignored
            }
            return dateDefault;
        }
    }
}
