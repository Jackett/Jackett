using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Jackett.Common.Extensions;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class BJShare : IndexerBase
    {
        public override string Id => "bjshare";
        public override string Name => "BJ-Share";
        public override string Description => "BJ-Share is a BRAZILIAN Private site";
        public override string SiteLink { get; protected set; } = "https://bj-share.info/";
        public override string[] LegacySiteLinks => new[]
        {
            "https://bj-share.me/"
        };
        public override string Language => "pt-BR";
        public override string Type => "private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private string BrowseUrl => SiteLink + "torrents.php";
        private string TodayUrl => SiteLink + "torrents.php?action=today";
        private static readonly Regex _EpisodeRegex = new Regex(@"(?:[SsEe]\d{2,4}){1,2}");
        private static readonly Regex _PagerPageRegex = new Regex(@"[?&]page=(\d+)", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

        // Hard cap on pages fetched per search; prevents runaway requests on very common terms.
        // Same pattern as EpubLibre.MaxSearchPageLimit.
        private const int MaxSearchPages = 6;

        // Selectors are kept as named constants so a future site change touches only this block.
        private const string SearchRowsSelector = "table.torrent_table > tbody > tr:not(tr.colhead)";
        private const string PagerLinksSelector = "div.linkbox a[href*=\"page=\"]";
        private const string EditionInfoSelector = ".edition_info";
        private const string DetailLinkPrimarySelector = "a[href*=\"torrentid=\"]:not(.tooltip)";
        private const string DetailLinkAnySelector = "a[href*=\"torrentid=\"]";
        private const string DetailLinkLegacyPrimarySelector = "a[href^=\"torrents.php?id=\"]:not(.tooltip)";
        private const string DetailLinkLegacyAnySelector = "a[href^=\"torrents.php?id=\"]";
        private const string SeriesLinkSelector = "a[href^=\"series.php?id=\"]";
        private const string CategoryLinkSelector = "a[href^=\"/torrents.php?filter_cat\"]";
        private const string DownloadLinkSelector = "a[href^=\"torrents.php?action=download\"]";
        private const string FreeLeechSelector = "strong[title=\"Free\"]";
        private const string DateTooltipSelector = "span.time.bjtooltip";
        private const string TorrentInfoSelector = "div.torrent_info";

        private new ConfigurationDataCookieUA configData => (ConfigurationDataCookieUA)base.configData;

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
            : base(configService: configService,
                    client: wc,
                    logger: l,
                    p: ps,
                    cacheService: cs,
                    configData: new ConfigurationDataCookieUA())
        {
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
                BookSearchParams = new List<BookSearchParam>
                {
                    BookSearchParam.Q
                }
            };

            caps.Categories.AddCategoryMapping(1, TorznabCatType.Movies, "Filmes");
            caps.Categories.AddCategoryMapping(2, TorznabCatType.TV, "Seriados");
            caps.Categories.AddCategoryMapping(3, TorznabCatType.PC, "Aplicativos");
            caps.Categories.AddCategoryMapping(4, TorznabCatType.PCGames, "Jogos");
            caps.Categories.AddCategoryMapping(5, TorznabCatType.BooksComics, "Mangás");
            caps.Categories.AddCategoryMapping(6, TorznabCatType.TV, "Vídeos de TV");
            caps.Categories.AddCategoryMapping(7, TorznabCatType.Other, "Outros");
            caps.Categories.AddCategoryMapping(8, TorznabCatType.TVSport, "Esportes");
            caps.Categories.AddCategoryMapping(9, TorznabCatType.BooksMags, "Revistas");
            caps.Categories.AddCategoryMapping(10, TorznabCatType.BooksEBook, "E-Books");
            caps.Categories.AddCategoryMapping(11, TorznabCatType.AudioAudiobook, "Audiobook");
            caps.Categories.AddCategoryMapping(12, TorznabCatType.BooksComics, "HQs");
            caps.Categories.AddCategoryMapping(13, TorznabCatType.TVOther, "Stand Up Comedy");
            caps.Categories.AddCategoryMapping(14, TorznabCatType.TVAnime, "Animes");
            caps.Categories.AddCategoryMapping(15, TorznabCatType.XXXImageSet, "Fotos Adultas");
            caps.Categories.AddCategoryMapping(16, TorznabCatType.TVOther, "Desenhos Animado");
            caps.Categories.AddCategoryMapping(17, TorznabCatType.TVDocumentary, "Documentários");
            caps.Categories.AddCategoryMapping(18, TorznabCatType.Other, "Cursos");
            caps.Categories.AddCategoryMapping(19, TorznabCatType.XXX, "Filmes Adultos");
            caps.Categories.AddCategoryMapping(20, TorznabCatType.XXXOther, "Jogos Adultos");
            caps.Categories.AddCategoryMapping(21, TorznabCatType.XXXOther, "Mangás Adultos");
            caps.Categories.AddCategoryMapping(22, TorznabCatType.XXXOther, "Animes Adultos");
            caps.Categories.AddCategoryMapping(23, TorznabCatType.XXXOther, "HQs Adultos");

            return caps;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            CookieHeader = configData.Cookie.Value;
            try
            {
                var results = await PerformQuery(new TorznabQuery());
                if (!results.Any())
                    throw new Exception("Found 0 results in the tracker");
                IsConfigured = true;
                SaveConfig();
                return IndexerConfigurationStatus.Completed;
            }
            catch (Exception e)
            {
                IsConfigured = false;
                throw new Exception("Your cookie did not work, make sure the user agent matches your computer: " + e.Message);
            }
        }

        private static string InternationalTitle(string title)
        {
            var match = Regex.Match(title, @".* \[(.*\/?)\]");
            return match.Success ? match.Groups[1].Value.Split('/')[0] : title;
        }

        private static string NationalTitle(string title)
        {
            var match = Regex.Match(title, @"(.*) \[.*\/?\]");
            return match.Success ? match.Groups[1].Value : title;
        }

        private static string AppendDescriptionToTitle(string baseTitle, string cleanDescription, string description)
        {
            var formattedTitle = baseTitle.Trim();
            if (!string.IsNullOrEmpty(cleanDescription))
            {
                var stringSeparators = new[]
                {
                    " / "
                };
                var titleElements = cleanDescription.Split(stringSeparators, StringSplitOptions.None);
                if (titleElements.Length < 6)
                    // Usually non movies / series could have less than 6 elements, eg: Books.
                    formattedTitle += " " + string.Join(" ", titleElements);
                else
                    formattedTitle += " " + titleElements[5] + " " + titleElements[3] + " " + titleElements[1] +
                                      " " + titleElements[2] + " " + titleElements[4] + " " +
                                      string.Join(" ", titleElements.Skip(6));
            }

            if (Regex.IsMatch(description, "(Dual|[Nn]acional|[Dd]ublado)"))
                formattedTitle += " Brazilian";

            return formattedTitle;
        }

        private static string StripSearchString(string term, bool isAnime)
        {
            // Search does not support searching with episode numbers so strip it if we have one
            // Ww AND filter the result later to archive the proper result
            term = _EpisodeRegex.Replace(term, string.Empty);
            term = isAnime ? Regex.Replace(term, @"\d*$", string.Empty) : term;
            return term.TrimEnd();
        }

        private string ParseTitle(string title, string seasonEp, string year, string categoryStr, bool international)
        {
            // Removes the SxxExx if it comes on the title
            var cleanTitle = _EpisodeRegex.Replace(title, string.Empty);
            // Removes the year if it comes on the title
            // The space is added because on daily releases the date will be XX/XX/YYYY
            if (!string.IsNullOrEmpty(year))
                cleanTitle = cleanTitle.Replace(" " + year, string.Empty);
            cleanTitle = Regex.Replace(cleanTitle, @"^\s*|[\s-]*$", string.Empty);

            // Get international title if available, or use the full title if not
            cleanTitle = international ? InternationalTitle(cleanTitle) : NationalTitle(cleanTitle);
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
                title = Regex.Replace(title, @"(Ep[\.]?[ ]?)|([S]\d\d[Ee])", "E");
                return title;
            }

            return title;
        }

        private bool IsSessionIsClosed(WebResult result) => result.IsRedirect && result.RedirectingTo.Contains("login.php");

        private string FixSearchTerm(TorznabQuery query) => _commonSearchTerms.Aggregate(
            query.GetQueryString(),
            (current, searchTerm) => current.ToLower().Replace(searchTerm.Key.ToLower(), searchTerm.Value));

        // if the search string is empty use the "last 24h torrents" view
        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query) =>
            query.SearchTerm.IsNullOrWhiteSpace() ? await ParseLast24HoursAsync() : await ParseUserSearchAsync(query);

        private async Task<List<ReleaseInfo>> ParseUserSearchAsync(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
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

            Dictionary<string, string> headers = null;
            if (!string.IsNullOrEmpty(configData.UserAgent.Value))
            {
                headers = new Dictionary<string, string>
                {
                    { "User-Agent", configData.UserAgent.Value }
                };
            }

            // BJ-Share returns ~50 rows per page (configurable per user profile, max 100).
            // Page 1 also exposes a pager which tells us the real total page count; we iterate
            // up to that, capped by MaxSearchPages as a safety net against very common queries.
            var totalPages = 1;
            var state = new SearchParseState { Query = query, SearchTerm = searchTerm };
            // HtmlParser is stateless; reuse a single instance across pages to avoid per-page allocations.
            var parser = new HtmlParser();

            for (var page = 1; page <= MaxSearchPages && page <= totalPages; page++)
            {
                var pageUrl = BuildSearchUrl(queryCollection, page);
                var results = await RequestWithCookiesAsync(pageUrl, headers: headers);
                if (IsSessionIsClosed(results))
                {
                    throw new Exception("The user is not logged in. It is possible that the cookie has expired or you " +
                                        "made a mistake when copying it. Please check the settings.");
                }

                try
                {
                    using var document = parser.ParseDocument(results.ContentString);

                    if (page == 1)
                        totalPages = Math.Min(MaxSearchPages, DiscoverTotalPages(document));

                    var rows = document.QuerySelectorAll(SearchRowsSelector);
                    if (rows.Length == 0)
                        break;

                    foreach (var row in rows)
                    {
                        try
                        {
                            var release = TryParseSearchRow(row, state);
                            if (release != null)
                                releases.Add(release);
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, $"{Id}: Error while parsing row '{row.OuterHtml}': {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Partial failure: surface the error and return whatever we have collected so far
                    // rather than dropping all earlier pages of valid results.
                    logger.Warn(ex, $"{Id}: Failed to parse page {page} of {totalPages}; returning {releases.Count} releases collected so far.");
                    OnParseError(results.ContentString, ex);
                    break;
                }

                // Honor TorznabQuery.Limit so we do not over-fetch when the caller (e.g. Sonarr) only
                // wants a small page of results.
                if (query.Limit > 0 && releases.Count >= query.Limit)
                    break;
            }

            return releases;
        }

        private string BuildSearchUrl(NameValueCollection parameters, int page)
        {
            var builder = new UriBuilder(BrowseUrl);
            var parts = new List<string>(parameters.Count + 1);
            foreach (string key in parameters)
            {
                var value = parameters[key] ?? string.Empty;
                parts.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}");
            }
            if (page > 1)
                parts.Add("page=" + page);
            builder.Query = string.Join("&", parts);
            return builder.Uri.ToString();
        }

        private static int DiscoverTotalPages(IHtmlDocument document)
        {
            var maxPage = 1;
            var pagerLinks = document.QuerySelectorAll(PagerLinksSelector);
            foreach (var link in pagerLinks)
            {
                var href = link.GetAttribute("href") ?? string.Empty;
                var match = _PagerPageRegex.Match(href);
                if (match.Success && int.TryParse(match.Groups[1].Value, out var pageNum) && pageNum > maxPage)
                    maxPage = pageNum;
            }
            return maxPage;
        }

        private ReleaseInfo TryParseSearchRow(IElement row, SearchParseState state)
        {
            // Skip sub-group info rows (e.g., "Dual Áudio" / "Legendado" section headers).
            if (row.QuerySelector(EditionInfoSelector) != null)
                return null;

            // Some torrents have multiple links to the details page; the one with the .tooltip class
            // is usually wrong, so try the non-tooltip selector first and fall back progressively.
            var qDetailsLink = row.QuerySelector(DetailLinkPrimarySelector)
                               ?? row.QuerySelector(DetailLinkAnySelector)
                               ?? row.QuerySelector(DetailLinkLegacyPrimarySelector)
                               ?? row.QuerySelector(DetailLinkLegacyAnySelector);
            if (qDetailsLink == null)
            {
                logger.Error($"{Id}: Error while parsing row '{row.OuterHtml}': Can't find the right details link");
                return null;
            }

            var title = StripSearchString(qDetailsLink.TextContent, false);

            // For TV/Anime series rows the show name lives in a separate <a href="series.php?id=...">
            // link; the details link only contains the season label (e.g. "S05"). Use the series link
            // text so the search filter matches the show name instead of the season label.
            var qSeriesLink = row.QuerySelector(SeriesLinkSelector);
            if (qSeriesLink != null)
            {
                var seriesName = qSeriesLink.TextContent.Trim();
                if (!string.IsNullOrEmpty(seriesName))
                    title = StripSearchString(seriesName, false);
            }

            var seasonEl = row.QuerySelector(DetailLinkAnySelector);
            string seasonEp = null;
            if (seasonEl != null)
            {
                var seasonMatch = _EpisodeRegex.Match(seasonEl.TextContent);
                seasonEp = seasonMatch.Success ? seasonMatch.Value : null;
            }
            seasonEp ??= _EpisodeRegex.Match(qDetailsLink.TextContent).Value;

            ICollection<int> category = null;
            string yearStr = null;
            if (row.ClassList.Contains("group") || row.ClassList.Contains("torrent"))
            {
                var qCatLink = row.QuerySelector(CategoryLinkSelector);
                var categoryHref = qCatLink?.GetAttribute("href");
                if (string.IsNullOrEmpty(categoryHref) || !categoryHref.Contains('='))
                {
                    logger.Error($"{Id}: Error while parsing row '{row.OuterHtml}': missing or malformed category link");
                    return null;
                }
                state.CategoryStr = categoryHref.Split('=')[1].Split('&')[0];
                category = MapTrackerCatToNewznab(state.CategoryStr);

                var torrentInfoEl = row.QuerySelector(TorrentInfoSelector);
                if (torrentInfoEl != null)
                    yearStr = torrentInfoEl.GetAttribute("data-year");
                yearStr ??= qDetailsLink.NextSibling.TextContent.Trim().TrimStart('[').TrimEnd(']');

                if (row.ClassList.Contains("group"))
                {
                    state.GroupCategory = category;
                    state.GroupTitle = title;
                    state.GroupYearStr = yearStr;
                    return null;
                }
            }

            var release = new ReleaseInfo
            {
                MinimumRatio = 1,
                MinimumSeedTime = 0
            };
            var qDlLink = row.QuerySelector(DownloadLinkSelector);
            var qSize = row.QuerySelector("td:nth-last-child(4)");
            var qGrabs = row.QuerySelector("td:nth-last-child(3)");
            var qSeeders = row.QuerySelector("td:nth-last-child(2)");
            var qLeechers = row.QuerySelector("td:nth-last-child(1)");
            var qFreeLeech = row.QuerySelector(FreeLeechSelector);
            var nationalTitle = "";
            if (row.ClassList.Contains("group_torrent"))
            {
                release.Description = Regex.Match(qDetailsLink.TextContent, @"\[.*?\]").Value;
                release.Title = ParseTitle(state.GroupTitle, seasonEp, state.GroupYearStr, state.CategoryStr, true);
                nationalTitle = ParseTitle(state.GroupTitle, seasonEp, state.GroupYearStr, state.CategoryStr, false);
                release.Category = state.GroupCategory;
            }
            else if (row.ClassList.Contains("torrent"))
            {
                release.Description = row.QuerySelector(TorrentInfoSelector).TextContent;
                release.Title = ParseTitle(title, seasonEp, yearStr, state.CategoryStr, true);
                nationalTitle = ParseTitle(title, seasonEp, yearStr, state.CategoryStr, false);
                release.Category = category;
            }

            release.Description = release.Description.Replace(" / Free", "");
            release.Description = release.Description.Replace("/ WEB ", "/ WEB-DL ");
            release.Description = release.Description.Replace("Full HD", "1080p");
            // Handles HDR conflict
            release.Description = release.Description.Replace("/ HD /", "/ 720p /");
            release.Description = release.Description.Replace("/ HD]", "/ 720p]");
            release.Description = release.Description.Replace("4K", "2160p");
            release.Description = release.Description.Replace("SD", "480p");
            release.Description = release.Description.Replace("Dual Áudio", "Dual");

            // Adjust the description so it can be read by Radarr/Sonarr.
            var cleanDescription = release.Description.Trim().TrimStart('[').TrimEnd(']');
            release.Title = AppendDescriptionToTitle(release.Title, cleanDescription, release.Description);
            nationalTitle = AppendDescriptionToTitle(nationalTitle, cleanDescription, release.Description);

            // Extract publish date from the time span tooltip (e.g., title="Feb 09 2026, 15:46").
            var dateStr = row.QuerySelector(DateTooltipSelector)?.GetAttribute("title");
            release.PublishDate = !string.IsNullOrWhiteSpace(dateStr) &&
                                  DateTime.TryParseExact(dateStr, "MMM dd yyyy, HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var publishDate)
                ? publishDate
                : DateTime.Today;

            // Drop rows whose parsed title doesn't actually contain the search term (BJ-Share's
            // search matches IMDb tags / Titan* prefix / etc., which we don't want surfacing).
            if (!state.Query.MatchQueryStringAND(release.Title, null, state.SearchTerm) &&
                !state.Query.MatchQueryStringAND(nationalTitle, null, state.SearchTerm))
                return null;

            release.Size = ParseUtil.GetBytes(qSize.TextContent);
            release.Link = new Uri(SiteLink + qDlLink.GetAttribute("href"));
            release.Details = new Uri(SiteLink + qDetailsLink.GetAttribute("href"));
            release.Guid = release.Link;
            release.Grabs = ParseUtil.CoerceLong(qGrabs.TextContent);
            release.Seeders = ParseUtil.CoerceInt(qSeeders.TextContent);
            release.Peers = ParseUtil.CoerceInt(qLeechers.TextContent) + release.Seeders;
            release.DownloadVolumeFactor = qFreeLeech != null ? 0 : 1;
            release.UploadVolumeFactor = 1;
            return release;
        }

        // Mutable per-search state shared across rows on a page (group accumulator + query context).
        private sealed class SearchParseState
        {
            public TorznabQuery Query { get; set; }
            public string SearchTerm { get; set; }
            public ICollection<int> GroupCategory { get; set; }
            public string GroupTitle { get; set; }
            public string GroupYearStr { get; set; }
            public string CategoryStr { get; set; } = string.Empty;
        }

        private async Task<List<ReleaseInfo>> ParseLast24HoursAsync()
        {
            var releases = new List<ReleaseInfo>();
            var results = await RequestWithCookiesAsync(TodayUrl);
            if (IsSessionIsClosed(results))
            {
                throw new Exception("The user is not logged in. It is possible that the cookie has expired or you " +
                                    "made a mistake when copying it. Please check the settings.");
            }

            try
            {
                const string rowsSelector = "table.torrent_table > tbody > tr:not(tr.colhead)";
                var searchResultParser = new HtmlParser();
                using var searchResultDocument = searchResultParser.ParseDocument(results.ContentString);
                var rows = searchResultDocument.QuerySelectorAll(rowsSelector);
                foreach (var row in rows)
                    try
                    {
                        var release = new ReleaseInfo
                        {
                            MinimumRatio = 1,
                            MinimumSeedTime = 0
                        };

                        var qDetailsLink = row.QuerySelector("a[href*=\"torrentid=\"]");
                        if (qDetailsLink == null)
                        {
                            logger.Error($"{Id}: Error while parsing row '{row.OuterHtml}': Can't find the details link");
                            continue;
                        }

                        var qCatLink = row.QuerySelector("a[href^=\"/torrents.php?filter_cat\"]");
                        var qDlLink = row.QuerySelector("a[href^=\"torrents.php?action=download\"]");
                        var qSeeders = row.QuerySelector("td:nth-last-child(2)");
                        var qLeechers = row.QuerySelector("td:nth-last-child(1)");
                        var qFreeLeech = row.QuerySelector("strong[title=\"Free\"]");
                        var qSize = row.QuerySelector("td.number_column.nobr");

                        // Title from the details link
                        release.Title = qDetailsLink.TextContent.Trim();
                        var seasonEp = _EpisodeRegex.Match(release.Title).Value;

                        // Year from text after title link (e.g., " [2026]" inside the <strong> wrapper)
                        var year = "";
                        var yearNode = qDetailsLink.NextSibling;
                        if (yearNode != null)
                        {
                            var yearMatch = Regex.Match(yearNode.TextContent, @"\[(\d{4})\]");
                            if (yearMatch.Success)
                                year = yearMatch.Groups[1].Value;
                        }

                        // Category from data-categoryid attribute on the row, or from cat link
                        var catStr = row.GetAttribute("data-categoryid");
                        if (string.IsNullOrEmpty(catStr) && qCatLink != null)
                            catStr = qCatLink.GetAttribute("href").Split('=')[1].Split('&')[0];

                        // Description from bracket info (e.g., "[MKV / H.264 / WEB-DL / ...]")
                        release.Description = "";
                        var groupInfo = row.QuerySelector("div.group_info");
                        if (groupInfo != null)
                        {
                            var groupInfoText = groupInfo.TextContent;
                            // Match bracket content containing " / " (space-slash-space) — this is the format/quality info
                            // This avoids matching title brackets like [Subtitle] or year brackets like [2026]
                            var descMatch = Regex.Match(groupInfoText, @"\[([^\]]* / [^\]]*)\]");
                            if (descMatch.Success)
                                release.Description = descMatch.Groups[1].Value.Trim();
                        }

                        // Process description similar to search method
                        release.Description = release.Description.Replace(" / Free", "");
                        release.Description = release.Description.Replace("/ WEB ", "/ WEB-DL ");
                        if (release.Description.EndsWith("/ WEB"))
                            release.Description = release.Description.Substring(0, release.Description.Length - 3) + "WEB-DL";
                        release.Description = release.Description.Replace("Full HD", "1080p");
                        release.Description = release.Description.Replace("/ HD /", "/ 720p /");
                        release.Description = Regex.Replace(release.Description, @"/ HD$", "/ 720p");
                        release.Description = release.Description.Replace("4K", "2160p");
                        release.Description = release.Description.Replace("SD", "480p");
                        release.Description = release.Description.Replace("Dual Áudio", "Dual");

                        // Build title
                        release.Title = ParseTitle(release.Title, seasonEp, year, catStr ?? "", true);

                        // Append description elements to title
                        var cleanDescription = release.Description.Trim().TrimStart('[').TrimEnd(']');
                        if (!string.IsNullOrEmpty(cleanDescription))
                        {
                            var stringSeparators = new[] { " / " };
                            var titleElements = cleanDescription.Split(stringSeparators, StringSplitOptions.None);
                            release.Title = release.Title.Trim();
                            if (titleElements.Length < 6)
                                release.Title += " " + string.Join(" ", titleElements);
                            else
                                release.Title += " " + titleElements[5] + " " + titleElements[3] + " " + titleElements[1] + " " +
                                                 titleElements[2] + " " + titleElements[4] + " " + string.Join(
                                                     " ", titleElements.Skip(6));
                        }

                        if (Regex.IsMatch(release.Description, "(Dual|[Nn]acional|[Dd]ublado)"))
                            release.Title += " Brazilian";

                        // Extract publish date from the time span tooltip (e.g., title="Feb 09 2026, 15:46")
                        var dateStr = row.QuerySelector("span.time.bjtooltip")?.GetAttribute("title");

                        if (!string.IsNullOrWhiteSpace(dateStr) &&
                            DateTime.TryParseExact(dateStr, "MMM dd yyyy, HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var publishDate))
                        {
                            release.PublishDate = publishDate;
                        }
                        else
                        {
                            release.PublishDate = DateTime.Today;
                        }

                        if (qSize != null)
                            release.Size = ParseUtil.GetBytes(qSize.TextContent);

                        release.Category = MapTrackerCatToNewznab(catStr ?? "");
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
                        logger.Error(ex, $"{Id}: Error while parsing row '{row.OuterHtml}': {ex.Message}");
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
