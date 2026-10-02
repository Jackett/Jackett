using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Jackett.Common.Helpers;
using Newtonsoft.Json;

namespace Jackett.Common.Models
{
    public enum TvSearchParam
    {
        Q,
        Season,
        Ep,
        ImdbId,
        TvdbId,
        RId,
        TmdbId,
        TvmazeId,
        TraktId,
        DoubanId,
        Year,
        Genre
    }

    public enum MovieSearchParam
    {
        Q,
        ImdbId,
        TmdbId,
        TraktId,
        DoubanId,
        Year,
        Genre
    }

    public enum MusicSearchParam
    {
        Q,
        Album,
        Artist,
        Label,
        Track,
        Year,
        Genre
    }

    public enum BookSearchParam
    {
        Q,
        Title,
        Author,
        Publisher,
        Year,
        Genre
    }

    public class TorznabCapabilities
    {
        public int? LimitsMax { get; set; }
        public int? LimitsDefault { get; set; }

        public bool SearchAvailable { get; set; }

        public bool SupportsRawSearch { get; set; }

        public List<TvSearchParam> TvSearchParams;
        public bool TvSearchAvailable => (TvSearchParams.Count > 0);
        public bool TvSearchSeasonAvailable => (TvSearchParams.Contains(TvSearchParam.Season));
        public bool TvSearchEpAvailable => (TvSearchParams.Contains(TvSearchParam.Ep));
        //TvSearchImdbAvailable temporarily disabled due to #8107
        // Introduce setter so individual trackers can override
        public bool TvSearchImdbAvailable { get; set; } = false; // (TvSearchParams.Contains(TvSearchParam.ImdbId));
        public bool TvSearchTvdbAvailable => (TvSearchParams.Contains(TvSearchParam.TvdbId));
        public bool TvSearchTvRageAvailable => (TvSearchParams.Contains(TvSearchParam.RId));
        public bool TvSearchTmdbAvailable => (TvSearchParams.Contains(TvSearchParam.TmdbId));
        public bool TvSearchTvMazeAvailable => (TvSearchParams.Contains(TvSearchParam.TvmazeId));
        public bool TvSearchTraktAvailable => (TvSearchParams.Contains(TvSearchParam.TraktId));
        public bool TvSearchDoubanAvailable => (TvSearchParams.Contains(TvSearchParam.DoubanId));
        public bool TvSearchYearAvailable => (TvSearchParams.Contains(TvSearchParam.Year));
        public bool TvSearchGenreAvailable => (TvSearchParams.Contains(TvSearchParam.Genre));

        public List<MovieSearchParam> MovieSearchParams;
        public bool MovieSearchAvailable => (MovieSearchParams.Count > 0);
        public bool MovieSearchImdbAvailable => (MovieSearchParams.Contains(MovieSearchParam.ImdbId));
        public bool MovieSearchTmdbAvailable => (MovieSearchParams.Contains(MovieSearchParam.TmdbId));
        public bool MovieSearchTraktAvailable => (MovieSearchParams.Contains(MovieSearchParam.TraktId));
        public bool MovieSearchDoubanAvailable => (MovieSearchParams.Contains(MovieSearchParam.DoubanId));
        public bool MovieSearchYearAvailable => (MovieSearchParams.Contains(MovieSearchParam.Year));
        public bool MovieSearchGenreAvailable => (MovieSearchParams.Contains(MovieSearchParam.Genre));

        public List<MusicSearchParam> MusicSearchParams;
        public bool MusicSearchAvailable => (MusicSearchParams.Count > 0);
        public bool MusicSearchAlbumAvailable => (MusicSearchParams.Contains(MusicSearchParam.Album));
        public bool MusicSearchArtistAvailable => (MusicSearchParams.Contains(MusicSearchParam.Artist));
        public bool MusicSearchLabelAvailable => (MusicSearchParams.Contains(MusicSearchParam.Label));
        public bool MusicSearchTrackAvailable => (MusicSearchParams.Contains(MusicSearchParam.Track));
        public bool MusicSearchYearAvailable => (MusicSearchParams.Contains(MusicSearchParam.Year));
        public bool MusicSearchGenreAvailable => (MusicSearchParams.Contains(MusicSearchParam.Genre));

        public List<BookSearchParam> BookSearchParams;
        public bool BookSearchAvailable => (BookSearchParams.Count > 0);
        public bool BookSearchTitleAvailable => (BookSearchParams.Contains(BookSearchParam.Title));
        public bool BookSearchAuthorAvailable => (BookSearchParams.Contains(BookSearchParam.Author));
        public bool BookSearchPublisherAvailable => (BookSearchParams.Contains(BookSearchParam.Publisher));
        public bool BookSearchYearAvailable => (BookSearchParams.Contains(BookSearchParam.Year));
        public bool BookSearchGenreAvailable => (BookSearchParams.Contains(BookSearchParam.Genre));

        public readonly TorznabCapabilitiesCategories Categories;

