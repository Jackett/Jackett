using System;
using System.Text.RegularExpressions;
using Jackett.Common.Utils;

namespace Jackett.Common.Models.DTO
{
    public class ApiSearch
    {
        public string Query { get; set; }
        public int[] Category { get; set; }
        public string[] Tracker { get; set; }
        public string ImdbId { get; set; }
        public int? DoubanID { get; set; }
        public int? RageID { get; set; }
        public int? TvdbId { get; set; }
        public int? TmdbId { get; set; }
        public int? TvMazeId { get; set; }
        public int? TraktID { get; set; }

        public static TorznabQuery ToTorznabQuery(ApiSearch request)
        {
            var stringQuery = new TorznabQuery
            {
                QueryType = "search"
            };

            var queryStr = $"{request.Query}".Trim();

            if (!string.IsNullOrWhiteSpace(queryStr))
            {
                var seasonEpisodeMatch = Regex.Match(queryStr, @"\bS(\d{2,4})E(\d{2,4}[A-Za-z]?)$");
                if (seasonEpisodeMatch.Success)
                {
                    stringQuery.Season = int.Parse(seasonEpisodeMatch.Groups[1].Value);
                    stringQuery.Episode = seasonEpisodeMatch.Groups[2].Value.TrimStart('0');
                    queryStr = queryStr.Remove(seasonEpisodeMatch.Index, seasonEpisodeMatch.Length).Trim();
                }
                else
                {
                    var episodeMatch = Regex.Match(queryStr, @"\bE(\d{2,4}[A-Za-z]?)$");
                    if (episodeMatch.Success)
                    {
                        stringQuery.Episode = episodeMatch.Groups[1].Value.TrimStart('0');
                        queryStr = queryStr.Remove(episodeMatch.Index, episodeMatch.Length).Trim();
                    }

                    var seasonMatch = Regex.Match(queryStr, @"\bS(\d{2,4})$");
                    if (seasonMatch.Success)
                    {
                        stringQuery.Season = int.Parse(seasonMatch.Groups[1].Value);
                        queryStr = queryStr.Remove(seasonMatch.Index, seasonMatch.Length).Trim();
                    }
                }

                queryStr = queryStr.Trim();
            }

            stringQuery.SearchTerm = queryStr;
            stringQuery.Categories = request.Category ?? Array.Empty<int>();
            stringQuery.DoubanID = request.DoubanID;
            stringQuery.RageID = request.RageID;
            stringQuery.TraktID = request.TraktID;
            stringQuery.TmdbID = request.TmdbId;
            stringQuery.TvdbID = request.TvdbId;
            stringQuery.ImdbID = request.ImdbId;
            stringQuery.TvmazeID = request.TvMazeId;

            // try to build an IMDB Query (tt plus 6 to 8 digits)
            if (stringQuery.SanitizedSearchTerm.StartsWith("tt") && stringQuery.SanitizedSearchTerm.Length <= 10)
            {
                var imdbId = ParseUtil.GetFullImdbId(stringQuery.SanitizedSearchTerm);
                if (imdbId != null)
                {
                    return new TorznabQuery
                    {
                        ImdbID = imdbId,
                        Categories = stringQuery.Categories,
                        Season = stringQuery.Season,
                        Episode = stringQuery.Episode,
                    };
                }
            }

            return stringQuery;
        }
    }
}
