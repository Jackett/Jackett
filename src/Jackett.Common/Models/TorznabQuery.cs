using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Jackett.Common.Utils;

namespace Jackett.Common.Models
{
    public class TorznabQuery
    {
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
        public bool Cache { get; set; } = true;

        public int Season { get; set; }
        public string Episode { get; set; }
        public string SearchTerm { get; set; }

        public string Album { get; set; }
        public string Artist { get; set; }
        public string Label { get; set; }
        public string Track { get; set; }
        public int? Year { get; set; }
        public ICollection<string> Genre { get; set; }

        public string Author { get; set; }
        public string Title { get; set; }

        public bool IsTest { get; set; }

        public string ImdbIDShort => ImdbID?.TrimStart('t');

        protected string[] QueryStringParts;

        public bool IsSearch => QueryType == "search";

        public bool IsTVSearch => QueryType == "tvsearch";

        public bool IsMovieSearch => QueryType == "movie" || (QueryType == "TorrentPotato" && !string.IsNullOrWhiteSpace(SearchTerm));

        public bool IsMusicSearch => QueryType == "music";

        public bool IsBookSearch => QueryType == "book";

        public bool IsTVRageSearch => RageID != null;

        public bool IsTvdbSearch => TvdbID != null;

        public bool IsImdbQuery => ImdbID != null;

        public bool IsTmdbQuery => TmdbID != null;

        public bool HasSpecifiedCategories => (Categories != null && Categories.Length > 0);

        public string SanitizedSearchTerm
        {
            get
            {
                var term = SearchTerm;
                if (SearchTerm == null)
                    term = "";
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
                                               ));
                return string.Concat(safeTitle);
            }
        }

        public TorznabQuery()
        {
            Categories = new int[0];
            IsTest = false;
        }

        public TorznabQuery CreateFallback(string search)
        {
            var ret = Clone();
            if (Categories == null || Categories.Length == 0)
            {
                ret.Categories = new int[]{ TorznabCatType.Movies.ID,
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
                Year = Year,
                Author = Author,
                Title = Title,
                RageID = RageID,
                TvdbID = TvdbID,
                ImdbID = ImdbID,
                TmdbID = TmdbID,
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
        // With limit we can limit the amount of characters which should be compared (use it if a tracker doesn't return the full title).
        public bool MatchQueryStringAND(string title, int? limit = null, string queryStringOverride = null)
        {
            // We cache the regex split results so we have to do it only once for each query.
            if (QueryStringParts == null)
            {
                var queryString = !string.IsNullOrWhiteSpace(queryStringOverride) ? queryStringOverride : GetQueryString();

                if (limit != null && limit > 0)
                {
                    if (limit > queryString.Length)
                        limit = queryString.Length;
                    queryString = queryString.Substring(0, (int)limit);
                }
                var SplitRegex = new Regex("[^a-zA-Z0-9]+");
                QueryStringParts = SplitRegex.Split(queryString);
            }

            // Check if each part of the query string is in the given title.
            foreach (var QueryStringPart in QueryStringParts)
            {
                if (title.IndexOf(QueryStringPart, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return false;
                }
            }
            return true;
        }

        public string GetEpisodeSearchString()
        {
            if (Season == 0)
                return string.Empty;

            string episodeString;
            if (DateTime.TryParseExact(string.Format("{0} {1}", Season, Episode), "yyyy MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var showDate))
                episodeString = showDate.ToString("yyyy.MM.dd");
            else if (string.IsNullOrEmpty(Episode))
                episodeString = string.Format("S{0:00}", Season);
            else
            {
                try
                {
                    episodeString = string.Format("S{0:00}E{1:00}", Season, ParseUtil.CoerceInt(Episode));
                }
                catch (FormatException) // e.g. seaching for S01E01A
                {
                    episodeString = string.Format("S{0:00}E{1}", Season, Episode);
                }

            }
            return episodeString;
        }
    }
}