        public TorznabCapabilities()
        {
            SupportsRawSearch = false;
            SearchAvailable = true;
            TvSearchParams = new List<TvSearchParam>();
            MovieSearchParams = new List<MovieSearchParam>();
            MusicSearchParams = new List<MusicSearchParam>();
            BookSearchParams = new List<BookSearchParam>();
            Categories = new TorznabCapabilitiesCategories();
            LimitsDefault = 100;
            LimitsMax = 100;
        }

        public void ParseCardigannSearchModes(Dictionary<string, List<string>> modes)
        {
            if (modes == null || !modes.Any())
                throw new Exception("At least one search mode is required");
            if (!modes.ContainsKey("search"))
                throw new Exception("The search mode 'search' is mandatory");
            foreach (var entry in modes)
                switch (entry.Key)
                {
                    case "search":
                        if (entry.Value == null || entry.Value.Count != 1 || entry.Value[0] != "q")
                            throw new Exception("In search mode 'search' only 'q' parameter is supported and it's mandatory");
                        break;
                    case "tv-search":
                        ParseTvSearchParams(entry.Value);
                        break;
                    case "movie-search":
                        ParseMovieSearchParams(entry.Value);
                        break;
                    case "music-search":
                        ParseMusicSearchParams(entry.Value);
                        break;
                    case "book-search":
                        ParseBookSearchParams(entry.Value);
                        break;
                    default:
                        throw new Exception($"Unsupported search mode: {entry.Key}");
                }
        }

        private void ParseTvSearchParams(IEnumerable<string> paramsList)
        {
            if (paramsList == null)
                return;
            foreach (var paramStr in paramsList)
                if (Enum.TryParse(paramStr, true, out TvSearchParam param))
                    if (!TvSearchParams.Contains(param))
                        TvSearchParams.Add(param);
                    else
                        throw new Exception($"Duplicate tv-search param: {paramStr}");
                else
                    throw new Exception($"Not supported tv-search param: {paramStr}");
        }

        private void ParseMovieSearchParams(IEnumerable<string> paramsList)
        {
            if (paramsList == null)
                return;
            foreach (var paramStr in paramsList)
                if (Enum.TryParse(paramStr, true, out MovieSearchParam param))
                    if (!MovieSearchParams.Contains(param))
                        MovieSearchParams.Add(param);
                    else
                        throw new Exception($"Duplicate movie-search param: {paramStr}");
                else
                    throw new Exception($"Not supported movie-search param: {paramStr}");
        }

        private void ParseMusicSearchParams(IEnumerable<string> paramsList)
        {
            if (paramsList == null)
                return;
            foreach (var paramStr in paramsList)
                if (Enum.TryParse(paramStr, true, out MusicSearchParam param))
                    if (!MusicSearchParams.Contains(param))
                        MusicSearchParams.Add(param);
                    else
                        throw new Exception($"Duplicate music-search param: {paramStr}");
                else
                    throw new Exception($"Not supported music-search param: {paramStr}");
        }

        private void ParseBookSearchParams(IEnumerable<string> paramsList)
        {
            if (paramsList == null)
                return;
            foreach (var paramStr in paramsList)
                if (Enum.TryParse(paramStr, true, out BookSearchParam param))
                    if (!BookSearchParams.Contains(param))
                        BookSearchParams.Add(param);
                    else
                        throw new Exception($"Duplicate book-search param: {paramStr}");
                else
                    throw new Exception($"Not supported book-search param: {paramStr}");
        }

        private string SupportedTvSearchParams()
        {
            var parameters = new List<string> { "q" }; // q is always enabled
            if (TvSearchSeasonAvailable)
                parameters.Add("season");
            if (TvSearchEpAvailable)
                parameters.Add("ep");
            if (TvSearchImdbAvailable)
                parameters.Add("imdbid");
            if (TvSearchTvdbAvailable)
                parameters.Add("tvdbid");
            if (TvSearchTvRageAvailable)
                parameters.Add("rid");
            if (TvSearchTmdbAvailable)
                parameters.Add("tmdbid");
            if (TvSearchTvMazeAvailable)
                parameters.Add("tvmazeid");
            if (TvSearchTraktAvailable)
                parameters.Add("traktid");
            if (TvSearchDoubanAvailable)
                parameters.Add("doubanid");
            if (TvSearchYearAvailable)
                parameters.Add("year");
            if (TvSearchGenreAvailable)
                parameters.Add("genre");
            return string.Join(",", parameters);
        }

        private string SupportedMovieSearchParams()
        {
            var parameters = new List<string> { "q" }; // q is always enabled
            if (MovieSearchImdbAvailable)
                parameters.Add("imdbid");
            if (MovieSearchTmdbAvailable)
                parameters.Add("tmdbid");
            if (MovieSearchTraktAvailable)
                parameters.Add("traktid");
            if (MovieSearchDoubanAvailable)
                parameters.Add("doubanid");
            if (MovieSearchYearAvailable)
                parameters.Add("year");
            if (MovieSearchGenreAvailable)
                parameters.Add("genre");
            return string.Join(",", parameters);
        }

