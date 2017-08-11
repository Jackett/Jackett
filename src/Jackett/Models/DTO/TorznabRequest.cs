using System;
using System.Linq;
using Jackett.Utils;

namespace Jackett.Models.DTO
{
    public class TorznabRequest
    {
        public string t { get; set; }
        public string q { get; set; }
        public string cat { get; set; }
        public string imdbid { get; set; }
        public string extended { get; set; }
        public string limit { get; set; }
        public string offset { get; set; }
        public string rid { get; set; }
        public string season { get; set; }
        public string ep { get; set; }

        public static TorznabQuery ToTorznabQuery(TorznabRequest request)
        {
            var query = new TorznabQuery()
            {
                QueryType = "search",
                SearchTerm = request.q,
                ImdbID = request.imdbid,
                Episode = request.ep,
            };
            if (request.t != null)
                query.QueryType = request.t;
            if (!request.extended.IsNullOrEmptyOrWhitespace())
                query.Extended = ParseUtil.CoerceInt(request.extended);
            if (!request.limit.IsNullOrEmptyOrWhitespace())
                query.Limit = ParseUtil.CoerceInt(request.limit);
            if (!request.offset.IsNullOrEmptyOrWhitespace())
                query.Offset = ParseUtil.CoerceInt(request.offset);

            if (request.cat != null)
            {
                query.Categories = request.cat.Split(',').Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => int.Parse(s)).ToArray();
            }
            else
            {
                if (query.QueryType == "movie" && !string.IsNullOrWhiteSpace(request.imdbid))
                    query.Categories = new int[] { TorznabCatType.Movies.ID };
                else
                    query.Categories = new int[0];
            }

            if (!request.rid.IsNullOrEmptyOrWhitespace())
                query.RageID = int.Parse(request.rid);

            if (!request.season.IsNullOrEmptyOrWhitespace())
                query.Season = int.Parse(request.season);

            query.ExpandCatsToSubCats();

            return query;
        }
    }
}
