using System.Collections.Generic;
using Jackett.Common.Indexers;
using Jackett.Common.Models;
using NUnit.Framework;

namespace Jackett.Test.Common.Indexers
{
    [TestFixture]
    public class CardigannIndexerTests
    {
        // TODO: split this test into smaller tests
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
