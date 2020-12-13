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
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Newtonsoft.Json.Linq;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class MejorTorrent : BaseWebIndexer
    {
        private static class MejorTorrentCatType
        {
            public static string Pelicula => "Película";
            public static string Serie => "Serie";
            public static string SerieHd => "SerieHD"; // this category is created, doesn't exist in the site
            public static string Musica => "Música";
            public static string Otro => "Otro";
        }

        private const string NewTorrentsUrl = "secciones.php?sec=ultimos_torrents";
        private const string SearchUrl = "secciones.php";

        public override string[] LegacySiteLinks { get; protected set; } = {
            "http://www.mejortorrent.org/",
            "http://www.mejortorrent.tv/",
            "http://www.mejortorrentt.com/",
            "https://www.mejortorrentt.org/",
            "http://www.mejortorrentt.org/"
        };

        public MejorTorrent(IIndexerConfigurationService configService, WebClient w, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "mejortorrent",
                   name: "MejorTorrent",
                   description: "MejorTorrent - Hay veces que un torrent viene mejor! :)",
                   link: "https://www.mejortorrentt.net/",
                   caps: new TorznabCapabilities
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
                       }
                   },
                   configService: configService,
                   client: w,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationData())
        {
            Encoding = Encoding.UTF8;
            Language = "es-es";
            Type = "public";

            var matchWords = new BoolItem { Name = "Match words in title", Value = true };
            configData.AddDynamic("MatchWords", matchWords);

            AddCategoryMapping(MejorTorrentCatType.Pelicula, TorznabCatType.Movies, "Pelicula");
            AddCategoryMapping(MejorTorrentCatType.Serie, TorznabCatType.TVSD, "Serie");
            AddCategoryMapping(MejorTorrentCatType.SerieHd, TorznabCatType.TVHD, "Serie HD");
            AddCategoryMapping(MejorTorrentCatType.Musica, TorznabCatType.Audio, "Musica");
            // Other category is disabled because we have problems parsing documentaries
            //AddCategoryMapping(MejorTorrentCatType.Otro, TorznabCatType.Other, "Otro");
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
            var matchWords = ((BoolItem)configData.GetDynamic("MatchWords")).Value;
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
            var parser = new HtmlParser();
            var downloadUrl = link.ToString();

            // Eg https://www.mejortorrentt.net/peli-descargar-torrent-11995-Harry-Potter-y-la-piedra-filosofal.html
            var result = await RequestWithCookiesAsync(downloadUrl);
            if (result.Status != HttpStatusCode.OK)
                throw new ExceptionWithConfigData(result.ContentString, configData);
            var dom = parser.ParseDocument(result.ContentString);
            downloadUrl = SiteLink + dom.QuerySelector("a[href*=\"sec=descargas\"]").GetAttribute("href");

            // Eg https://www.mejortorrentt.net/secciones.php?sec=descargas&ap=contar&tabla=peliculas&id=11995&link_bajar=1
            result = await RequestWithCookiesAsync(downloadUrl);
            if (result.Status != HttpStatusCode.OK)
                throw new ExceptionWithConfigData(result.ContentString, configData);
            dom = parser.ParseDocument(result.ContentString);
            downloadUrl = SiteLink + dom.QuerySelector("a[href^=\"/tor/\"]").GetAttribute("href");

            // Eg https://www.mejortorrentt.net/tor/peliculas/Harry_Potter_1_y_la_Piedra_Filosofal_MicroHD_1080p.torrent
            var content = await base.Download(new Uri(downloadUrl));
            return content;
        }

        private async Task<List<ReleaseInfo>> PerformQueryNewest(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var url = SiteLink + NewTorrentsUrl;
            var result = await RequestWithCookiesAsync(url);
            if (result.Status != HttpStatusCode.OK)
                throw new ExceptionWithConfigData(result.ContentString, configData);
            try
            {
                var searchResultParser = new HtmlParser();
                var doc = searchResultParser.ParseDocument(result.ContentString);

                var container = doc.QuerySelector("#main_table_center_center1 table div");
                var parsedDetailsLink = new List<string>();
                string rowTitle = null;
                string rowDetailsLink = null;
                string rowPublishDate = null;
                string rowQuality = null;

                foreach (var row in container.Children)
                    if (row.TagName.Equals("A"))
                    {
                        rowTitle = row.TextContent;
                        rowDetailsLink = SiteLink + row.GetAttribute("href");
                    }
                    else if (rowPublishDate == null && row.TagName.Equals("SPAN"))
                        rowPublishDate = row.TextContent;
                    else if (rowPublishDate != null && row.TagName.Equals("SPAN"))
                        rowQuality = row.TextContent;
                    else if (row.TagName.Equals("BR"))
                    {
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
            }

            return releases;
        }

        private async Task<List<ReleaseInfo>> PerformQuerySearch(TorznabQuery query, bool matchWords)
        {
            var releases = new List<ReleaseInfo>();
            // search only the longest word, we filter the results later
            var searchTerm = GetLongestWord(query.SearchTerm);
            var qc = new NameValueCollection { { "sec", "buscador" }, { "valor", searchTerm } };
            var url = SiteLink + SearchUrl + "?" + qc.GetQueryString();
            var result = await RequestWithCookiesAsync(url);
            if (result.Status != HttpStatusCode.OK)
                throw new ExceptionWithConfigData(result.ContentString, configData);

            try
            {
                var searchResultParser = new HtmlParser();
                var doc = searchResultParser.ParseDocument(result.ContentString);

                var table = doc.QuerySelector("#main_table_center_center2 table table");
                // check the search term is valid
                if (table?.QuerySelector("tr table") != null)
                {
                    // check there are results
                    table = table.QuerySelector("tr table");
                    var rows = table.QuerySelectorAll("tr");
                    if (rows != null && rows.Length > 0 && rows[0].QuerySelectorAll("td").Length == 2)
                        foreach (var row in rows)
                        {
                            var link = row.QuerySelector("td a");
                            var rowTitle = link.TextContent;
                            var rowDetailsLink = SiteLink + link.GetAttribute("href").TrimStart('/');
                            var rowMejortorrentCat = row.QuerySelectorAll("td")[1].TextContent;
                            string rowQuality = null;
                            if (row.QuerySelector("td span") != null)
                                rowQuality = row.QuerySelector("td span").TextContent;

                            await ParseRelease(releases, rowTitle, rowDetailsLink, rowMejortorrentCat,
                                null, rowQuality, query, matchWords);
                        }
                }
            }
            catch (Exception ex)
            {
                OnParseError(result.ContentString, ex);
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

            var cat = GetMejortorrentCategory(mejortorrentCat, detailsStr, title);
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
                await ParseSeriesRelease(releases, query, title, detailsStr, cat, publishDate);
            else if (query.Episode == null) // if it's scene series, we don't return other categories
            {
                if (cat == MejorTorrentCatType.Pelicula)
                    ParseMovieRelease(releases, query, title, detailsStr, cat, publishDate, quality);
                else
                {
                    const long size = 104857600L; // 100 MB
                    var release = GenerateRelease(title, detailsStr, detailsStr, cat, publishDate, size);
                    releases.Add(release);
                }
            }
        }

        private async Task ParseSeriesRelease(ICollection<ReleaseInfo> releases, TorznabQuery query, string title,
            string detailsStr, string cat, DateTime publishDate)
        {
            var result = await RequestWithCookiesAsync(detailsStr);
            if (result.Status != HttpStatusCode.OK)
                throw new ExceptionWithConfigData(result.ContentString, configData);

            var searchResultParser = new HtmlParser();
            var doc = searchResultParser.ParseDocument(result.ContentString);

            var rows = doc.QuerySelectorAll("#main_table_center_center1 table table table tr");
            foreach (var row in rows)
            {
                var anchor = row.QuerySelector("a");
                if (anchor == null)
                    continue;

                var episodeTitle = anchor.TextContent.Trim();
                var downloadLink = SiteLink + anchor.GetAttribute("href").TrimStart('/');
                var episodePublishStr = row.QuerySelector("div").TextContent.Trim().Replace("Fecha: ", "");
                var episodePublish = TryToParseDate(episodePublishStr, publishDate);

                // Convert the title to Scene format
                episodeTitle = ParseMejorTorrentSeriesTitle(title, episodeTitle, query);

                // if the original query was in scene format, we filter the results to match episode
                // query.Episode != null means scene title
                if (query.Episode != null && !episodeTitle.Contains(query.GetEpisodeSearchString()))
                    continue;

                // guess size
                var size = 524288000L; // 500 MB
                if (episodeTitle.ToLower().Contains("720p"))
                    size = 1288490188L; // 1.2 GB

                var release = GenerateRelease(episodeTitle, detailsStr, downloadLink, cat, episodePublish, size);
                releases.Add(release);
            }

        }

        private void ParseMovieRelease(ICollection<ReleaseInfo> releases, TorznabQuery query, string title,
            string detailsStr, string cat, DateTime publishDate, string quality)
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
            title = title.Trim();

            // clean quality
            if (quality != null)
            {
                var queryMatch = Regex.Match(quality, @"[\[\(]([^\]\)]+)[\]\)]", RegexOptions.IgnoreCase);
                if (queryMatch.Success)
                    quality = queryMatch.Groups[1].Value;
                quality = quality.Trim().Replace("-", " ");
                quality = Regex.Replace(quality, "HDRip", "BDRip", RegexOptions.IgnoreCase); // fix for Radarr
            }

            // add the year
            title = query.Year != null ? title + " " + query.Year : title;

            // add the tags
            title += tags;

            // add spanish
            title += " SPANISH";

            // add quality
            if (quality != null)
                title += " " + quality;

            // guess size
            var size = 1610612736L; // 1.5 GB
            if (title.ToLower().Contains("microhd"))
                size = 7516192768L; // 7 GB
            else if (title.ToLower().Contains("complete bluray") || title.ToLower().Contains("2160p"))
                size = 53687091200L; // 50 GB
            else if (title.ToLower().Contains("bluray"))
                size = 17179869184L; // 16 GB
            else if (title.ToLower().Contains("bdremux"))
                size = 21474836480L; // 20 GB

            var release = GenerateRelease(title, detailsStr, detailsStr, cat, publishDate, size);
            releases.Add(release);
        }

        private ReleaseInfo GenerateRelease(string title, string detailsStr, string downloadLink, string cat,
                                            DateTime publishDate, long size)
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

        private static string ParseMejorTorrentSeriesTitle(string title, string episodeTitle, TorznabQuery query)
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
            if (title.ToLower().Contains("[720p]"))
                newTitle += " SPANISH 720p HDTV x264";
            else
                newTitle += " SPANISH SDTV XviD";

            // return The Mandalorian S01E04 SPANISH 720p HDTV x264
            return newTitle;
        }

        private static string GetMejortorrentCategory(string mejortorrentCat, string detailsStr, string title)
        {
            // get root category
            var cat = MejorTorrentCatType.Otro;
            if (mejortorrentCat == null)
            {
                if (detailsStr.Contains("peliculas_extend"))
                    cat = MejorTorrentCatType.Pelicula;
                else if (detailsStr.Contains("series_extend"))
                    cat = MejorTorrentCatType.Serie;
                else if (detailsStr.Contains("musica_extend"))
                    cat = MejorTorrentCatType.Musica;
            }
            else if (mejortorrentCat.Equals(MejorTorrentCatType.Pelicula) ||
                     mejortorrentCat.Equals(MejorTorrentCatType.Serie) ||
                     mejortorrentCat.Equals(MejorTorrentCatType.Musica))
                cat = mejortorrentCat;

            // hack to separate SD & HD series
            if (cat.Equals(MejorTorrentCatType.Serie) && title.ToLower().Contains("720p"))
                cat = MejorTorrentCatType.SerieHd;

            return cat;
        }

        private static string GetLongestWord(string text)
        {
            var words = text.Split(' ');
            if (!words.Any())
                return null;
            var longestWord = words.First();
            foreach (var word in words)
                if (word.Length >= longestWord.Length)
                    longestWord = word;
            return longestWord;
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
