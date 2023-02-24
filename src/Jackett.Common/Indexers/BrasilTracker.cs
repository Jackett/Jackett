using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
    public class BrasilTracker : BaseWebIndexer
    {
        private string LoginUrl => SiteLink + "login.php";
        private string BrowseUrl => SiteLink + "torrents.php";
        private static readonly Regex _EpisodeRegex = new Regex(@"(?:[SsEe]\d{2,4}){1,2}");

        private new ConfigurationDataBasicLogin configData => (ConfigurationDataBasicLogin)base.configData;

        public BrasilTracker(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "brasiltracker",
                    name: "BrasilTracker",
                    description: "BrasilTracker is a BRAZILIAN Private Torrent Tracker for MOVIES / TV / GENERAL",
                    link: "https://brasiltracker.org/",
                    caps: new TorznabCapabilities
                    {
                        TvSearchParams = new List<TvSearchParam>
                        {
                            TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.Genre
                        },
                        MovieSearchParams = new List<MovieSearchParam>
                        {
                            MovieSearchParam.Q, MovieSearchParam.ImdbId, MovieSearchParam.Genre
                        },
                        MusicSearchParams = new List<MusicSearchParam>
                        {
                            MusicSearchParam.Q, MusicSearchParam.Genre
                        },
                        BookSearchParams = new List<BookSearchParam>
                        {
                            BookSearchParam.Q, BookSearchParam.Genre
                        }
                    },
                    configService: configService,
                    client: wc,
                    logger: l,
                    p: ps,
                    cacheService: cs,
                    configData: new ConfigurationDataBasicLogin("BrasilTracker does not return categories in its search results.</br>To add to your Apps' Torznab indexer, replace all categories with 8000(Other).</br>For best results, change the <b>Torrents per page:</b> setting to <b>100</b> on your account profile."))
        {
            Encoding = Encoding.UTF8;
            Language = "pt-BR";
            Type = "private";
            AddCategoryMapping(1, TorznabCatType.Other, "Other");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string>
            {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "keeplogged", "1" },
                { "login", "Log in" }
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, LoginUrl, true);
            await ConfigureIfOK(result.Cookies, result.ContentString?.Contains("logout.php") == true, () =>
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(result.ContentString);
                var errorMessage = dom.QuerySelector("form#loginform").TextContent.Trim();
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        private static string InternationalTitle(string title)
        {
            var match = Regex.Match(title, @".* \[(.*\/?)\]");
            return match.Success ? match.Groups[1].Value.Split('/')[0] : title;
        }

        private static string StripSearchString(string term)
        {
            // Search does not support searching with episode numbers so strip it if we have one
            // we will AND filter the result later to archive the proper result
            term = _EpisodeRegex.Replace(term, string.Empty);
            return term.TrimEnd();
        }

        private string ParseTitle(string title, string seasonEp, string year)
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
            cleanTitle += " " + year + " " + seasonEp;
            cleanTitle = cleanTitle.Trim();
            return cleanTitle;
        }
        private string FixSearchTerm(TorznabQuery query)
        {
            if (query.IsImdbQuery)
                return query.ImdbID;
            return query.GetQueryString();
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchUrl = BrowseUrl;
            var searchTerm = FixSearchTerm(query);
            var queryCollection = new NameValueCollection
            {
                {"searchstr", StripSearchString(searchTerm)},
                {"order_by", "time"},
                {"order_way", "desc"},
                {"group_results", "1"},
                {"action", "basic"},
                {"searchsubmit", "1"}
            };
            if (query.IsGenreQuery)
                queryCollection.Add("taglist", query.Genre);

            searchUrl += "?" + queryCollection.GetQueryString();
            var results = await RequestWithCookiesAsync(searchUrl);
            try
            {
                const string rowsSelector = "table.torrent_table > tbody > tr:not(tr.colhead)";
                var searchResultParser = new HtmlParser();
                var searchResultDocument = searchResultParser.ParseDocument(results.ContentString);
                var rows = searchResultDocument.QuerySelectorAll(rowsSelector);
                string groupTitle = null;
                string groupYearStr = null;
                Uri groupPoster = null;
                string imdbLink = null;
                string tmdbLink = null;
                string genres = null;
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
                        var title = StripSearchString(qDetailsLink.TextContent);

                        var seasonEl = row.QuerySelector("a[href^=\"torrents.php?torrentid=\"]");
                        string seasonEp = null;
                        if (seasonEl != null)
                        {
                            var seasonMatch = _EpisodeRegex.Match(seasonEl.TextContent);
                            seasonEp = seasonMatch.Success ? seasonMatch.Value : null;
                        }
                        seasonEp ??= _EpisodeRegex.Match(qDetailsLink.TextContent).Value;

                        ICollection<int> category = new List<int> { TorznabCatType.Other.ID };
                        string yearStr = null;
                        if (row.ClassList.Contains("group") || row.ClassList.Contains("torrent")) // group/ungrouped headers
                        {
                            var qCatLink = row.QuerySelector("a[href^=\"/torrents.php?filter_cat\"]");

                            var torrentInfoEl = row.QuerySelector("div.torrent_info");
                            if (torrentInfoEl != null)
                            {
                                // valid for torrent grouped but that has only 1 episode yet
                                yearStr = torrentInfoEl.GetAttribute("data-year");
                            }
                            yearStr ??= qDetailsLink.NextSibling.TextContent.Trim().TrimStart('[').TrimEnd(']');

                            if (Uri.TryCreate(row.QuerySelector("img[alt=\"Cover\"]")?.GetAttribute("src"),
                                              UriKind.Absolute, out var posterUri))
                                groupPoster = posterUri;
                            if (row.ClassList.Contains("group")) // group headers
                            {
                                groupTitle = title;
                                groupYearStr = yearStr;
                                imdbLink = row.QuerySelector("a[href*=\"imdb.com/title/tt\"]")?.GetAttribute("href");
                                tmdbLink = row.QuerySelector("a[href*=\"themoviedb.org/\"]")?.GetAttribute("href");
                                genres = row.QuerySelector("div.tags")?.TextContent;
                                continue;
                            }
                        }

                        var release = new ReleaseInfo
                        {
                            MinimumRatio = 1,
                            MinimumSeedTime = 172800
                        };
                        var qDlLink = row.QuerySelector("a[href^=\"torrents.php?action=download\"]");
                        var qSize = row.QuerySelector("td:nth-last-child(4)");
                        var qGrabs = row.QuerySelector("td:nth-last-child(3)");
                        var qSeeders = row.QuerySelector("td:nth-last-child(2)");
                        var qLeechers = row.QuerySelector("td:nth-last-child(1)");
                        var qFreeLeech = row.QuerySelector("strong[title=\"Free\"]");
                        if (row.ClassList.Contains("group_torrent")) // torrents belonging to a group
                        {
                            release.Description = qDetailsLink.TextContent;
                            release.Title = ParseTitle(groupTitle, seasonEp, groupYearStr);
                        }
                        else if (row.ClassList.Contains("torrent")) // standalone/un grouped torrents
                        {
                            release.Description = row.QuerySelector("div.torrent_info").TextContent;
                            release.Title = ParseTitle(title, seasonEp, yearStr);
                            imdbLink = row.QuerySelector("a[href*=\"imdb.com/title/tt\"]")?.GetAttribute("href");
                            tmdbLink = row.QuerySelector("a[href*=\"themoviedb.org/\"]")?.GetAttribute("href");
                            genres = row.QuerySelector("div.tags")?.TextContent;
                        }
                        release.Poster = groupPoster;
                        release.Imdb = ParseUtil.GetLongFromString(imdbLink);
                        release.TMDb = ParseUtil.GetLongFromString(tmdbLink);
                        if (!string.IsNullOrEmpty(genres))
                        {
                            if (release.Genres == null)
                                release.Genres = new List<string>();
                            release.Genres = release.Genres.Union(genres.Replace(", ", ",").Split(',')).ToList();
                        }
                        release.Category = category;
                        release.Description = release.Description.Replace(" / Free", ""); // Remove Free Tag
                        release.Description = release.Description.Replace("/ WEB ", "/ WEB-DL "); // Fix web/web-dl
                        release.Description = release.Description.Replace("Full HD", "1080p");
                        // Handles HDR conflict
                        release.Description = release.Description.Replace("/ HD /", "/ 720p /");
                        release.Description = release.Description.Replace("/ HD]", "/ 720p]");
                        release.Description = release.Description.Replace("4K", "2160p");
                        release.Description = release.Description.Replace("SD", "480p");
                        release.Description = release.Description.Replace("Dual Áudio", "Dual");
                        release.Description = release.Description.Replace("Dual Audio", "Dual");

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
