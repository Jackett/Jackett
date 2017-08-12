using System.Text.RegularExpressions;
using Jackett.Utils;

namespace Jackett.Models.DTO
{
    public class ApiSearch
    {
        public string Query { get; set; }
        public int Category { get; set; }

        public static TorznabQuery ToTorznabQuery(ApiSearch request)
        {
            var stringQuery = new TorznabQuery();
            stringQuery.QueryType = "search";

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
                    stringQuery.Episode = episodeMatch.Groups[1].Value;
                    queryStr = queryStr.Remove(episodeMatch.Index, episodeMatch.Length);
                }
                queryStr = queryStr.Trim();
            }
            else
            {
                queryStr = ""; // empty string search is interpreted as null 
            }

            stringQuery.SearchTerm = queryStr;
            stringQuery.Categories = request.Category == 0 ? new int[0] : new int[1] { request.Category };
            stringQuery.ExpandCatsToSubCats();

            // try to build an IMDB Query
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
                imdbQuery.ExpandCatsToSubCats();

                return imdbQuery;
            }

            return stringQuery;
        }
    }
}
