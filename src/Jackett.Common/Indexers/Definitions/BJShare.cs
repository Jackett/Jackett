using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
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
            // Keep the full title including both Portuguese and English versions
            // This allows searches in both languages to work correctly
            return title;
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

            Dictionary<string, string> headers = null;

            if (!string.IsNullOrEmpty(configData.UserAgent.Value))
            {
                headers = new Dictionary<string, string>
                {
                    { "User-Agent", configData.UserAgent.Value }
                };
            }

            foreach (var cat in MapTorznabCapsToTrackers(query))
            {
                queryCollection.Add("filter_cat[" + cat + "]", "1");
            }

            searchUrl += "?" + queryCollection.GetQueryString();
            var results = await RequestWithCookiesAsync(searchUrl, headers: headers);
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
                        var qDetailsLink = row.QuerySelector("a[href^=\"torrents.php?torrentid=\"]:not(.tooltip)");
                        if (qDetailsLink == null)
                            qDetailsLink = row.QuerySelector("a[href^=\"torrents.php?torrentid=\"]");
                        // fallback to old id= format for backward compatibility
                        if (qDetailsLink == null)
                            qDetailsLink = row.QuerySelector("a[href^=\"torrents.php?id=\"]:not(.tooltip)");
                        if (qDetailsLink == null)
                            qDetailsLink = row.QuerySelector("a[href^=\"torrents.php?id=\"]");
                        if (qDetailsLink == null)
                        {
                            logger.Error($"{Id}: Error while parsing row '{row.OuterHtml}': Can't find the right details link");
                            continue;
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

                        // check for previously stripped search terms
                        if (!query.MatchQueryStringAND(release.Title, null, searchTerm))
                        {
                            continue;
                        }

                        var size = qSize.TextContent;
                        release.Size = ParseUtil.GetBytes(size);
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
                        logger.Error(ex, $"{Id}: Error while parsing row '{row.OuterHtml}': {ex.Message}");
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

                        // Details link - site changed from a.BJinfoBox to standard torrentid links
                        var qDetailsLink = row.QuerySelector("a[href^=\"torrents.php?torrentid=\"]");
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
                        release.Title = ParseTitle(release.Title, seasonEp, year, catStr ?? "");

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
