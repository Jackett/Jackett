using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Jackett.Common.Extensions;
using Jackett.Common.Utils;
using Newtonsoft.Json;

namespace Jackett.Common.Models
{
    public class TorznabQuery
    {
        private static readonly Regex _StandardizeDashesRegex = new Regex(@"\p{Pd}+", RegexOptions.Compiled);
        private static readonly Regex _StandardizeSingleQuotesRegex = new Regex(@"[\u0060\u00B4\u2018\u2019]", RegexOptions.Compiled);

        public bool InteractiveSearch { get; set; }
        public string QueryType { get; set; }
        public int[] Categories { get; set; }
        public int Extended { get; set; }
        public string ApiKey { get; set; }
        public int Limit { get; set; }
        public int Offset { get; set; }
        public int? RageID { get; set; }
        public int? TvdbID { get; set; }
        public string ImdbID { get; set; }
        public int? TmdbID { get; set; }
        public int? TvmazeID { get; set; }
        public int? TraktID { get; set; }
        public int? DoubanID { get; set; }

        [JsonIgnore]
        public bool Cache { get; set; } = true;

        public int? Season { get; set; }
        public string Episode { get; set; }
        public string SearchTerm { get; set; }

        public string Album { get; set; }
        public string Artist { get; set; }
        public string Label { get; set; }
        public string Track { get; set; }
        public int? Year { get; set; }
        public string Genre { get; set; }

        public string Author { get; set; }
        public string Title { get; set; }
        public string Publisher { get; set; }

        public bool IsTest { get; set; }

        public string ImdbIDShort => ImdbID?.TrimStart('t');

        protected string[] QueryStringParts;

        public bool IsSearch => QueryType == "search";

        public bool IsTVSearch => QueryType == "tvsearch";

        public bool IsMovieSearch => QueryType == "movie" || (QueryType == "TorrentPotato" && !string.IsNullOrWhiteSpace(SearchTerm));

        public bool IsMusicSearch => QueryType == "music";

        public bool IsBookSearch => QueryType == "book";

        public bool IsTVRageQuery => RageID != null;

        public bool IsTvdbQuery => TvdbID != null;

        public bool IsImdbQuery => ImdbID != null;

        public bool IsTmdbQuery => TmdbID != null;

        public bool IsTvmazeQuery => TvmazeID != null;

        public bool IsTraktQuery => TraktID != null;

        public bool IsDoubanQuery => DoubanID != null;

        public bool IsGenreQuery => Genre != null;

        public bool IsRssSearch =>
            SearchTerm.IsNullOrWhiteSpace() &&
            !IsIdSearch;

        public bool IsIdSearch =>
            Episode.IsNotNullOrWhiteSpace() ||
            Season > 0 ||
            IsImdbQuery ||
            IsTvdbQuery ||
            IsTVRageQuery ||
            IsTraktQuery ||
            IsTvmazeQuery ||
            IsTmdbQuery ||
            IsDoubanQuery ||
            Album.IsNotNullOrWhiteSpace() ||
            Artist.IsNotNullOrWhiteSpace() ||
            Label.IsNotNullOrWhiteSpace() ||
            Genre.IsNotNullOrWhiteSpace() ||
            Track.IsNotNullOrWhiteSpace() ||
            Author.IsNotNullOrWhiteSpace() ||
            Title.IsNotNullOrWhiteSpace() ||
            Publisher.IsNotNullOrWhiteSpace() ||
            Year.HasValue;

        public bool HasSpecifiedCategories => Categories is { Length: > 0 };

        public string SanitizedSearchTerm
        {
            get
            {
                var term = SearchTerm ?? "";

                term = _StandardizeDashesRegex.Replace(term, "-");
                term = _StandardizeSingleQuotesRegex.Replace(term, "'");

                var safeTitle = term.Where(c => (char.IsLetterOrDigit(c)
                                                 || char.IsWhiteSpace(c)
                                                 || c == '-'
                                                 || c == '.'
                                                 || c == '_'
                                                 || c == '('
                                                 || c == ')'
                                                 || c == '@'
                                                 || c == '/'
                                                 || c == '\''
                                                 || c == '['
                                                 || c == ']'
                                                 || c == '+'
                                                 || c == '%'
                                                 || c == ':'
                                               ));

                return string.Concat(safeTitle);
            }
        }