        private string SupportedMusicSearchParams()
        {
            var parameters = new List<string> { "q" }; // q is always enabled
            if (MusicSearchAlbumAvailable)
                parameters.Add("album");
            if (MusicSearchArtistAvailable)
                parameters.Add("artist");
            if (MusicSearchLabelAvailable)
                parameters.Add("label");
            if (MusicSearchTrackAvailable)
                parameters.Add("track");
            if (MusicSearchYearAvailable)
                parameters.Add("year");
            if (MusicSearchGenreAvailable)
                parameters.Add("genre");
            return string.Join(",", parameters);
        }

        private string SupportedBookSearchParams()
        {
            var parameters = new List<string> { "q" }; // q is always enabled
            if (BookSearchTitleAvailable)
                parameters.Add("title");
            if (BookSearchAuthorAvailable)
                parameters.Add("author");
            if (BookSearchPublisherAvailable)
                parameters.Add("publisher");
            if (BookSearchYearAvailable)
                parameters.Add("year");
            if (BookSearchGenreAvailable)
                parameters.Add("genre");
            return string.Join(",", parameters);
        }

        public XDocument GetXDocument()
        {
            var xdoc = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement("caps",
                    new XElement("server",
                        new XAttribute("title", "Jackett")
                    ),
                    LimitsMax != null || LimitsDefault != null ?
                        new XElement("limits",
                            LimitsDefault != null ? new XAttribute("default", LimitsDefault) : null,
                            LimitsMax != null ? new XAttribute("max", LimitsMax) : null
                        )
                    : null,
                    new XElement("searching",
                        new XElement("search",
                            new XAttribute("available", SearchAvailable ? "yes" : "no"),
                            new XAttribute("supportedParams", "q"),
                            SupportsRawSearch ? new XAttribute("searchEngine", "raw") : null
                        ),
                        new XElement("tv-search",
                            new XAttribute("available", TvSearchAvailable ? "yes" : "no"),
                            new XAttribute("supportedParams", SupportedTvSearchParams()),
                            SupportsRawSearch ? new XAttribute("searchEngine", "raw") : null
                        ),
                        new XElement("movie-search",
                            new XAttribute("available", MovieSearchAvailable ? "yes" : "no"),
                            new XAttribute("supportedParams", SupportedMovieSearchParams()),
                            SupportsRawSearch ? new XAttribute("searchEngine", "raw") : null
                        ),
                        new XElement("music-search",
                            new XAttribute("available", MusicSearchAvailable ? "yes" : "no"),
                            new XAttribute("supportedParams", SupportedMusicSearchParams()),
                            SupportsRawSearch ? new XAttribute("searchEngine", "raw") : null
                        ),
                        // inconsistent but apparently already used by various newznab indexers (see #1896)
                        new XElement("audio-search",
                            new XAttribute("available", MusicSearchAvailable ? "yes" : "no"),
                            new XAttribute("supportedParams", SupportedMusicSearchParams()),
                            SupportsRawSearch ? new XAttribute("searchEngine", "raw") : null
                        ),
                        new XElement("book-search",
                            new XAttribute("available", BookSearchAvailable ? "yes" : "no"),
                            new XAttribute("supportedParams", SupportedBookSearchParams()),
                            SupportsRawSearch ? new XAttribute("searchEngine", "raw") : null
                        )
                    ),
                    new XElement("categories",
                        from c in Categories.GetTorznabCategoryTree(true)
                        select new XElement("category",
                            new XAttribute("id", c.ID),
                            new XAttribute("name", c.Name),
                            from sc in c.SubCategories
                            select new XElement("subcat",
                                new XAttribute("id", sc.ID),
                                new XAttribute("name", sc.Name)
                            )
                        )
                    )
                )
            );
            return xdoc;
        }

        public string ToXml() =>
            GetXDocument().Declaration + Environment.NewLine + GetXDocument();

        public string ToJson(JsonSerializerSettings serializerSettings = null)
        {
            var jsonObject = XmlToJsonConverter.XmlToJson(GetXDocument().Root);
            return JsonConvert.SerializeObject(jsonObject, serializerSettings);
        }

        public static TorznabCapabilities Concat(TorznabCapabilities lhs, TorznabCapabilities rhs)
        {
            lhs.SearchAvailable = lhs.SearchAvailable || rhs.SearchAvailable;
            lhs.TvSearchParams = lhs.TvSearchParams.Union(rhs.TvSearchParams).ToList();
            lhs.MovieSearchParams = lhs.MovieSearchParams.Union(rhs.MovieSearchParams).ToList();
            lhs.MusicSearchParams = lhs.MusicSearchParams.Union(rhs.MusicSearchParams).ToList();
            lhs.BookSearchParams = lhs.BookSearchParams.Union(rhs.BookSearchParams).ToList();
            lhs.Categories.Concat(rhs.Categories);
            lhs.SupportsRawSearch = lhs.SupportsRawSearch || rhs.SupportsRawSearch;
            return lhs;
        }
    }
}
