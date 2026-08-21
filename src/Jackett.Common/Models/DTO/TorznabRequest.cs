using System;
using System.Linq;
using Jackett.Common.Utils;

namespace Jackett.Common.Models.DTO
{
    public class TorznabRequest
    {
        public string q { get; set; }
        public string imdbid { get; set; }
        public string ep { get; set; }
        public string t { get; set; }
        public string o { get; set; }
        public string extended { get; set; }
        public string limit { get; set; }
        public string offset { get; set; }
        public string cache { get; set; }
        public string cat { get; set; }
        public string season { get; set; }
        public string rid { get; set; }
        public string tvdbid { get; set; }
        public string tmdbid { get; set; }
        public string tvmazeid { get; set; }
        public string traktid { get; set; }
        public string doubanid { get; set; }
        public string album { get; set; }
        public string artist { get; set; }
        public string label { get; set; }
        public string track { get; set; }
        public string year { get; set; }
        public string genre { get; set; }
        public string title { get; set; }
        public string author { get; set; }
        public string publisher { get; set; }
        public string configured { get; set; }

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
            if (!string.IsNullOrWhiteSpace(request.extended))
                query.Extended = ParseUtil.CoerceInt(request.extended);
            if (!string.IsNullOrWhiteSpace(request.limit))
                query.Limit = ParseUtil.CoerceInt(request.limit);
            if (!string.IsNullOrWhiteSpace(request.offset))
                query.Offset = ParseUtil.CoerceInt(request.offset);

            if (bool.TryParse(request.cache, out var _cache))
                query.Cache = _cache;

            if (request.cat != null)
                query.Categories = request.cat.Split(',').Where(s => !string.IsNullOrWhiteSpace(s)).Select(int.Parse).ToArray();
            else
            {
                if (query.QueryType == "movie" && !string.IsNullOrWhiteSpace(request.imdbid))
                    query.Categories = new[] { TorznabCatType.Movies.ID };
                else
                    query.Categories = Array.Empty<int>();
            }

            if (!string.IsNullOrWhiteSpace(request.season))
                query.Season = int.Parse(request.season);
            if (!string.IsNullOrWhiteSpace(request.rid))
                query.RageID = int.Parse(request.rid);
            if (!string.IsNullOrWhiteSpace(request.tvdbid))
                query.TvdbID = int.Parse(request.tvdbid);
            if (!string.IsNullOrWhiteSpace(request.tvmazeid))
                query.TvmazeID = int.Parse(request.tvmazeid);

            if (!string.IsNullOrWhiteSpace(request.tmdbid))
                query.TmdbID = int.Parse(request.tmdbid);
            if (!string.IsNullOrWhiteSpace(request.traktid))
                query.TraktID = int.Parse(request.traktid);
            if (!string.IsNullOrWhiteSpace(request.doubanid))
                query.DoubanID = int.Parse(request.doubanid);

            if (!string.IsNullOrWhiteSpace(request.album))
                query.Album = request.album;
            if (!string.IsNullOrWhiteSpace(request.artist))
                query.Artist = request.artist;
            if (!string.IsNullOrWhiteSpace(request.label))
                query.Label = request.label;
            if (!string.IsNullOrWhiteSpace(request.track))
                query.Track = request.track;
            if (!string.IsNullOrWhiteSpace(request.year))
                query.Year = int.Parse(request.year);
            if (!string.IsNullOrWhiteSpace(request.genre))
                query.Genre = request.genre;

            if (!string.IsNullOrWhiteSpace(request.title))
                query.Title = request.title;
            if (!string.IsNullOrWhiteSpace(request.author))
                query.Author = request.author;
            if (!string.IsNullOrWhiteSpace(request.publisher))
                query.Author = request.publisher;

            return query;
        }
    }
}
