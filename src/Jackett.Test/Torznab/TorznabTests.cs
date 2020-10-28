using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jackett.Common.Indexers;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Jackett.Test.Torznab
{
    // TODO: this cass is temporary. We have categories functionality spread across a lot of classes and we
    // need to make sure we are not losing features in the refactor.

    [TestFixture]
    public class TorznabTests: BaseWebIndexer
    {
        public TorznabTests():
            base(id: "test_id",
                 name: "test_name",
                 description: "test_description",
                 link: "https://test.link/",
                 caps: new TorznabCapabilities(),
                 client: null,
                 configService: null,
                 logger: null,
                 configData: new ConfigurationData(),
                 p: null)
        {
        }

        public override Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson) => throw new NotImplementedException();
        protected override Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query) => throw new NotImplementedException();

        [Test]
        public void TestCSharpTorznabCategories()
        {
            Assert.True(TorznabCaps.SearchAvailable);
            Assert.IsEmpty(TorznabCaps.TvSearchParams);
            Assert.False(TorznabCaps.TvSearchAvailable);
            Assert.False(TorznabCaps.TvSearchSeasonAvailable);
            Assert.False(TorznabCaps.TvSearchEpAvailable);
            Assert.False(TorznabCaps.TvSearchImdbAvailable);
            Assert.False(TorznabCaps.TvSearchTvdbAvailable);
            Assert.False(TorznabCaps.TvSearchTvRageAvailable);
            Assert.IsEmpty(TorznabCaps.MovieSearchParams);
            Assert.False(TorznabCaps.MovieSearchAvailable);
            Assert.False(TorznabCaps.MovieSearchImdbAvailable);
            Assert.False(TorznabCaps.MovieSearchTmdbAvailable);
            Assert.IsEmpty(TorznabCaps.MusicSearchParams);
            Assert.False(TorznabCaps.MusicSearchAvailable);
            Assert.False(TorznabCaps.MusicSearchAlbumAvailable);
            Assert.False(TorznabCaps.MusicSearchArtistAvailable);
            Assert.False(TorznabCaps.MusicSearchLabelAvailable);
            Assert.False(TorznabCaps.MusicSearchYearAvailable);
            Assert.IsEmpty(TorznabCaps.BookSearchParams);
            Assert.False(TorznabCaps.BookSearchAvailable);
            Assert.False(TorznabCaps.BookSearchTitleAvailable);
            Assert.False(TorznabCaps.BookSearchAuthorAvailable);
            Assert.AreEqual(0, TorznabCaps.Categories.GetTorznabCategories().Count);

            // simple category tests
            AddCategoryMapping("1", TorznabCatType.Movies);
            AddCategoryMapping("mov_sd", TorznabCatType.MoviesSD);
            AddCategoryMapping("33", TorznabCatType.BooksComics);
            AddCategoryMapping("44", TorznabCatType.ConsoleXBox, "Console/Xbox_c");
            AddCategoryMapping("con_wii", TorznabCatType.ConsoleWii, "Console/Wii_c");
            AddCategoryMapping("45", TorznabCatType.ConsoleXBox, "Console/Xbox_c2");

            var query = new TorznabQuery // int category with subcategories (parent cat)
            {
                Categories = new [] { TorznabCatType.Movies.ID }
            };
            var trackerCats = MapTorznabCapsToTrackers(query);
            Assert.AreEqual(2, trackerCats.Count);
            Assert.AreEqual("1", trackerCats[0]); // Movies
            Assert.AreEqual("mov_sd", trackerCats[1]); // Movies SD

            // TODO: this is wrong, custom cat 100001 doesn't exists (it's not defined by us)
            var torznabCats = MapTrackerCatToNewznab("1").ToList();
            Assert.AreEqual(2, torznabCats.Count);
            Assert.AreEqual(2000, torznabCats[0]);
            Assert.AreEqual(100001, torznabCats[1]);

            torznabCats = MapTrackerCatDescToNewznab("Console/Wii_c").ToList();
            Assert.AreEqual(1, torznabCats.Count);
            Assert.AreEqual(1030, torznabCats[0]);

            // TODO: test AddMultiCategoryMapping
            // TODO: add duplicates: different trackerCat but same newznabCat
            // TODO: duplicates are not working well because we keep 2 internal lists with categories. One is deduplicated
            // and the other doesn't

            // test Jackett UI categories (internal JSON)
            var dto = new Jackett.Common.Models.DTO.Indexer(this);
            var dtoCaps = dto.caps.ToList();
            Assert.AreEqual(7, dtoCaps.Count);
            Assert.AreEqual("100044", dtoCaps[0].ID);
            Assert.AreEqual("100045", dtoCaps[1].ID);
            Assert.AreEqual("1030", dtoCaps[2].ID);
            Assert.AreEqual("1040", dtoCaps[3].ID);
            Assert.AreEqual("2000", dtoCaps[4].ID);
            Assert.AreEqual("2030", dtoCaps[5].ID);
            Assert.AreEqual("7030", dtoCaps[6].ID);

            // test Torznab caps (XML) => more in Common.Model.TorznabCapabilitiesTests
            var xDocument = TorznabCaps.GetXDocument();
            var xDoumentCategories = xDocument.Root?.Element("categories")?.Elements("category").ToList();
            Assert.AreEqual(7, xDoumentCategories?.Count);
            Assert.AreEqual("100044", xDoumentCategories?[0].Attribute("id")?.Value);
            Assert.AreEqual("100045", xDoumentCategories?[1].Attribute("id")?.Value);
            Assert.AreEqual("1030", xDoumentCategories?[2].Attribute("id")?.Value);
            Assert.AreEqual("1040", xDoumentCategories?[3].Attribute("id")?.Value);
            Assert.AreEqual("2000", xDoumentCategories?[4].Attribute("id")?.Value); // Movies
            Assert.AreEqual("2030", xDoumentCategories?[5].Attribute("id")?.Value);
            Assert.AreEqual("7030", xDoumentCategories?[6].Attribute("id")?.Value);
            Assert.AreEqual(9, xDoumentCategories?[4]?.Elements("subcat").ToList().Count); // Movies
        }

        [Test]
        public void TestCardigannTorznabCategories()
        {
            var definition = new IndexerDefinition // minimun indexer definition
            {
                Links = new List<string>{ "https://example.com" },
                Caps = new capabilitiesBlock
                {
                    Modes = new Dictionary<string, List<string>>
                    {
                        {"search", new List<string>{"q"}}
                    }
                },
                Search = new searchBlock()
            };
            var indexer = new CardigannIndexer(null, null, null, null, definition);

            Assert.True(indexer.TorznabCaps.SearchAvailable);
            Assert.IsEmpty(indexer.TorznabCaps.TvSearchParams);
            Assert.False(indexer.TorznabCaps.TvSearchAvailable);
            Assert.False(indexer.TorznabCaps.TvSearchSeasonAvailable);
            Assert.False(indexer.TorznabCaps.TvSearchEpAvailable);
            Assert.False(indexer.TorznabCaps.TvSearchImdbAvailable);
            Assert.False(indexer.TorznabCaps.TvSearchTvdbAvailable);
            Assert.False(indexer.TorznabCaps.TvSearchTvRageAvailable);
            Assert.IsEmpty(indexer.TorznabCaps.MovieSearchParams);
            Assert.False(indexer.TorznabCaps.MovieSearchAvailable);
            Assert.False(indexer.TorznabCaps.MovieSearchImdbAvailable);
            Assert.False(indexer.TorznabCaps.MovieSearchTmdbAvailable);
            Assert.IsEmpty(indexer.TorznabCaps.MusicSearchParams);
            Assert.False(indexer.TorznabCaps.MusicSearchAvailable);
            Assert.False(indexer.TorznabCaps.MusicSearchAlbumAvailable);
            Assert.False(indexer.TorznabCaps.MusicSearchArtistAvailable);
            Assert.False(indexer.TorznabCaps.MusicSearchLabelAvailable);
            Assert.False(indexer.TorznabCaps.MusicSearchYearAvailable);
            Assert.IsEmpty(indexer.TorznabCaps.BookSearchParams);
            Assert.False(indexer.TorznabCaps.BookSearchAvailable);
            Assert.False(indexer.TorznabCaps.BookSearchTitleAvailable);
            Assert.False(indexer.TorznabCaps.BookSearchAuthorAvailable);
            Assert.AreEqual(0, indexer.TorznabCaps.Categories.GetTorznabCategories().Count);

            definition = new IndexerDefinition // test categories (same as in C# indexer)
            {
                Links = new List<string>{ "https://example.com" },
                Caps = new capabilitiesBlock
                {
                    Modes = new Dictionary<string, List<string>>
                    {
                        {"search", new List<string>{"q"}}
                    },
                    Categories = new Dictionary<string, string>
                    {
                        {"1", TorznabCatType.Movies.Name}, // integer cat (has children)
                        {"mov_sd", TorznabCatType.MoviesSD.Name}, // string cat (child cat)
                        {"33", TorznabCatType.BooksComics.Name} // integer cat (child cat)
                    },
                    Categorymappings = new List<CategorymappingBlock>
                    {
                        new CategorymappingBlock // integer cat with description (child cat) => generates custom cat 100044
                        {
                            id = "44",
                            cat = TorznabCatType.ConsoleXBox.Name,
                            desc = "Console/Xbox_c"
                        },
                        new CategorymappingBlock // string cat with description (child cat)
                        {
                            id = "con_wii",
                            cat = TorznabCatType.ConsoleWii.Name,
                            desc = "Console/Wii_c"
                        },
                        new CategorymappingBlock // duplicate category (2 indexer cats => 1 toznab cat)
                        {
                            id = "45",
                            cat = TorznabCatType.ConsoleXBox.Name,
                            desc = "Console/Xbox_c2"
                        },
                    }
                },
                Search = new searchBlock()
            };
            indexer = new CardigannIndexer(null, null, null, null, definition);

            // TODO: test duplicates
            var cats = indexer.TorznabCaps.Categories.GetTorznabCategories();
            Assert.AreEqual(7, cats.Count);
            Assert.AreEqual(2000, cats[0].ID);
            Assert.AreEqual(2030, cats[1].ID);
            Assert.AreEqual(7030, cats[2].ID);
            Assert.AreEqual(1040, cats[3].ID);
            Assert.AreEqual(100044, cats[4].ID);
            Assert.AreEqual(1030, cats[5].ID);
            Assert.AreEqual(100045, cats[6].ID);

            definition = new IndexerDefinition // test search modes
            {
                Links = new List<string>{ "https://example.com" },
                Caps = new capabilitiesBlock
                {
                    Modes = new Dictionary<string, List<string>>
                    {
                        {"search", new List<string>{ "q" }},
                        {"tv-search", new List<string>{ "q", "season", "ep", "imdbid", "tvdbid", "rid" }},
                        {"movie-search", new List<string>{ "q", "imdbid", "tmdbid" }},
                        {"music-search", new List<string>{ "q", "album", "artist", "label", "year" }},
                        {"book-search", new List<string>{ "q", "title", "author" }}
                    },
                    Categories = new Dictionary<string, string>()
                },
                Search = new searchBlock()
            };
            indexer = new CardigannIndexer(null, null, null, null, definition);

            Assert.True(indexer.TorznabCaps.SearchAvailable);
            Assert.AreEqual(
                new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId, TvSearchParam.TvdbId, TvSearchParam.RId
                },
                indexer.TorznabCaps.TvSearchParams
                );
            Assert.True(indexer.TorznabCaps.TvSearchAvailable);
            Assert.True(indexer.TorznabCaps.TvSearchSeasonAvailable);
            Assert.True(indexer.TorznabCaps.TvSearchEpAvailable);
            // TODO: SupportsImdbTVSearch is disabled in Jackett.Common.Models.TorznabCapabilities.TvSearchImdbAvailable
            Assert.False(indexer.TorznabCaps.TvSearchImdbAvailable);
            Assert.True(indexer.TorznabCaps.TvSearchTvdbAvailable);
            Assert.True(indexer.TorznabCaps.TvSearchTvRageAvailable);
            Assert.AreEqual(
                new List<MovieSearchParam>
                {
                    MovieSearchParam.Q, MovieSearchParam.ImdbId, MovieSearchParam.TmdbId
                },
                indexer.TorznabCaps.MovieSearchParams
                );
            Assert.True(indexer.TorznabCaps.MovieSearchAvailable);
            Assert.True(indexer.TorznabCaps.MovieSearchImdbAvailable);
            Assert.True(indexer.TorznabCaps.MovieSearchTmdbAvailable);
            Assert.AreEqual(
                new List<MusicSearchParam>
                {
                    MusicSearchParam.Q, MusicSearchParam.Album, MusicSearchParam.Artist, MusicSearchParam.Label, MusicSearchParam.Year
                },
                indexer.TorznabCaps.MusicSearchParams
                );
            Assert.True(indexer.TorznabCaps.MusicSearchAvailable);
            Assert.True(indexer.TorznabCaps.MusicSearchAlbumAvailable);
            Assert.True(indexer.TorznabCaps.MusicSearchArtistAvailable);
            Assert.True(indexer.TorznabCaps.MusicSearchLabelAvailable);
            Assert.True(indexer.TorznabCaps.MusicSearchYearAvailable);
            Assert.AreEqual(
                new List<BookSearchParam>
                {
                    BookSearchParam.Q, BookSearchParam.Title, BookSearchParam.Author
                },
                indexer.TorznabCaps.BookSearchParams
                );
            Assert.True(indexer.TorznabCaps.BookSearchAvailable);
            Assert.True(indexer.TorznabCaps.BookSearchTitleAvailable);
            Assert.True(indexer.TorznabCaps.BookSearchAuthorAvailable);

            // test Jackett UI categories (internal JSON) => same code path as C# indexer
            // test Torznab caps (XML) => same code path as C# indexer
        }
    }
}
