using System;
using System.Collections.Generic;
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
    public class DonTorrent : IndexerBase
    {
        public override string Id => "dontorrent";
        public override string Name => "DonTorrent";
        public override string Description => "DonTorrent is a SPANISH public tracker for MOVIES / TV / GENERAL";
        public override string SiteLink { get; protected set; } = "https://dontorrent.care/";
        public override string[] AlternativeSiteLinks => new[]
        {
            "https://dontorrent.care/",
            "https://todotorrents.net/",
            "https://tomadivx.net/",
            "https://seriesblanco.one/",
            "https://verdetorrent.com/",
            "https://naranjatorrent.com/"
        };
        public override string[] LegacySiteLinks => new[]
        {
            "https://dontorrent.gs/",
            "https://dontorrent.gy/",
            "https://dontorrent.click/",
            "https://dontorrent.fail/",
            "https://dontorrent.futbol/",
            "https://dontorrent.mba/",
            "https://dontorrent.army/",
            "https://dontorrent.blue/",
            "https://dontorrent.beer/",
            "https://dontorrent.surf/",
            "https://dontorrent.how/",
            "https://dontorrent.casa/",
            "https://dontorrent.chat/",
            "https://dontorrent.plus/",
            "https://dontorrent.ninja/",
            "https://dontorrent.love/",
            "https://dontorrent.cloud/",
            "https://dontorrent.africa/",
            "https://dontorrent.pictures/",
            "https://dontorrent.ms/"
        };
        public override string Language => "es-ES";
        public override string Type => "public";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private static class DonTorrentCatType
        {
            public static string Pelicula => "pelicula";
            public static string Pelicula4K => "pelicula4k";
            public static string Serie => "serie";
            public static string SerieHD => "seriehd";
            public static string Documental => "documental";
            public static string Musica => "musica";
            public static string Variado => "variado";
            public static string Juego => "juego";
        }

        private const string NewTorrentsUrl = "ultimos";
        private const string SearchUrl = "buscar/";

        private static Dictionary<string, string> CategoriesMap => new Dictionary<string, string>
            {
                { "/pelicula/", DonTorrentCatType.Pelicula },
                { "/serie/", DonTorrentCatType.Serie },
                { "/documental", DonTorrentCatType.Documental },
                { "/musica/", DonTorrentCatType.Musica },
                { "/variado/", DonTorrentCatType.Variado },
                { "/juego/", DonTorrentCatType.Juego } //games, it can be pc or console
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
                MusicSearchParams = new List<MusicSearchParam>
                {
                    MusicSearchParam.Q,
                }
            };

            caps.Categories.AddCategoryMapping(DonTorrentCatType.Pelicula, TorznabCatType.Movies, "Pelicula");
            caps.Categories.AddCategoryMapping(DonTorrentCatType.Pelicula4K, TorznabCatType.MoviesUHD, "Peliculas 4K");
            caps.Categories.AddCategoryMapping(DonTorrentCatType.Serie, TorznabCatType.TVSD, "Serie");
            caps.Categories.AddCategoryMapping(DonTorrentCatType.SerieHD, TorznabCatType.TVHD, "Serie HD");
            caps.Categories.AddCategoryMapping(DonTorrentCatType.Musica, TorznabCatType.Audio, "Música");

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
            if (downloadUrl.Contains("cdn.pizza") || downloadUrl.Contains("blazing.network") || downloadUrl.Contains("tor.cat") || downloadUrl.Contains("cdndelta.com") || downloadUrl.Contains("cdnbeta.in"))
            {
                return await base.Download(link);
            }

            var parser = new HtmlParser();

            // Eg https://dontorrent.li/pelicula/24797/Halloween-Kills
            var result = await RequestWithCookiesAsync(downloadUrl);
            if (result.Status != HttpStatusCode.OK)
                throw new ExceptionWithConfigData(result.ContentString, configData);
            var dom = parser.ParseDocument(result.ContentString);

            //var info = dom.QuerySelectorAll("div.descargar > div.card > div.card-body").First();
            //var title = info.QuerySelector("h2.descargarTitulo").TextContent;

            var dlStr = dom.QuerySelector("div.text-center > p > a");

            //dl site starts with "//cdn.pizza" and they accept https so use it
            downloadUrl = dlStr != null ? string.Format("https:{0}", dlStr.GetAttribute("href")) : "";

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
            logger.Debug("\naaa");
            try
            {
                var searchResultParser = new HtmlParser();
                var doc = searchResultParser.ParseDocument(result.ContentString);

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
                        // we add parsed items to rowDetailsLink to avoid duplicates in newest torrents
                        // list results
                        if (!parsedDetailsLink.Contains(rowDetailsLink) && rowTitle != null)
                        {
                            var cat = GetCategory(rowTitle, rowDetailsLink);
                            switch (cat)
                            {
                                case "pelicula":
                                case "pelicula4k":
                                case "serie":
                                case "seriehd":
                                case "musica":
                                    await ParseRelease(releases, rowDetailsLink, rowTitle, cat, rowQuality, query, false);
                                    parsedDetailsLink.Add(rowDetailsLink);
                                    break;
                                default:
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

        private async Task<List<ReleaseInfo>> PerformQuerySearch(TorznabQuery query, bool matchWords)
        {
            var releases = new List<ReleaseInfo>();
            // search only the longest word, we filter the results later
            var searchTerm = GetLongestWord(query.SearchTerm);
            var url = SiteLink + SearchUrl + searchTerm;
            var result = await RequestWithCookiesAsync(url, referer: url);
            if (result.Status != HttpStatusCode.OK)
                throw new ExceptionWithConfigData(result.ContentString, configData);

            try
            {
                var searchResultParser = new HtmlParser();
                var doc = searchResultParser.ParseDocument(result.ContentString);

                var rows = doc.QuerySelectorAll("div.seccion#buscador > div.card > div.card-body > p");

                if (rows.First().TextContent.Contains("Introduce alguna palabra para buscar con al menos 2 letras."))
                {
                    return releases; //no enough search terms
                }

                foreach (var row in rows.Skip(2))
                {
                    //href=/pelicula/6981/Saga-Spiderman
                    var link = string.Format("{0}{1}", SiteLink.TrimEnd('/'), row.QuerySelector("p > span > a").GetAttribute("href"));
                    var title = row.QuerySelector("p > span > a").TextContent;
                    var cat = GetCategory(title, link);
                    var quality = "";

                    switch (GetCategoryFromURL(link))
                    {
                        case "pelicula":
                        case "serie":
                            quality = Regex.Replace(row.QuerySelector("p > span > span").TextContent, "([()])", "");

                            break;
                    }

                    switch (cat)
                    {
                        case "pelicula":
                        case "pelicula4k":
                        case "serie":
                        case "seriehd":
                        case "musica":
                            await ParseRelease(releases, link, title, cat, quality, query, matchWords);
                            break;
                        default: //ignore different categories
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                OnParseError(result.ContentString, ex);
            }

            return releases;
        }

        private async Task ParseRelease(ICollection<ReleaseInfo> releases, string link, string title, string category, string quality, TorznabQuery query, bool matchWords)
        {
            // Remove trailing dot if there's one.
            title = title.Trim();
            if (title.EndsWith("."))
                title = title.Remove(title.Length - 1).Trim();

            //There's no public publishDate
            //var publishDate = TryToParseDate(publishStr, DateTime.Now);

            // return results only for requested categories
            if (query.Categories.Any() && !query.Categories.Contains(MapTrackerCatToNewznab(category).First()))
                return;

            // match the words in the query with the titles
            if (matchWords && !CheckTitleMatchWords(query.SearchTerm, title))
                return;

            switch (category)
            {
                case "pelicula":
                case "pelicula4k":
                    await ParseMovieRelease(releases, link, query, title, quality);
                    break;
                case "serie":
                case "seriehd":
                    await ParseSeriesRelease(releases, link, query, title, quality);
                    break;
                case "musica":
                    await ParseMusicRelease(releases, link, query, title);
                    break;
                default:
                    break;
            }
        }

        private async Task ParseMusicRelease(ICollection<ReleaseInfo> releases, string link, TorznabQuery query, string title)
        {
            var result = await RequestWithCookiesAsync(link);
            if (result.Status != HttpStatusCode.OK)
                throw new ExceptionWithConfigData(result.ContentString, configData);

            var searchResultParser = new HtmlParser();
            var doc = searchResultParser.ParseDocument(result.ContentString);

            var data = doc.QuerySelector("div.descargar > div.card > div.card-body");

            //var _title = data.QuerySelector("h2.descargarTitulo").TextContent;

            //var data2 = data.QuerySelectorAll("div.d-inline-block > p");

            //var yearStr = data2[0].TextContent;

            var data3 = data.QuerySelectorAll("div.text-center > div.d-inline-block");

            var publishStr = data3[0].TextContent; //"Fecha: {0}" -- needs trimming
            var sizeStr = data3[1].TextContent; //"Tamaño: {0}" -- needs trimming, contains number of episodes available

            var publishDate = TryToParseDate(publishStr, DateTime.Now);
            var size = ParseUtil.GetBytes(sizeStr);

            var release = GenerateRelease(title, link, link, GetCategory(title, link), publishDate, size);
            releases.Add(release);
        }

        private async Task ParseSeriesRelease(ICollection<ReleaseInfo> releases, string link, TorznabQuery query, string title, string quality)
        {
            var result = await RequestWithCookiesAsync(link);
            if (result.Status != HttpStatusCode.OK)
                throw new ExceptionWithConfigData(result.ContentString, configData);

            var searchResultParser = new HtmlParser();
            var doc = searchResultParser.ParseDocument(result.ContentString);

            var data = doc.QuerySelector("div.descargar > div.card > div.card-body");

            //var _title = data.QuerySelector("h2.descargarTitulo").TextContent;

            //var data2 = data.QuerySelectorAll("div.d-inline-block > p");

            //var quality = data2[0].TextContent; //"Formato: {0}" -- needs trimming
            //var episodes = data2[1].TextContent; //"Episodios: {0}" -- needs trimming, contains number of episodes available

            var data3 = data.QuerySelectorAll("div.d-inline-block > table.table > tbody > tr");

            foreach (var row in data3)
            {
                var episodeData = row.QuerySelectorAll("td");

                var episodeTitle = episodeData[0].TextContent; //it may contain two episodes divided by '&', eg '1x01 & 1x02'
                var downloadLink = "https:" + episodeData[1].QuerySelector("a").GetAttribute("href"); // URL like "//cdn.pizza/"
                var episodePublishStr = episodeData[2].TextContent;
                var episodePublish = TryToParseDate(episodePublishStr, DateTime.Now);

                // Convert the title to Scene format
                episodeTitle = ParseSeriesTitle(title, episodeTitle, query);

                // if the original query was in scene format, we filter the results to match episode
                // query.Episode != null means scene title
                if (query.Episode != null && !episodeTitle.Contains(query.GetEpisodeSearchString()))
                    continue;

                // guess size
                var size = 536870912L; // 512 MB
                if (episodeTitle.ToLower().Contains("720p"))
                    size = 1073741824L; // 1 GB
                if (episodeTitle.ToLower().Contains("1080p"))
                    size = 4294967296L; // 4 GB

                size *= GetEpisodeCountFromTitle(episodeTitle);

                var release = GenerateRelease(episodeTitle, link, downloadLink, GetCategory(title, link), episodePublish, size);
                releases.Add(release);
            }
        }

        private async Task ParseMovieRelease(ICollection<ReleaseInfo> releases, string link, TorznabQuery query, string title, string quality)
        {
            title = title.Trim();

            var result = await RequestWithCookiesAsync(link);
            if (result.Status != HttpStatusCode.OK)
                throw new ExceptionWithConfigData(result.ContentString, configData);

            var searchResultParser = new HtmlParser();
            var doc = searchResultParser.ParseDocument(result.ContentString);

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

            var info = doc.QuerySelectorAll("div.descargar > div.card > div.card-body").First();
            var moreinfo = info.QuerySelectorAll("div.text-center > div.d-inline-block");

            // guess size
            long size;
            if (moreinfo.Length == 2)
                size = ParseUtil.GetBytes(moreinfo[1].QuerySelector("p").TextContent);
            else if (title.ToLower().Contains("4k"))
                size = 53687091200L; // 50 GB
            else if (title.ToLower().Contains("1080p"))
                size = 4294967296L; // 4 GB
            else if (title.ToLower().Contains("720p"))
                size = 1073741824L; // 1 GB
            else
                size = 536870912L; // 512 MB

            var release = GenerateRelease(title, link, link, GetCategory(title, link), DateTime.Now, size);
            releases.Add(release);
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

        private static string ParseSeriesTitle(string title, string episodeTitle, TorznabQuery query)
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

            newTitle += " SPANISH";

            // multilanguage
            if (title.ToLower().Contains("ES-EN"))
                newTitle += " ENGLISH";

            //quality
            if (title.ToLower().Contains("720p"))
                newTitle += " 720p";
            else if (title.ToLower().Contains("1080p"))
                newTitle += " 1080p";
            else
                newTitle += " SDTV";

            if (title.ToLower().Contains("HDTV"))
                newTitle += " HDTV";

            if (title.ToLower().Contains("x265"))
                newTitle += " x265";
            else
                newTitle += " x264";

            // return The Mandalorian S01E04 SPANISH 720p HDTV x264
            return newTitle;
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


        public static string GetCategory(string title, string url)
        {
            var cat = GetCategoryFromURL(url);
            switch (cat)
            {
                case "pelicula":
                case "pelicula4k":
                    if (title.Contains("4K"))
                    {
                        cat = DonTorrentCatType.Pelicula4K;
                    }
                    break;

                case "serie":
                case "seriehd":
                    if (title.Contains("720p") || title.Contains("1080p"))
                    {
                        cat = DonTorrentCatType.SerieHD;
                    }

                    break;
                default:
                    break;
            }
            return cat;
        }

        public static string GetCategoryFromURL(string url)
        {
            return CategoriesMap
                .Where(categoryMap => url.Contains(categoryMap.Key))
                .Select(categoryMap => categoryMap.Value)
                .FirstOrDefault();
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
