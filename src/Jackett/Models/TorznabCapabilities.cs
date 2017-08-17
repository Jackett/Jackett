using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Jackett.Models
{
    public class TorznabCapabilities
    {
        public int? LimitsMax { get; set; } = null;
        public int? LimitsDefault { get; set; } = null;

        public bool SearchAvailable { get; set; }

        public bool TVSearchAvailable { get; set; }

        public bool MovieSearchAvailable { get; set; }

        public bool SupportsTVRageSearch { get; set; }

        public bool SupportsImdbSearch { get; set; }

        public List<TorznabCategory> Categories { get; private set; }

        public TorznabCapabilities()
        {
            Categories = new List<TorznabCategory>();
            SearchAvailable = true;
            TVSearchAvailable = true;
            MovieSearchAvailable = false;
            SupportsTVRageSearch = false;
            SupportsImdbSearch = false;
        }

        public TorznabCapabilities(params TorznabCategory[] cats)
        {
            SearchAvailable = true;
            TVSearchAvailable = true;
            SupportsTVRageSearch = false;
            SupportsImdbSearch = false;
            Categories = new List<TorznabCategory>();
            Categories.AddRange(cats);
            MovieSearchAvailable = Categories.Any(i => TorznabCatType.Movies.Contains(i));
        }

        string SupportedTVSearchParams
        {
            get
            {
                var parameters = new List<string>() { "q", "season", "ep" };
                if (SupportsTVRageSearch)
                    parameters.Add("rid");
                return string.Join(",", parameters);
            }
        }

        string SupportedMovieSearchParams
        {
            get
            {
                var parameters = new List<string>() { "q" };
                if (SupportsImdbSearch)
                    parameters.Add("imdbid");
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

        public JArray CapsToJson()
        {
            var jArray = new JArray();
            foreach (var cat in Categories.GroupBy(p => p.ID).Select(g => g.First()).OrderBy(c => c.ID < 100000 ? "z" + c.ID.ToString() : c.Name))
            {
                jArray.Add(cat.ToJson());
            }
            return jArray;
        }

        public string ToXml()
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
                            new XAttribute("supportedParams", SupportedMovieSearchParams)
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

            return xdoc.Declaration.ToString() + Environment.NewLine + xdoc.ToString();
        }
        public static TorznabCapabilities Concat(TorznabCapabilities lhs, TorznabCapabilities rhs)
        {
            lhs.SearchAvailable = lhs.SearchAvailable || rhs.SearchAvailable;
            lhs.TVSearchAvailable = lhs.TVSearchAvailable || rhs.TVSearchAvailable;
            lhs.MovieSearchAvailable = lhs.MovieSearchAvailable || rhs.MovieSearchAvailable;
            lhs.SupportsTVRageSearch = lhs.SupportsTVRageSearch || rhs.SupportsTVRageSearch;
            lhs.SupportsImdbSearch = lhs.SupportsImdbSearch || rhs.SupportsImdbSearch;
            lhs.Categories.AddRange(rhs.Categories.Where(x => x.ID < 100000).Except(lhs.Categories)); // exclude indexer specific categories (>= 100000)

            return lhs;
        }
    }
}
