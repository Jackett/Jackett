using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class BJShare : BaseWebIndexer
    {
        private string LoginUrl => SiteLink + "login.php";
        private string BrowseUrl => SiteLink + "torrents.php";
        private string TodayUrl => SiteLink + "torrents.php?action=today";
        private static readonly Regex _EpisodeRegex = new Regex(@"(?:[SsEe]\d{2,4}){1,2}");

        public override string[] LegacySiteLinks { get; protected set; } =
        {
            "https://bj-share.me/"
        };

        private ConfigurationDataBasicLoginWithRSSAndDisplay ConfigData => (ConfigurationDataBasicLoginWithRSSAndDisplay)configData;



        private readonly List<string> _absoluteNumbering = new List<string>
        {
            "One Piece",
            "Boruto",
            "Black Clover",
            "Fairy Tail",
            "Super Dragon Ball Heroes"
        };

        private readonly Dictionary<string, string> _commonResultTerms = new Dictionary<string, string>
        {
            {"tell me a story", "Tell Me a Story US"},
            {"fairy tail: final season", "Fairy Tail: Final Series"},
            {"agents of s.h.i.e.l.d.", "Marvels Agents of SHIELD"},
            {"legends of tomorrow", "DCs Legends of Tomorrow"}
        };

        private readonly Dictionary<string, string> _commonSearchTerms = new Dictionary<string, string>
        {
            {"agents of shield", "Agents of S.H.I.E.L.D."},
            {"tell me a story us", "Tell Me a Story"},
            {"greys anatomy", "grey's anatomy"}
        };

        public BJShare(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "bjshare",
                    name: "BJ-Share",
                    description: "A brazilian tracker.",
                    link: "https://bj-share.info/",
                    caps: new TorznabCapabilities
                    {
                        TvSearchParams = new List<TvSearchParam>
                        {
                            TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                        },
                        MovieSearchParams = new List<MovieSearchParam>
                        {
                            MovieSearchParam.Q, MovieSearchParam.ImdbId
                        },
                        MusicSearchParams = new List<MusicSearchParam>
                        {
                            MusicSearchParam.Q
                        },
                        BookSearchParams = new List<BookSearchParam>
                        {
                            BookSearchParam.Q
                        }
                    },
                    configService: configService,
                    client: wc,
                    logger: l,
                    p: ps,
                    cacheService: cs,
                    configData: new ConfigurationDataBasicLoginWithRSSAndDisplay())
        {
            Encoding = Encoding.UTF8;
            Language = "pt-BR";
            Type = "private";

            AddCategoryMapping(14, TorznabCatType.TVAnime, "Anime");
            AddCategoryMapping(3, TorznabCatType.PC0day, "Aplicativos");
            AddCategoryMapping(8, TorznabCatType.Other, "Apostilas/Tutoriais");
            AddCategoryMapping(19, TorznabCatType.AudioAudiobook, "Audiobook");
            AddCategoryMapping(16, TorznabCatType.TVOther, "Desenho Animado");
            AddCategoryMapping(18, TorznabCatType.TVDocumentary, "Documentários");
            AddCategoryMapping(10, TorznabCatType.Books, "E-Books");
            AddCategoryMapping(20, TorznabCatType.TVSport, "Esportes");
            AddCategoryMapping(1, TorznabCatType.Movies, "Filmes");
            AddCategoryMapping(12, TorznabCatType.MoviesOther, "Histórias em Quadrinhos");
            AddCategoryMapping(5, TorznabCatType.Audio, "Músicas");
            AddCategoryMapping(7, TorznabCatType.Other, "Outros");
            AddCategoryMapping(9, TorznabCatType.BooksMags, "Revistas");
            AddCategoryMapping(2, TorznabCatType.TV, "Seriados");
            AddCategoryMapping(17, TorznabCatType.TV, "Shows");
            AddCategoryMapping(13, TorznabCatType.TV, "Stand Up Comedy");
            AddCategoryMapping(11, TorznabCatType.Other, "Video-Aula");
            AddCategoryMapping(6, TorznabCatType.TV, "Vídeos de TV");
            AddCategoryMapping(4, TorznabCatType.PCGames, "Jogos");
            AddCategoryMapping(199, TorznabCatType.XXX, "Filmes Adultos");
            AddCategoryMapping(200, TorznabCatType.XXX, "Jogos Adultos");
            AddCategoryMapping(201, TorznabCatType.XXXImageSet, "Fotos Adultas");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string>
            {
                {"username", ConfigData.Username.Value},
                {"password", ConfigData.Password.Value},
                {"keeplogged", "1"}
            };
            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, LoginUrl, true);
            await ConfigureIfOK(
                result.Cookies, result.ContentString?.Contains("logout.php") == true, () =>
                {
                    var errorMessage = result.ContentString;
                    throw new ExceptionWithConfigData(errorMessage, ConfigData);
                });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        private static string InternationalTitle(string title)
        {
            var match = Regex.Match(title, @".* \[(.*\/?)\]");
            return match.Success ? match.Groups[1].Value.Split('/')[0] : title;
        }

        private static string StripSearchString(string term, bool isAnime)
        {
            // Search does not support searching with episode numbers so strip it if we have one
            // Ww AND filter the result later to archive the proper result
            term = _EpisodeRegex.Replace(term, string.Empty);
            term = isAnime ? Regex.Replace(term, @"\d*$", string.Empty) : term;
            return term.TrimEnd();
        }

        private string ParseTitle(string title, string seasonEp, string year, string categoryStr)
        {
            // Removes the SxxExx if it comes on the title
            var cleanTitle = _EpisodeRegex.Replace(title, string.Empty);
            // Removes the year if it comes on the title
            // The space is added because on daily releases the date will be XX/XX/YYYY
            if (!string.IsNullOrEmpty(year))
                cleanTitle = cleanTitle.Replace(" " + year, string.Empty);
            cleanTitle = Regex.Replace(cleanTitle, @"^\s*|[\s-]*$", string.Empty);

            // Get international title if available, or use the full title if not
            cleanTitle = InternationalTitle(cleanTitle);
            foreach (var resultTerm in _commonResultTerms)
            {
                var newTitle = cleanTitle.ToLower().Replace(resultTerm.Key.ToLower(), resultTerm.Value);
                if (!string.Equals(newTitle, cleanTitle, StringComparison.CurrentCultureIgnoreCase))
                    cleanTitle = newTitle;
            }

            // do not include year to animes
            if (categoryStr == "14" || cleanTitle.Contains(year))
                cleanTitle += " " + seasonEp;
            else
                cleanTitle += " " + year + " " + seasonEp;

            cleanTitle = FixAbsoluteNumbering(cleanTitle);
            cleanTitle = FixNovelNumber(cleanTitle);
            cleanTitle = cleanTitle.Trim();
            return cleanTitle;
        }

        private bool IsAbsoluteNumbering(string title)
        {
            foreach (var absoluteTitle in _absoluteNumbering)
                if (title.ToLower().Contains(absoluteTitle.ToLower()))
                    return true;
            return false;
        }

        private string FixNovelNumber(string title)
        {

            if (title.Contains("[Novela]"))
            {
                title = Regex.Replace(title, @"(Cap[\.]?[ ]?)", "S01E");
                title = Regex.Replace(title, @"(\[Novela\]\ )", "");
                title = Regex.Replace(title, @"(\ \-\s*Completo)", " - S01");
                return title;
            }

            return title;
        }

        private string FixAbsoluteNumbering(string title)
        {
            // if result is absolute numbered, convert title from SXXEXX to EXX
            // Only few animes that i'm aware is in "absolute" numbering, the problem is that they include
            // the season (wrong season) and episode as absolute, eg: One Piece - S08E836
            // 836 is the latest episode in absolute numbering, that is correct, but S08 is not the current season...
            // So for this show, i don't see a other way to make it work...
            //
            // All others animes that i tested is with correct season and episode set, so i can't remove the season from all
            // or will break everything else
            //
            // In this indexer, it looks that it is added "automatically", so all current and new releases will be broken
            // until they or the source from where they get that info fix it...
            if (IsAbsoluteNumbering(title))
            {
                title = Regex.Replace(title, @"(Ep[\.]?[ ]?)|([S]\d\d[Ee])", "");
                return title;
            }

            return title;
        }

        private bool IsSessionIsClosed(WebResult result)
        {
            return result.IsRedirect && result.RedirectingTo.Contains("login.php");
        }

        private string FixSearchTerm(TorznabQuery query)
        {
            if (query.IsImdbQuery)
                return query.ImdbID;
            return _commonSearchTerms.Aggregate(
                query.GetQueryString(),
                (current, searchTerm) => current.ToLower().Replace(searchTerm.Key.ToLower(), searchTerm.Value));
        }

        // if the search string is empty use the "last 24h torrents" view
        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query) =>
            (string.IsNullOrWhiteSpace(query.SearchTerm) && !query.IsImdbQuery)
                ? await ParseLast24HoursAsync()
                : await ParseUserSearchAsync(query);

        private async Task<List<ReleaseInfo>> ParseUserSearchAsync(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchUrl = BrowseUrl;
            var isSearchAnime = query.Categories.Any(s => s == TorznabCatType.TVAnime.ID);
            var searchTerm = FixSearchTerm(query);
            var queryCollection = new NameValueCollection
            {
                {"searchstr", StripSearchString(searchTerm, isSearchAnime)},
                {"order_by", "time"},
                {"order_way", "desc"},
                {"group_results", "1"},
                {"action", "basic"},
                {"searchsubmit", "1"}
            };
            foreach (var cat in MapTorznabCapsToTrackers(query))
                queryCollection.Add("filter_cat[" + cat + "]", "1");
            searchUrl += "?" + queryCollection.GetQueryString();
            var results = await RequestWithCookiesAsync(searchUrl);
            if (IsSessionIsClosed(results))
            {
                // re-login
                await ApplyConfiguration(null);
                results = await RequestWithCookiesAsync(searchUrl);
            }

            try
            {
                const string rowsSelector = "table.torrent_table > tbody > tr:not(tr.colhead)";
                var searchResultParser = new HtmlParser();
                var searchResultDocument = searchResultParser.ParseDocument(results.ContentString);
                var rows = searchResultDocument.QuerySelectorAll(rowsSelector);
                ICollection<int> groupCategory = null;
                string groupTitle = null;
                string groupYearStr = null;
                var categoryStr = "";
                foreach (var row in rows)
                    try
                    {
                        // ignore sub groups info row, it's just an row with an info about the next section, something like "Dual Áudio" or "Legendado"
                        if (row.QuerySelector(".edition_info") != null)
                            continue;

                        // some torrents has more than one link, and the one with .tooltip is the wrong one in that case,
                        // so let's try to pick up first without the .tooltip class,
                        // if nothing is found, then we try again without that filter
                        var qDetailsLink = row.QuerySelector("a[href^=\"torrents.php?id=\"]:not(.tooltip)");
                        if (qDetailsLink == null)
                        {
                            qDetailsLink = row.QuerySelector("a[href^=\"torrents.php?id=\"]");
                            // if still can't find the right link, skip it
                            if (qDetailsLink == null)
                            {
                                logger.Error($"{Id}: Error while parsing row '{row.OuterHtml}': Can't find the right details link");
                                continue;
                            }
                        }
                        var title = StripSearchString(qDetailsLink.TextContent, false);

                        var seasonEl = row.QuerySelector("a[href^=\"torrents.php?torrentid=\"]");
                        string seasonEp = null;
                        if (seasonEl != null)
                        {
                            var seasonMatch = _EpisodeRegex.Match(seasonEl.TextContent);
                            seasonEp = seasonMatch.Success ? seasonMatch.Value : null;
                        }
                        seasonEp ??= _EpisodeRegex.Match(qDetailsLink.TextContent).Value;

                        ICollection<int> category = null;
                        string yearStr = null;
                        if (row.ClassList.Contains("group") || row.ClassList.Contains("torrent")) // group/ungrouped headers
                        {
                            var qCatLink = row.QuerySelector("a[href^=\"/torrents.php?filter_cat\"]");
                            categoryStr = qCatLink.GetAttribute("href").Split('=')[1].Split('&')[0];
                            category = MapTrackerCatToNewznab(categoryStr);

                            var torrentInfoEl = row.QuerySelector("div.torrent_info");
                            if (torrentInfoEl != null)
                            {
                                // valid for torrent grouped but that has only 1 episode yet
                                yearStr = torrentInfoEl.GetAttribute("data-year");
                            }
                            yearStr ??= qDetailsLink.NextSibling.TextContent.Trim().TrimStart('[').TrimEnd(']');

                            if (row.ClassList.Contains("group")) // group headers
                            {
                                groupCategory = category;
                                groupTitle = title;
                                groupYearStr = yearStr;
                                continue;
                            }
                        }

                        var release = new ReleaseInfo
                        {
                            MinimumRatio = 1,
                            MinimumSeedTime = 0
                        };
                        var qDlLink = row.QuerySelector("a[href^=\"torrents.php?action=download\"]");
                        var qSize = row.QuerySelector("td:nth-last-child(4)");
                        var qGrabs = row.QuerySelector("td:nth-last-child(3)");
                        var qSeeders = row.QuerySelector("td:nth-last-child(2)");
                        var qLeechers = row.QuerySelector("td:nth-last-child(1)");
                        var qFreeLeech = row.QuerySelector("strong[title=\"Free\"]");
                        if (row.ClassList.Contains("group_torrent")) // torrents belonging to a group
                        {
                            release.Description = Regex.Match(qDetailsLink.TextContent, @"\[.*?\]").Value;
                            release.Title = ParseTitle(groupTitle, seasonEp, groupYearStr, categoryStr);
                            release.Category = groupCategory;
                        }
                        else if (row.ClassList.Contains("torrent")) // standalone/un grouped torrents
                        {
                            release.Description = row.QuerySelector("div.torrent_info").TextContent;
                            release.Title = ParseTitle(title, seasonEp, yearStr, categoryStr);
                            release.Category = category;
                        }

                        release.Description = release.Description.Replace(" / Free", ""); // Remove Free Tag
                        release.Description = release.Description.Replace("/ WEB ", "/ WEB-DL "); // Fix web/web-dl
                        release.Description = release.Description.Replace("Full HD", "1080p");
                        // Handles HDR conflict
                        release.Description = release.Description.Replace("/ HD /", "/ 720p /");
                        release.Description = release.Description.Replace("/ HD]", "/ 720p]");
                        release.Description = release.Description.Replace("4K", "2160p");
                        release.Description = release.Description.Replace("SD", "480p");
                        release.Description = release.Description.Replace("Dual Áudio", "Dual");

                        // Adjust the description in order to can be read by Radarr and Sonarr
                        var cleanDescription = release.Description.Trim().TrimStart('[').TrimEnd(']');
                        string[] titleElements;

                        //Formats the title so it can be parsed later
                        var stringSeparators = new[]
                        {
                            " / "
                        };
                        titleElements = cleanDescription.Split(stringSeparators, StringSplitOptions.None);
                        // release.Title += string.Join(" ", titleElements);
                        release.Title = release.Title.Trim();
                        if (titleElements.Length < 6)
                            // Usually non movies / series could have less than 6 elements, eg: Books.
                            release.Title += " " + string.Join(" ", titleElements);
                        else
                            release.Title += " " + titleElements[5] + " " + titleElements[3] + " " + titleElements[1] + " " +
                                             titleElements[2] + " " + titleElements[4] + " " + string.Join(
                                                 " ", titleElements.Skip(6));

                        if (Regex.IsMatch(release.Description, "(Dual|[Nn]acional|[Dd]ublado)"))
                            release.Title += " Brazilian";

                        // This tracker does not provide an publish date to search terms (only on last 24h page)
                        release.PublishDate = DateTime.Today;

                        // check for previously stripped search terms
                        if (!query.IsImdbQuery && !query.MatchQueryStringAND(release.Title, null, searchTerm))
                            continue;
                        var size = qSize.TextContent;
                        release.Size = ReleaseInfo.GetBytes(size);
                        release.Link = new Uri(SiteLink + qDlLink.GetAttribute("href"));
                        release.Details = new Uri(SiteLink + qDetailsLink.GetAttribute("href"));
                        release.Guid = release.Link;
                        release.Grabs = ParseUtil.CoerceLong(qGrabs.TextContent);
                        release.Seeders = ParseUtil.CoerceInt(qSeeders.TextContent);
                        release.Peers = ParseUtil.CoerceInt(qLeechers.TextContent) + release.Seeders;
                        release.DownloadVolumeFactor = qFreeLeech != null ? 0 : 1;
                        release.UploadVolumeFactor = 1;
                        releases.Add(release);
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"{Id}: Error while parsing row '{row.OuterHtml}': {ex.Message}");
                    }
            }
            catch (Exception ex)
            {
                OnParseError(results.ContentString, ex);
            }

            return releases;
        }

        private async Task<List<ReleaseInfo>> ParseLast24HoursAsync()
        {
            var releases = new List<ReleaseInfo>();
            var results = await RequestWithCookiesAsync(TodayUrl);
            if (IsSessionIsClosed(results))
            {
                // re-login
                await ApplyConfiguration(null);
                results = await RequestWithCookiesAsync(TodayUrl);
            }

            try
            {
                const string rowsSelector = "table.torrent_table > tbody > tr:not(tr.colhead)";
                var searchResultParser = new HtmlParser();
                var searchResultDocument = searchResultParser.ParseDocument(results.ContentString);
                var rows = searchResultDocument.QuerySelectorAll(rowsSelector);
                foreach (var row in rows)
                    try
                    {
                        var release = new ReleaseInfo
                        {
                            MinimumRatio = 1,
                            MinimumSeedTime = 0
                        };
                        var qDetailsLink = row.QuerySelector("a.BJinfoBox");
                        var qBJinfoBox = qDetailsLink.QuerySelector("span");
                        var qCatLink = row.QuerySelector("a[href^=\"/torrents.php?filter_cat\"]");
                        var qDlLink = row.QuerySelector("a[href^=\"torrents.php?action=download\"]");
                        var qSeeders = row.QuerySelector("td:nth-child(4)");
                        var qLeechers = row.QuerySelector("td:nth-child(5)");
                        var qQuality = row.QuerySelector("font[color=\"red\"]");
                        var qFreeLeech = row.QuerySelector("font[color=\"green\"]:contains(Free)");
                        var qTitle = qDetailsLink.QuerySelector("font");
                        // Get international title if available, or use the full title if not
                        release.Title = qTitle.TextContent;
                        var seasonEp = _EpisodeRegex.Match(release.Title).Value;
                        var year = "";
                        release.Description = "";
                        var extraInfo = "";
                        var releaseQuality = "";
                        foreach (var child in qBJinfoBox.ChildNodes)
                        {
                            var type = child.NodeType;
                            if (type != NodeType.Text)
                                continue;
                            var line = child.TextContent;
                            if (line.StartsWith("Tamanho:"))
                            {
                                var size = line.Substring("Tamanho: ".Length);
                                release.Size = ReleaseInfo.GetBytes(size);
                            }
                            else if (line.StartsWith("Lançado em: "))
                            {
                                var publishDateStr = line.Substring("Lançado em: ".Length).Replace("às ", "");
                                publishDateStr += " +0";
                                var publishDate = DateTime.SpecifyKind(
                                    DateTime.ParseExact(publishDateStr, "dd/MM/yyyy HH:mm z", CultureInfo.InvariantCulture),
                                    DateTimeKind.Unspecified);
                                release.PublishDate = publishDate.ToLocalTime();
                            }
                            else if (line.StartsWith("Ano:"))
                            {
                                year = line.Substring("Ano: ".Length);
                            }
                            else if (line.StartsWith("Qualidade:"))
                            {
                                releaseQuality = line.Substring("Qualidade: ".Length);
                                if (releaseQuality == "WEB")
                                    releaseQuality = "WEB-DL";
                                extraInfo += releaseQuality + " ";
                            }
                            else
                            {
                                release.Description += line + "\n";
                                if (line.Contains(":"))
                                    if (!(line.StartsWith("Lançado") || line.StartsWith("Resolução") ||
                                          line.StartsWith("Idioma") || line.StartsWith("Autor")))
                                    {
                                        var info = line.Substring(line.IndexOf(": ", StringComparison.Ordinal) + 2);
                                        if (info == "Dual Áudio")
                                            info = "Dual";
                                        extraInfo += info + " ";
                                    }
                            }
                        }

                        if (Regex.IsMatch(extraInfo, "(Dual|[Nn]acional|[Dd]ublado)"))
                            extraInfo += " Brazilian";

                        var catStr = qCatLink.GetAttribute("href").Split('=')[1].Split('&')[0];

                        release.Title = ParseTitle(release.Title, seasonEp, year, catStr);

                        if (qQuality != null)
                        {
                            var quality = qQuality.TextContent;
                            release.Title += quality switch
                            {
                                "4K" => " 2160p",
                                "Full HD" => " 1080p",
                                "HD" => " 720p",
                                _ => " 480p"
                            };
                        }

                        release.Title += " " + extraInfo.TrimEnd();
                        release.Category = MapTrackerCatToNewznab(catStr);
                        release.Link = new Uri(SiteLink + qDlLink.GetAttribute("href"));
                        release.Details = new Uri(SiteLink + qDetailsLink.GetAttribute("href"));
                        release.Guid = release.Link;
                        release.Seeders = ParseUtil.CoerceInt(qSeeders.TextContent);
                        release.Peers = ParseUtil.CoerceInt(qLeechers.TextContent) + release.Seeders;
                        release.DownloadVolumeFactor = qFreeLeech != null ? 0 : 1;
                        release.UploadVolumeFactor = 1;
                        releases.Add(release);
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"{Id}: Error while parsing row '{row.OuterHtml}': {ex.Message}");
                    }
            }
            catch (Exception ex)
            {
                OnParseError(results.ContentString, ex);
            }

            return releases;
        }
    }
}
