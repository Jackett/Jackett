using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Jackett.Common.Models
{
    public class TorznabCapabilities
    {
        public enum SearchEngineType
        {
            sphinx,
            raw
        }

        public int? LimitsMax { get; set; }
        public int? LimitsDefault { get; set; }

        public SearchEngineType? SearchType { get; set; }

        public bool SearchAvailable { get; set; }

        public bool TVSearchAvailable { get; set; }

        public bool MovieSearchAvailable { get; set; }

        public bool SupportsTitleSearch { get; set; }

        public bool SupportsTVRageSearch { get; set; }
        public bool SupportsTvdbSearch { get; set; }

        public bool SupportsImdbMovieSearch { get; set; }
        public bool SupportsTmdbMovieSearch { get; set; }

        public bool SupportsImdbTVSearch { get; set; }

        public bool MusicSearchAvailable => (SupportedMusicSearchParamsList.Count > 0);

        public List<string> SupportedMusicSearchParamsList;

        public bool BookSearchAvailable { get; set; }

        public List<TorznabCategory> Categories { get; private set; }

        public bool SupportsSearchEngine
        {
            get
            {
                return SearchType != null;
            }
        }

        public TorznabCapabilities()
        {
            Categories = new List<TorznabCategory>();
            SearchAvailable = true;
            TVSearchAvailable = true;
            MovieSearchAvailable = false;
            SupportsTitleSearch = false;
            SupportsTVRageSearch = false;
            SupportsTvdbSearch = false;
            SupportsImdbMovieSearch = false;
            SupportsTmdbMovieSearch = false;
            SupportsImdbTVSearch = false;
            SupportedMusicSearchParamsList = new List<string>();
            BookSearchAvailable = false;
        }

        public TorznabCapabilities(params TorznabCategory[] cats)
        {
            SearchAvailable = true;
            TVSearchAvailable = true;
            SupportsTitleSearch = false;
            SupportsTVRageSearch = false;
            SupportsTvdbSearch = false;
            SupportsImdbMovieSearch = false;
            SupportsTmdbMovieSearch = false;
            SupportsImdbTVSearch = false;
            SupportedMusicSearchParamsList = new List<string>();
            BookSearchAvailable = false;
            Categories = new List<TorznabCategory>();
            Categories.AddRange(cats);
            MovieSearchAvailable = Categories.Any(i => TorznabCatType.Movies.Contains(i));
        }

        private string SupportedSearchParams
        {
            get
            {
                var parameters = new List<string>() { "q" };
                if (SupportsSearchEngine)
                    parameters.Add("title");
                return string.Join(",", parameters);
            }
        }

        private string SupportedTVSearchParams
        {
            get
            {
                var parameters = new List<string>() { "q", "season", "ep" };
                if (SupportsSearchEngine)
                    parameters.Add("title");
                if (SupportsTVRageSearch)
                    parameters.Add("rid");
                if (SupportsTvdbSearch)
                    parameters.Add("tvdbid");
                if (SupportsImdbTVSearch)
                    parameters.Add("imdbid");
                if (SupportsTitleSearch)
                    parameters.Add("title");

                return string.Join(",", parameters);
            }
        }

        private string SupportedMovieSearchParams
        {
            get
            {
                var parameters = new List<string>() { "q" };
                if (SupportsImdbMovieSearch)
                    parameters.Add("imdbid");
                if (SupportsTmdbMovieSearch)
                    parameters.Add("tmdbid");
                return string.Join(",", parameters);
            }
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
                            new XAttribute("supportedParams", SupportedSearchParams),
                            SupportsSearchEngine ? new XAttribute("searchEngine", SearchType) : null
                        ),
                        new XElement("tv-search",
                            new XAttribute("available", TVSearchAvailable ? "yes" : "no"),
                            new XAttribute("supportedParams", SupportedTVSearchParams),
                            SupportsSearchEngine ? new XAttribute("searchEngine", SearchType) : null
                        ),
                        new XElement("movie-search",
                            new XAttribute("available", MovieSearchAvailable ? "yes" : "no"),
                            new XAttribute("supportedParams", SupportedMovieSearchParams)
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
            lhs.MovieSearchAvailable = lhs.MovieSearchAvailable || rhs.MovieSearchAvailable;
            lhs.BookSearchAvailable = lhs.BookSearchAvailable || rhs.BookSearchAvailable;
            lhs.SupportsTVRageSearch = lhs.SupportsTVRageSearch || rhs.SupportsTVRageSearch;
            lhs.SupportsTvdbSearch = lhs.SupportsTvdbSearch || rhs.SupportsTvdbSearch;
            lhs.SupportsImdbMovieSearch = lhs.SupportsImdbMovieSearch || rhs.SupportsImdbMovieSearch;
            lhs.SupportsTmdbMovieSearch = lhs.SupportsTmdbMovieSearch || rhs.SupportsTmdbMovieSearch;
            lhs.SupportsImdbTVSearch = lhs.SupportsImdbTVSearch || rhs.SupportsImdbTVSearch;
            lhs.Categories.AddRange(rhs.Categories.Where(x => x.ID < 100000).Except(lhs.Categories)); // exclude indexer specific categories (>= 100000)
            lhs.SearchType = lhs.SearchType != null ? lhs.SearchType : rhs.SearchType;

            return lhs;
        }
    }
}
