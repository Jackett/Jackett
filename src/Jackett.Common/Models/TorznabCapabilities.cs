using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Jackett.Common.Models
{
    public enum MovieSearchParam
    {
        Q,
        ImdbId,
        TmdbId
    }

    public class TorznabCapabilities
    {
        public int? LimitsMax { get; set; }
        public int? LimitsDefault { get; set; }

        public bool SearchAvailable { get; set; }

        public bool TVSearchAvailable { get; set; }
        public bool SupportsImdbTVSearch { get; set; }
        public bool SupportsTvdbSearch { get; set; }
        public bool SupportsTVRageSearch { get; set; }

        public List<MovieSearchParam> MovieSearchParams;
        public bool MovieSearchAvailable => (MovieSearchParams.Count > 0);
        public bool MovieSearchImdbAvailable => (MovieSearchParams.Contains(MovieSearchParam.ImdbId));
        public bool MovieSearchTmdbAvailable => (MovieSearchParams.Contains(MovieSearchParam.TmdbId));

        public List<string> SupportedMusicSearchParamsList;
        public bool MusicSearchAvailable => (SupportedMusicSearchParamsList.Count > 0);

        public bool BookSearchAvailable { get; set; }

        public List<TorznabCategory> Categories { get; private set; }

        public TorznabCapabilities()
        {
            SearchAvailable = true;
            TVSearchAvailable = true;
            SupportsImdbTVSearch = false;
            SupportsTvdbSearch = false;
            SupportsTVRageSearch = false;
            MovieSearchParams = new List<MovieSearchParam>();
            SupportedMusicSearchParamsList = new List<string>();
            BookSearchAvailable = false;
            Categories = new List<TorznabCategory>();
        }

        private string SupportedTVSearchParams
        {
            get
            {
                var parameters = new List<string>() { "q", "season", "ep" };
                if (SupportsImdbTVSearch)
                    parameters.Add("imdbid");
                if (SupportsTvdbSearch)
                    parameters.Add("tvdbid");
                if (SupportsTVRageSearch)
                    parameters.Add("rid");
                return string.Join(",", parameters);
            }
        }

        public void ParseMovieSearchParams(IEnumerable<string> paramsList)
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

        private string SupportedMovieSearchParams()
        {
            // TODO: always enable q? It can't be disabled
            var parameters = new List<string> { "q" };
            if (MovieSearchImdbAvailable)
                parameters.Add("imdbid");
            if (MovieSearchTmdbAvailable)
                parameters.Add("tmdbid");
            return string.Join(",", parameters);
        }

        private string SupportedMusicSearchParams => string.Join(",", SupportedMusicSearchParamsList);

        private string SupportedBookSearchParams
        {
            get
            {
                var parameters = new List<string>() { "q" };
                if (BookSearchAvailable)
                    parameters.Add("author,title");
                return string.Join(",", parameters);
            }
        }

        public bool SupportsCategories(int[] categories)
        {
            var subCategories = Categories.SelectMany(c => c.SubCategories);
            var allCategories = Categories.Concat(subCategories);
            var supportsCategory = allCategories.Any(i => categories.Any(c => c == i.ID));
            return supportsCategory;
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
                            LimitsMax != null ? new XAttribute("max", LimitsMax) : null,
                            LimitsDefault != null ? new XAttribute("default", LimitsDefault) : null
                        )
                    : null,
                    new XElement("searching",
                        new XElement("search",
                            new XAttribute("available", SearchAvailable ? "yes" : "no"),
                            new XAttribute("supportedParams", "q")
                        ),
                        new XElement("tv-search",
                            new XAttribute("available", TVSearchAvailable ? "yes" : "no"),
                            new XAttribute("supportedParams", SupportedTVSearchParams)
                        ),
                        new XElement("movie-search",
                            new XAttribute("available", MovieSearchAvailable ? "yes" : "no"),
                            new XAttribute("supportedParams", SupportedMovieSearchParams())
                        ),
                        new XElement("music-search",
                            new XAttribute("available", MusicSearchAvailable ? "yes" : "no"),
                            new XAttribute("supportedParams", SupportedMusicSearchParams)
                        ),
                        // inconsistend but apparently already used by various newznab indexers (see #1896)
                        new XElement("audio-search",
                            new XAttribute("available", MusicSearchAvailable ? "yes" : "no"),
                            new XAttribute("supportedParams", SupportedMusicSearchParams)
                        ),
                        new XElement("book-search",
                            new XAttribute("available", BookSearchAvailable ? "yes" : "no"),
                            new XAttribute("supportedParams", SupportedBookSearchParams)
                        )
                    ),
                    new XElement("categories",
                        from c in Categories.OrderBy(x => x.ID < 100000 ? "z" + x.ID.ToString() : x.Name)
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

        public static TorznabCapabilities Concat(TorznabCapabilities lhs, TorznabCapabilities rhs)
        {
            lhs.SearchAvailable = lhs.SearchAvailable || rhs.SearchAvailable;
            lhs.TVSearchAvailable = lhs.TVSearchAvailable || rhs.TVSearchAvailable;
            lhs.SupportsImdbTVSearch = lhs.SupportsImdbTVSearch || rhs.SupportsImdbTVSearch;
            lhs.SupportsTvdbSearch = lhs.SupportsTvdbSearch || rhs.SupportsTvdbSearch;
            lhs.SupportsTVRageSearch = lhs.SupportsTVRageSearch || rhs.SupportsTVRageSearch;
            lhs.MovieSearchParams = lhs.MovieSearchParams.Union(rhs.MovieSearchParams).ToList();
            // TODO: add music search
            lhs.BookSearchAvailable = lhs.BookSearchAvailable || rhs.BookSearchAvailable;
            lhs.Categories.AddRange(rhs.Categories.Where(x => x.ID < 100000).Except(lhs.Categories)); // exclude indexer specific categories (>= 100000)
            return lhs;
        }
    }
}
