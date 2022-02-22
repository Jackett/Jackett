using System.Text.RegularExpressions;
using Jackett.Common.Utils;

namespace Jackett.Common.Models.DTO
{
    public class ApiSearch
    {
        public string Query { get; set; }
        public int[] Category { get; set; }
        public string[] Tracker { get; set; }

        public static TorznabQuery ToTorznabQuery(ApiSearch request)
        {
            var stringQuery = new TorznabQuery
            {
                QueryType = "search"
            };

            var queryStr = request.Query;
            if (queryStr != null)
            {
                var seasonMatch = Regex.Match(queryStr, @"S(\d{2,4})");
                if (seasonMatch.Success)
                {
                    stringQuery.Season = int.Parse(seasonMatch.Groups[1].Value);
                    queryStr = queryStr.Remove(seasonMatch.Index, seasonMatch.Length);
                }

                var episodeMatch = Regex.Match(queryStr, @"E(\d{2,4}[A-Za-z]?)");
                if (episodeMatch.Success)
                {
                    stringQuery.Episode = episodeMatch.Groups[1].Value.TrimStart(new char[] { '0' });
                    queryStr = queryStr.Remove(episodeMatch.Index, episodeMatch.Length);
                }
                queryStr = queryStr.Trim();
            }
            else
            {
                queryStr = ""; // empty string search is interpreted as null 
            }

            stringQuery.SearchTerm = queryStr;
            stringQuery.Categories = request.Category ?? new int[0];

            // try to build an IMDB Query (tt plus 6 to 8 digits)
            if (stringQuery.SanitizedSearchTerm.StartsWith("tt") && stringQuery.SanitizedSearchTerm.Length <= 10)
            {
                var imdbID = ParseUtil.GetFullImdbID(stringQuery.SanitizedSearchTerm);
                TorznabQuery imdbQuery = null;
                if (imdbID != null)
                {
                    imdbQuery = new TorznabQuery()
                    {
                        ImdbID = imdbID,
                        Categories = stringQuery.Categories,
                        Season = stringQuery.Season,
                        Episode = stringQuery.Episode,
                    };

                    return imdbQuery;
                }
            }

            return stringQuery;
        }
    }
}