        public TorznabQuery()
        {
            Categories = Array.Empty<int>();
            IsTest = false;
        }

        public TorznabQuery CreateFallback(string search)
        {
            var ret = Clone();
            if (Categories == null || Categories.Length == 0)
            {
                ret.Categories = new[]
                {
                    TorznabCatType.Movies.ID,
                    TorznabCatType.MoviesForeign.ID,
                    TorznabCatType.MoviesOther.ID,
                    TorznabCatType.MoviesSD.ID,
                    TorznabCatType.MoviesHD.ID,
                    TorznabCatType.Movies3D.ID,
                    TorznabCatType.MoviesBluRay.ID,
                    TorznabCatType.MoviesDVD.ID,
                    TorznabCatType.MoviesWEBDL.ID,
                    TorznabCatType.MoviesUHD.ID,
                };
            }
            ret.SearchTerm = search;

            return ret;
        }

        public TorznabQuery Clone()
        {
            var ret = new TorznabQuery
            {
                InteractiveSearch = InteractiveSearch,
                QueryType = QueryType,
                Extended = Extended,
                ApiKey = ApiKey,
                Limit = Limit,
                Offset = Offset,
                Season = Season,
                Episode = Episode,
                SearchTerm = SearchTerm,
                IsTest = IsTest,
                Album = Album,
                Artist = Artist,
                Label = Label,
                Track = Track,
                Genre = Genre,
                Year = Year,
                Author = Author,
                Title = Title,
                Publisher = Publisher,
                RageID = RageID,
                TvdbID = TvdbID,
                ImdbID = ImdbID,
                TmdbID = TmdbID,
                TvmazeID = TvmazeID,
                TraktID = TraktID,
                DoubanID = DoubanID,
                Cache = Cache
            };
            if (Categories?.Length > 0)
            {
                ret.Categories = new int[Categories.Length];
                Array.Copy(Categories, ret.Categories, Categories.Length);
            }
            if (QueryStringParts?.Length > 0)
            {
                ret.QueryStringParts = new string[QueryStringParts.Length];
                Array.Copy(QueryStringParts, ret.QueryStringParts, QueryStringParts.Length);
            }

            return ret;
        }

        public string GetQueryString() => (SanitizedSearchTerm + " " + GetEpisodeSearchString()).Trim();

        // Some trackers don't support AND logic for search terms resulting in unwanted results.
        // Using this method we can AND filter it within jackett.
        // With "limit" we can limit the amount of characters which should be compared (use it if a tracker doesn't return the full title).
        public bool MatchQueryStringAND(string title, int? limit = null, string queryStringOverride = null)
        {
            var commonWords = new[] { "and", "the", "an" };

            // We cache the regex split results so we have to do it only once for each query.
            if (QueryStringParts == null)
            {
                var queryString = !string.IsNullOrWhiteSpace(queryStringOverride) ? queryStringOverride : GetQueryString();

                if (limit is > 0)
                {
                    if (limit > queryString.Length)
                    {
                        limit = queryString.Length;
                    }

                    queryString = queryString.Substring(0, (int)limit);
                }

                var splitRegex = new Regex("[^\\w]+");

                QueryStringParts = splitRegex.Split(queryString).Where(p => !string.IsNullOrWhiteSpace(p) && p.Length > 1 && !commonWords.ContainsIgnoreCase(p)).ToArray();
            }

            // Check if each part of the query string is in the given title.
            return QueryStringParts.All(title.ContainsIgnoreCase);
        }

        public string GetEpisodeSearchString()
        {
            if (Season == null || Season == 0)
            {
                return string.Empty;
            }

            string episodeString;
            if (DateTime.TryParseExact($"{Season} {Episode}", "yyyy MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var showDate))
            {
                episodeString = showDate.ToString("yyyy.MM.dd", CultureInfo.InvariantCulture);
            }
            else if (Episode.IsNullOrWhiteSpace())
            {
                episodeString = $"S{Season:00}";
            }
            else
            {
                try
                {
                    episodeString = $"S{Season:00}E{ParseUtil.CoerceInt(Episode):00}";
                }
                catch (FormatException) // e.g. seaching for S01E01A
                {
                    episodeString = $"S{Season:00}E{Episode}";
                }
            }

            return episodeString;
        }
    }
}
