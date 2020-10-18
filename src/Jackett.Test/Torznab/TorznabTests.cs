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

        public override TorznabCapabilities TorznabCaps { get; protected set; }
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
            Assert.False(TorznabCaps.BookSearchAvailable);
            Assert.AreEqual(0, TorznabCaps.Categories.Count);

            // add "int" category (parent category)
            AddCategoryMapping(1, TorznabCatType.Movies);
            Assert.AreEqual(1, TorznabCaps.Categories.Count);
            Assert.AreEqual(2000, TorznabCaps.Categories[0].ID);

            // add "string" category (child category)
            AddCategoryMapping("mov_sd", TorznabCatType.MoviesSD);
            Assert.AreEqual(2, TorznabCaps.Categories.Count);
            Assert.AreEqual(2030, TorznabCaps.Categories[1].ID);

            // add subcategory of books (child category)
            AddCategoryMapping(33, TorznabCatType.BooksComics);
            Assert.AreEqual(3, TorznabCaps.Categories.Count);
            Assert.AreEqual(8020, TorznabCaps.Categories[2].ID);

            // add int category with description => custom category. it's converted into 2 different categories
            AddCategoryMapping(44, TorznabCatType.ConsoleXbox, "Console/Xbox_c");
            Assert.AreEqual(5, TorznabCaps.Categories.Count);
            Assert.AreEqual(1040, TorznabCaps.Categories[3].ID);
            Assert.AreEqual(100044, TorznabCaps.Categories[4].ID);

            // TODO: we should add a way to add custom categories for string categories
            // https://github.com/Sonarr/Sonarr/wiki/Implementing-a-Torznab-indexer#caps-endpoint
            // add string category with description. it's converted into 1 category
            AddCategoryMapping("con_wii", TorznabCatType.ConsoleWii, "Console/Wii_c");
            Assert.AreEqual(6, TorznabCaps.Categories.Count);
            Assert.AreEqual(1030, TorznabCaps.Categories[5].ID);

            // add another int category with description that maps to ConsoleXbox (there are 2 tracker cats => 1 torznab cat)
            AddCategoryMapping(45, TorznabCatType.ConsoleXbox, "Console/Xbox_c2");
            Assert.AreEqual(7, TorznabCaps.Categories.Count);
            Assert.AreEqual(100045, TorznabCaps.Categories[6].ID); // 1040 is duplicated and it is not added

            // TODO: test AddMultiCategoryMapping
            // TODO: add duplicates: different trackerCat but same newznabCat
            // TODO: duplicates are not working well because we keep 2 internal lists with categories. One is deduplicated
            // and the other doesn't
            // add duplicate
            //AddCategoryMapping(1, TorznabCatType.Movies, "Movies");
            //Assert.AreEqual(6, TorznabCaps.Categories.Count);

            // test MapTorznabCapsToTrackers: maps TorznazQuery cats => Tracker cats
            var query = new TorznabQuery(); // no cats
            var trackerCats = MapTorznabCapsToTrackers(query);
            Assert.AreEqual(0, trackerCats.Count);

            query = new TorznabQuery // a lot of cats (mixed types)
            {
                Categories = TorznabCaps.Categories.Select(c => c.ID).ToArray()
            };
            trackerCats = MapTorznabCapsToTrackers(query);
            Assert.AreEqual(6, trackerCats.Count);
            Assert.AreEqual("1", trackerCats[0]);
            Assert.AreEqual("mov_sd", trackerCats[1]);
            Assert.AreEqual("33", trackerCats[2]);
            Assert.AreEqual("44", trackerCats[3]);
            Assert.AreEqual("45", trackerCats[4]);
            Assert.AreEqual("con_wii", trackerCats[5]);

            query = new TorznabQuery // int category with subcategories (parent cat)
            {
                Categories = new [] { 2000 } // Movies
            };
            trackerCats = MapTorznabCapsToTrackers(query);
            Assert.AreEqual(2, trackerCats.Count);
            Assert.AreEqual("1", trackerCats[0]); // Movies
            Assert.AreEqual("mov_sd", trackerCats[1]); // Movies SD

            query = new TorznabQuery // string child category
            {
                Categories = new [] { 2030 } // Movies SD
            };
            trackerCats = MapTorznabCapsToTrackers(query);
            Assert.AreEqual(1, trackerCats.Count);
            Assert.AreEqual("mov_sd", trackerCats[0]); // Movies SD
            trackerCats = MapTorznabCapsToTrackers(query, true); // get parent
            Assert.AreEqual(2, trackerCats.Count);
            Assert.AreEqual("1", trackerCats[0]); // Movies
            Assert.AreEqual("mov_sd", trackerCats[1]); // Movies SD

            query = new TorznabQuery // duplicate category (1 toznab cat => 2 indexer cats)
            {
                Categories = new [] { 1040 } // ConsoleXbox
            };
            trackerCats = MapTorznabCapsToTrackers(query);
            Assert.AreEqual(2, trackerCats.Count);
            Assert.AreEqual("44", trackerCats[0]);
            Assert.AreEqual("45", trackerCats[1]);

            query = new TorznabQuery // custom cat
            {
                Categories = new [] { 100001 } // Movies
            };
            trackerCats = MapTorznabCapsToTrackers(query);
            Assert.AreEqual(1, trackerCats.Count);
            Assert.AreEqual("1", trackerCats[0]); // Movies

            query = new TorznabQuery // unknown category
            {
                Categories = new [] { 9999 }
            };
            trackerCats = MapTorznabCapsToTrackers(query);
            Assert.AreEqual(0, trackerCats.Count);

            // TODO: this is wrong, custom cat 100001 doesn't exists (it's not defined by us)
            // test MapTrackerCatToNewznab: maps Tracker cat ID => Torznab cats
            var torznabCats = MapTrackerCatToNewznab("1").ToList();
            Assert.AreEqual(2, torznabCats.Count);
            Assert.AreEqual(2000, torznabCats[0]);
            Assert.AreEqual(100001, torznabCats[1]);

            torznabCats = MapTrackerCatToNewznab("mov_sd").ToList();
            Assert.AreEqual(1, torznabCats.Count);
            Assert.AreEqual(2030, torznabCats[0]);

            torznabCats = MapTrackerCatToNewznab("44").ToList(); // 44 and 45 maps to ConsoleXbox but different custom cat
            Assert.AreEqual(2, torznabCats.Count);
            Assert.AreEqual(1040, torznabCats[0]);
            Assert.AreEqual(100044, torznabCats[1]);
            torznabCats = MapTrackerCatToNewznab("45").ToList();
            Assert.AreEqual(2, torznabCats.Count);
            Assert.AreEqual(1040, torznabCats[0]);
            Assert.AreEqual(100045, torznabCats[1]);

            // TODO: this is wrong, we are returning cat 109999 which doesn't exist
            //torznabCats = MapTrackerCatToNewznab("9999").ToList(); // unknown cat
            //Assert.AreEqual(0, torznabCats.Count);

            torznabCats = MapTrackerCatToNewznab(null).ToList(); // null
            Assert.AreEqual(0, torznabCats.Count);

            // TODO: I think this method should be removed because description can be non-unique
            // test MapTrackerCatDescToNewznab: maps Tracker cat Description => Torznab cats
            torznabCats = MapTrackerCatDescToNewznab("Console/Xbox_c").ToList(); // Console/Xbox_c and Console/Xbox_c2 maps to ConsoleXbox but different custom cat
            Assert.AreEqual(2, torznabCats.Count);
            Assert.AreEqual(1040, torznabCats[0]);
            Assert.AreEqual(100044, torznabCats[1]);

            torznabCats = MapTrackerCatDescToNewznab("Console/Xbox_c2").ToList();
            Assert.AreEqual(2, torznabCats.Count);
            Assert.AreEqual(1040, torznabCats[0]);
            Assert.AreEqual(100045, torznabCats[1]);

            torznabCats = MapTrackerCatDescToNewznab("Console/Wii_c").ToList();
            Assert.AreEqual(1, torznabCats.Count);
            Assert.AreEqual(1030, torznabCats[0]);

            torznabCats = MapTrackerCatDescToNewznab("9999").ToList(); // unknown cat
            Assert.AreEqual(0, torznabCats.Count);

            torznabCats = MapTrackerCatDescToNewznab(null).ToList(); // null
            Assert.AreEqual(0, torznabCats.Count);

            // TODO: move these methods to TorznabCaps or TorznabQuery classess

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
            Assert.AreEqual("8020", dtoCaps[6].ID);

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
            Assert.AreEqual("8020", xDoumentCategories?[6].Attribute("id")?.Value);
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
                    Modes = new Dictionary<string, List<string>>()
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
            Assert.False(indexer.TorznabCaps.BookSearchAvailable);
            Assert.AreEqual(0, indexer.TorznabCaps.Categories.Count);

            definition = new IndexerDefinition // test categories (same as in C# indexer)
            {
                Links = new List<string>{ "https://example.com" },
                Caps = new capabilitiesBlock
                {
                    Modes = new Dictionary<string, List<string>>(),
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
                            cat = TorznabCatType.ConsoleXbox.Name,
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
                            cat = TorznabCatType.ConsoleXbox.Name,
                            desc = "Console/Xbox_c2"
                        },
                    }
                },
                Search = new searchBlock()
            };
            indexer = new CardigannIndexer(null, null, null, null, definition);

            // TODO: test duplicates
            Assert.AreEqual(7, indexer.TorznabCaps.Categories.Count);
            Assert.AreEqual(2000, indexer.TorznabCaps.Categories[0].ID);
            Assert.AreEqual(2030, indexer.TorznabCaps.Categories[1].ID);
            Assert.AreEqual(8020, indexer.TorznabCaps.Categories[2].ID);
            Assert.AreEqual(1040, indexer.TorznabCaps.Categories[3].ID);
            Assert.AreEqual(100044, indexer.TorznabCaps.Categories[4].ID);
            Assert.AreEqual(1030, indexer.TorznabCaps.Categories[5].ID);
            Assert.AreEqual(100045, indexer.TorznabCaps.Categories[6].ID);

            // TODO: we are not validating modes or params in each mode. ie: search is not required/supported and it's used
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
                        {"book-search", new List<string>{ "q", "author", "title" }}
                    },
                    Categories = new Dictionary<string, string>()
                },
                Search = new searchBlock()
            };
            indexer = new CardigannIndexer(null, null, null, null, definition);

            Assert.True(indexer.TorznabCaps.SearchAvailable);
            Assert.True(indexer.TorznabCaps.TvSearchAvailable);
            // TODO: SupportsImdbTVSearch is disabled in Jackett.Common.Models.TorznabCapabilities.TvSearchImdbAvailable
            Assert.False(indexer.TorznabCaps.TvSearchImdbAvailable);
            Assert.True(indexer.TorznabCaps.TvSearchTvdbAvailable);
            Assert.True(indexer.TorznabCaps.TvSearchTvRageAvailable);
            Assert.AreEqual(
                new List<MovieSearchParam> { MovieSearchParam.Q, MovieSearchParam.ImdbId, MovieSearchParam.TmdbId },
                indexer.TorznabCaps.MovieSearchParams
                );
            Assert.True(indexer.TorznabCaps.MovieSearchAvailable);
            Assert.True(indexer.TorznabCaps.MovieSearchImdbAvailable);
            Assert.True(indexer.TorznabCaps.MovieSearchTmdbAvailable);
            // TODO: improve this assert
            Assert.AreEqual(5, indexer.TorznabCaps.MusicSearchParams.Count);
            Assert.True(indexer.TorznabCaps.MusicSearchAvailable);
            Assert.True(indexer.TorznabCaps.BookSearchAvailable);

            // test Jackett UI categories (internal JSON) => same code path as C# indexer
            // test Torznab caps (XML) => same code path as C# indexer
        }
    }
}
