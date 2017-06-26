using Jackett.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Jackett.Models
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
        public string ImdbID { get; set; }

        public int Season { get; set; }
        public string Episode { get; set; }
        public string SearchTerm { get; set; }

        public bool IsTest { get; set; }

        public string ImdbIDShort { get { return (ImdbID != null ? ImdbID.TrimStart('t') : null); } }

        protected string[] QueryStringParts = null;

        public bool IsSearch
        {
            get
            {
                return (QueryStringParts != null && QueryStringParts.Length > 0);
            }
        }

        public bool IsTVSearch
        {
            get
            {
                return QueryType == "tvsearch";
            }
        }

        public bool IsMovieSearch
        {
            get
            {
                return QueryType == "movie";
            }
        }

        public bool IsTVRageSearch
        {
            get
            {
                return RageID != null;
            }
        }

        public bool IsImdbQuery
        {
            get
            {
                return ImdbID != null;
            }
        }

        public bool HasSpecifiedCategories
        {
            get
            {
                return (Categories != null && Categories.Length > 0);
            }
        }

        public string SanitizedSearchTerm
        {
            get
            {
                if (SearchTerm == null)
                    return string.Empty;

                char[] arr = SearchTerm.ToCharArray();

                arr = Array.FindAll<char>(arr, c => (char.IsLetterOrDigit(c)
                                                  || char.IsWhiteSpace(c)
                                                  || c == '-'
                                                  || c == '.'
                                                  || c == '_'
                                                  || c == '('
                                                  || c == ')'
                                                  || c == '@'
                                                  || c == '\''
                                                  || c == '['
                                                  || c == ']'
                                                  ));
                var safetitle = new string(arr);
                return safetitle;
            }
        }

        public TorznabQuery()
        {
            Categories = new int[0];
            IsTest = false;
        }

        public TorznabQuery CreateFallback(string search) {
            var ret = Clone();
            if (Categories == null || Categories.Length == 0) {
                ret.Categories = new int[]{ TorznabCatType.Movies.ID,
                                            TorznabCatType.MoviesForeign.ID,
                                            TorznabCatType.MoviesOther.ID,
                                            TorznabCatType.MoviesSD.ID,
                                            TorznabCatType.MoviesHD.ID,
                                            TorznabCatType.Movies3D.ID,
                                            TorznabCatType.MoviesBluRay.ID,
						                    TorznabCatType.MoviesDVD.ID,
						                    TorznabCatType.MoviesWEBDL.ID,
                };
            }
            ret.SearchTerm = search;

            return ret;
        }

        public TorznabQuery Clone() {
            var ret = new TorznabQuery();
            ret.QueryType = QueryType;
            if (Categories != null && Categories.Length > 0) {
                ret.Categories = new int [Categories.Length];
                Array.Copy (Categories, ret.Categories, Categories.Length);
            }
            ret.Extended = Extended;
            ret.ApiKey = ApiKey;
            ret.Limit = Limit;
            ret.Offset = Offset;
            ret.Season = Season;
            ret.Episode = Episode;
            ret.SearchTerm = SearchTerm;
            ret.IsTest = IsTest;
            if (QueryStringParts != null && QueryStringParts.Length > 0) {
                ret.QueryStringParts = new string [QueryStringParts.Length];
                Array.Copy (QueryStringParts, ret.QueryStringParts, QueryStringParts.Length);
            }

            return ret;
        }

        public string GetQueryString()
        {
            return (SanitizedSearchTerm + " " + GetEpisodeSearchString()).Trim();
        }

        // Some trackers don't support AND logic for search terms resulting in unwanted results.
        // Using this method we can AND filter it within jackett.
        // With limit we can limit the amount of characters which should be compared (use it if a tracker doesn't return the full title).
        public bool MatchQueryStringAND(string title, int? limit = null, string queryStringOverride = null)
        {
            // We cache the regex split results so we have to do it only once for each query.
            if (QueryStringParts == null)
            {
                var queryString = GetQueryString();
                if (queryStringOverride != null)
                    queryString = queryStringOverride;
                if (limit != null && limit > 0)
                {
                    if (limit > queryString.Length)
                        limit = queryString.Length;
                    queryString = queryString.Substring(0, (int)limit);
                }
                Regex SplitRegex = new Regex("[^a-zA-Z0-9]+");
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
            DateTime showDate;
            if (DateTime.TryParseExact(string.Format("{0} {1}", Season, Episode), "yyyy MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out showDate))
                episodeString = showDate.ToString("yyyy.MM.dd");
            else if (string.IsNullOrEmpty(Episode))
                episodeString = string.Format("S{0:00}", Season);
            else
            {
                try
                {
                    episodeString = string.Format("S{0:00}E{1:00}", Season, ParseUtil.CoerceInt(Episode));
                } catch (FormatException) // e.g. seaching for S01E01A
                {
                    episodeString = string.Format("S{0:00}E{1}", Season, Episode);
                }
                
            }
            return episodeString;
        }

        public static TorznabQuery FromHttpQuery(NameValueCollection query)
        {

            //{t=tvsearch&cat=5030%2c5040&extended=1&apikey=test&offset=0&limit=100&rid=24493&season=5&ep=1}
            var q = new TorznabQuery();
            q.QueryType = query["t"];

            if (query["q"] == null)
            {
                q.SearchTerm = string.Empty;
            }
            else
            {
                q.SearchTerm = query["q"];
            }

            if (query["cat"] != null)
            {
                q.Categories = query["cat"].Split(',').Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => int.Parse(s)).ToArray();
            }else
            {
                if (q.QueryType == "movie" && string.IsNullOrWhiteSpace(query["imdbid"]))
                    q.Categories = new int[] { TorznabCatType.Movies.ID };
                else
                    q.Categories = new int[0];
            }

            if (query["extended"] != null)
            {
                q.Extended = ParseUtil.CoerceInt(query["extended"]);
            }
            q.ApiKey = query["apikey"];
            if (query["limit"] != null)
            {
                q.Limit = ParseUtil.CoerceInt(query["limit"]);
            }
            if (query["offset"] != null)
            {
                q.Offset = ParseUtil.CoerceInt(query["offset"]);
            }

            q.ImdbID = query["imdbid"];

            int rageId;
            if (int.TryParse(query["rid"], out rageId))
            {
                q.RageID = rageId;
            }

            int season;
            if (int.TryParse(query["season"], out season))
            {
                q.Season = season;
            }

            q.Episode = query["ep"];

            return q;
        }

        public void ExpandCatsToSubCats()
        {
            if (Categories.Count() == 0)
                return;
            var newCatList = new List<int>();
            newCatList.AddRange(Categories);
            foreach (var cat in Categories)
            {
                var majorCat = TorznabCatType.AllCats.Where(c => c.ID == cat).FirstOrDefault();
                // If we search for TV we should also search for all sub cats
                if (majorCat != null)
                {
                    newCatList.AddRange(majorCat.SubCategories.Select(s => s.ID));
                }
            }

            Categories = newCatList.Distinct().ToArray();
        }
    }
}
