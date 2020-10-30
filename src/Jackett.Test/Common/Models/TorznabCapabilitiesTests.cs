using System;
using System.Collections.Generic;
using System.Linq;
using Jackett.Common.Models;
using Jackett.Test.TestHelpers;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Jackett.Test.Common.Models
{
    [TestFixture]
    public class TorznabCapabilitiesTests
    {
        [Test]
        public void TestConstructors()
        {
            var torznabCaps = new TorznabCapabilities();
            Assert.True(torznabCaps.SearchAvailable);

            Assert.IsEmpty(torznabCaps.TvSearchParams);
            Assert.False(torznabCaps.TvSearchAvailable);
            Assert.False(torznabCaps.TvSearchSeasonAvailable);
            Assert.False(torznabCaps.TvSearchEpAvailable);
            Assert.False(torznabCaps.TvSearchImdbAvailable);
            Assert.False(torznabCaps.TvSearchTvdbAvailable);
            Assert.False(torznabCaps.TvSearchTvRageAvailable);

            Assert.IsEmpty(torznabCaps.MovieSearchParams);
            Assert.False(torznabCaps.MovieSearchAvailable);
            Assert.False(torznabCaps.MovieSearchImdbAvailable);
            Assert.False(torznabCaps.MovieSearchTmdbAvailable);

            Assert.IsEmpty(torznabCaps.MusicSearchParams);
            Assert.False(torznabCaps.MusicSearchAvailable);
            Assert.False(torznabCaps.MusicSearchAlbumAvailable);
            Assert.False(torznabCaps.MusicSearchArtistAvailable);
            Assert.False(torznabCaps.MusicSearchLabelAvailable);
            Assert.False(torznabCaps.MusicSearchYearAvailable);

            Assert.IsEmpty(torznabCaps.BookSearchParams);
            Assert.False(torznabCaps.BookSearchAvailable);
            Assert.False(torznabCaps.BookSearchTitleAvailable);
            Assert.False(torznabCaps.BookSearchAuthorAvailable);

            Assert.IsEmpty(torznabCaps.Categories.GetTorznabCategories());
            Assert.IsEmpty(torznabCaps.Categories.GetTrackerCategories());
        }

        [Test]
        public void TestParseCardigannSearchModes()
        {
            var torznabCaps = new TorznabCapabilities();
            torznabCaps.ParseCardigannSearchModes(new Dictionary<string, List<string>>
            {
                {"search", new List<string> {"q"}},
                {"tv-search", new List<string> {"q"}},
                {"movie-search", new List<string> {"q"}},
                {"music-search", new List<string> {"q"}},
                {"book-search", new List<string> {"q"}}
            });
            Assert.True(torznabCaps.SearchAvailable);
            Assert.True(torznabCaps.TvSearchAvailable);
            Assert.True(torznabCaps.MovieSearchAvailable);
            Assert.True(torznabCaps.MusicSearchAvailable);
            Assert.True(torznabCaps.BookSearchAvailable);

            torznabCaps = new TorznabCapabilities();
            try
            {
                torznabCaps.ParseCardigannSearchModes(null); // null search modes
                Assert.Fail();
            }
            catch (Exception)
            {
                // ignored
            }

            torznabCaps = new TorznabCapabilities();
            try
            {
                torznabCaps.ParseCardigannSearchModes(new Dictionary<string, List<string>>()); // empty search modes
                Assert.Fail();
            }
            catch (Exception)
            {
                // ignored
            }

            torznabCaps = new TorznabCapabilities();
            try {
                torznabCaps.ParseCardigannSearchModes(new Dictionary<string, List<string>>
                {
                    {"bad", new List<string> {"q"}} // bad search mode
                });
                Assert.Fail();
            }
            catch (Exception)
            {
                // ignored
            }

            torznabCaps = new TorznabCapabilities();
            try {
                torznabCaps.ParseCardigannSearchModes(new Dictionary<string, List<string>>
                {
                    {"search", new List<string> {"bad"}} // search mode with bad parameters
                });
                Assert.Fail();
            }
            catch (Exception)
            {
                // ignored
            }
        }

        [Test]
        public void TestParseTvSearchParams()
        {
            var torznabCaps = new TorznabCapabilities();
            torznabCaps.ParseCardigannSearchModes(new Dictionary<string, List<string>>
            {
                {"search", new List<string>{"q"}},
                {"tv-search", null}
            });
            Assert.IsEmpty(torznabCaps.MovieSearchParams);

            torznabCaps = new TorznabCapabilities();
            torznabCaps.ParseCardigannSearchModes(new Dictionary<string, List<string>>
            {
                {"search", new List<string>{"q"}},
                {"tv-search", new List<string>()}
            });
            Assert.IsEmpty(torznabCaps.MovieSearchParams);

            torznabCaps = new TorznabCapabilities();
            torznabCaps.ParseCardigannSearchModes(new Dictionary<string, List<string>>
            {
                {"search", new List<string>{"q"}},
                {"tv-search", new List<string> {"q", "tvdbid"}}
            });
            Assert.AreEqual(new List<TvSearchParam> { TvSearchParam.Q, TvSearchParam.TvdbId }, torznabCaps.TvSearchParams);

            torznabCaps = new TorznabCapabilities();
            try {
                torznabCaps.ParseCardigannSearchModes(new Dictionary<string, List<string>>
                {
                    {"search", new List<string>{"q"}},
                    {"tv-search", new List<string> {"q", "q"}} // duplicate param
                });
                Assert.Fail();
            }
            catch (Exception)
            {
                // ignored
            }

            torznabCaps = new TorznabCapabilities();
            try {
                torznabCaps.ParseCardigannSearchModes(new Dictionary<string, List<string>>
                {
                    {"search", new List<string>{"q"}},
                    {"tv-search", new List<string> {"bad"}} // unsupported param
                });
                Assert.Fail();
            }
            catch (Exception)
            {
                // ignored
            }
        }

        [Test]
        public void TestParseMovieSearchParams()
        {
            var torznabCaps = new TorznabCapabilities();
            torznabCaps.ParseCardigannSearchModes(new Dictionary<string, List<string>>
            {
                {"search", new List<string>{"q"}},
                {"movie-search", null}
            });
            Assert.IsEmpty(torznabCaps.MovieSearchParams);

            torznabCaps = new TorznabCapabilities();
            torznabCaps.ParseCardigannSearchModes(new Dictionary<string, List<string>>
            {
                {"search", new List<string>{"q"}},
                {"movie-search", new List<string>()}
            });
            Assert.IsEmpty(torznabCaps.MovieSearchParams);

            torznabCaps = new TorznabCapabilities();
            torznabCaps.ParseCardigannSearchModes(new Dictionary<string, List<string>>
            {
                {"search", new List<string>{"q"}},
                {"movie-search", new List<string> {"q", "imdbid"}}
            });
            Assert.AreEqual(new List<MovieSearchParam> { MovieSearchParam.Q, MovieSearchParam.ImdbId }, torznabCaps.MovieSearchParams);

            torznabCaps = new TorznabCapabilities();
            try {
                torznabCaps.ParseCardigannSearchModes(new Dictionary<string, List<string>>
                {
                    {"search", new List<string>{"q"}},
                    {"movie-search", new List<string> {"q", "q"}} // duplicate param
                });
                Assert.Fail();
            }
            catch (Exception)
            {
                // ignored
            }

            torznabCaps = new TorznabCapabilities();
            try {
                torznabCaps.ParseCardigannSearchModes(new Dictionary<string, List<string>>
                {
                    {"search", new List<string>{"q"}},
                    {"movie-search", new List<string> {"bad"}} // unsupported param
                });
                Assert.Fail();
            }
            catch (Exception)
            {
                // ignored
            }
        }

        [Test]
        public void TestParseMusicSearchParams()
        {
            var torznabCaps = new TorznabCapabilities();
            torznabCaps.ParseCardigannSearchModes(new Dictionary<string, List<string>>
            {
                {"search", new List<string>{"q"}},
                {"music-search", null}
            });
            Assert.IsEmpty(torznabCaps.MovieSearchParams);

            torznabCaps = new TorznabCapabilities();
            torznabCaps.ParseCardigannSearchModes(new Dictionary<string, List<string>>
            {
                {"search", new List<string>{"q"}},
                {"music-search", new List<string>()}
            });
            Assert.IsEmpty(torznabCaps.MovieSearchParams);

            torznabCaps = new TorznabCapabilities();
            torznabCaps.ParseCardigannSearchModes(new Dictionary<string, List<string>>
            {
                {"search", new List<string>{"q"}},
                {"music-search", new List<string> {"q", "label"}}
            });
            Assert.AreEqual(new List<MusicSearchParam> { MusicSearchParam.Q, MusicSearchParam.Label }, torznabCaps.MusicSearchParams);

            torznabCaps = new TorznabCapabilities();
            try {
                torznabCaps.ParseCardigannSearchModes(new Dictionary<string, List<string>>
                {
                    {"search", new List<string>{"q"}},
                    {"music-search", new List<string> {"q", "q"}} // duplicate param
                });
                Assert.Fail();
            }
            catch (Exception)
            {
                // ignored
            }

            torznabCaps = new TorznabCapabilities();
            try {
                torznabCaps.ParseCardigannSearchModes(new Dictionary<string, List<string>>
                {
                    {"search", new List<string>{"q"}},
                    {"music-search", new List<string> {"bad"}} // unsupported param
                });
                Assert.Fail();
            }
            catch (Exception)
            {
                // ignored
            }
        }

        [Test]
        public void TestParseBookSearchParams()
        {
            var torznabCaps = new TorznabCapabilities();
            torznabCaps.ParseCardigannSearchModes(new Dictionary<string, List<string>>
            {
                {"search", new List<string>{"q"}},
                {"book-search", null}
            });
            Assert.IsEmpty(torznabCaps.MovieSearchParams);

            torznabCaps = new TorznabCapabilities();
            torznabCaps.ParseCardigannSearchModes(new Dictionary<string, List<string>>
            {
                {"search", new List<string>{"q"}},
                {"book-search", new List<string>()}
            });
            Assert.IsEmpty(torznabCaps.MovieSearchParams);

            torznabCaps = new TorznabCapabilities();
            torznabCaps.ParseCardigannSearchModes(new Dictionary<string, List<string>>
            {
                {"search", new List<string>{"q"}},
                {"book-search", new List<string> {"q", "title"}}
            });
            Assert.AreEqual(new List<BookSearchParam> { BookSearchParam.Q, BookSearchParam.Title }, torznabCaps.BookSearchParams);

            torznabCaps = new TorznabCapabilities();
            try {
                torznabCaps.ParseCardigannSearchModes(new Dictionary<string, List<string>>
                {
                    {"search", new List<string>{"q"}},
                    {"book-search", new List<string> {"q", "q"}} // duplicate param
                });
                Assert.Fail();
            }
            catch (Exception)
            {
                // ignored
            }

            torznabCaps = new TorznabCapabilities();
            try {
                torznabCaps.ParseCardigannSearchModes(new Dictionary<string, List<string>>
                {
                    {"search", new List<string>{"q"}},
                    {"book-search", new List<string> {"bad"}} // unsupported param
                });
                Assert.Fail();
            }
            catch (Exception)
            {
                // ignored
            }
        }

        [Test]
        public void TestTorznabCaps()
        {
            // test header
            var torznabCaps = new TorznabCapabilities();
            var xDocument = torznabCaps.GetXDocument();
            Assert.AreEqual("caps", xDocument.Root?.Name.LocalName);
            Assert.AreEqual("Jackett", xDocument.Root?.Element("server")?.Attribute("title")?.Value);
            Assert.True(xDocument.Root?.Element("searching")?.HasElements);
            Assert.False(xDocument.Root?.Element("categories")?.HasElements);

            // test all features disabled
            torznabCaps = new TorznabCapabilities
            {
                SearchAvailable = false
            };
            xDocument = torznabCaps.GetXDocument();
            var xDocumentSearching = xDocument.Root?.Element("searching");
            Assert.AreEqual("no", xDocumentSearching?.Element("search")?.Attribute("available")?.Value);
            Assert.AreEqual("q", xDocumentSearching?.Element("search")?.Attribute("supportedParams")?.Value);
            Assert.AreEqual("no", xDocumentSearching?.Element("tv-search")?.Attribute("available")?.Value);
            Assert.AreEqual("q", xDocumentSearching?.Element("tv-search")?.Attribute("supportedParams")?.Value);
            Assert.AreEqual("no", xDocumentSearching?.Element("movie-search")?.Attribute("available")?.Value);
            Assert.AreEqual("q", xDocumentSearching?.Element("movie-search")?.Attribute("supportedParams")?.Value);
            Assert.AreEqual("no", xDocumentSearching?.Element("music-search")?.Attribute("available")?.Value);
            Assert.AreEqual("q", xDocumentSearching?.Element("music-search")?.Attribute("supportedParams")?.Value);
            Assert.AreEqual("no", xDocumentSearching?.Element("audio-search")?.Attribute("available")?.Value);
            Assert.AreEqual("q", xDocumentSearching?.Element("audio-search")?.Attribute("supportedParams")?.Value);
            Assert.AreEqual("no", xDocumentSearching?.Element("book-search")?.Attribute("available")?.Value);
            Assert.AreEqual("q", xDocumentSearching?.Element("book-search")?.Attribute("supportedParams")?.Value);

            // test all features enabled
            torznabCaps = new TorznabCapabilities
            {
                SearchAvailable = true,
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId, TvSearchParam.TvdbId, TvSearchParam.RId
                },
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q, MovieSearchParam.ImdbId, MovieSearchParam.TmdbId
                },
                MusicSearchParams = new List<MusicSearchParam>
                {
                    MusicSearchParam.Q, MusicSearchParam.Album, MusicSearchParam.Artist, MusicSearchParam.Label, MusicSearchParam.Year
                },
                BookSearchParams = new List<BookSearchParam>
                {
                    BookSearchParam.Q, BookSearchParam.Title, BookSearchParam.Author
                },
            };
            xDocument = torznabCaps.GetXDocument();
            xDocumentSearching = xDocument.Root?.Element("searching");
            Assert.AreEqual("yes", xDocumentSearching?.Element("search")?.Attribute("available")?.Value);
            Assert.AreEqual("q", xDocumentSearching?.Element("search")?.Attribute("supportedParams")?.Value);
            Assert.AreEqual("yes", xDocumentSearching?.Element("tv-search")?.Attribute("available")?.Value);
            Assert.AreEqual("q,season,ep,tvdbid,rid", xDocumentSearching?.Element("tv-search")?.Attribute("supportedParams")?.Value);
            Assert.AreEqual("yes", xDocumentSearching?.Element("movie-search")?.Attribute("available")?.Value);
            Assert.AreEqual("q,imdbid,tmdbid", xDocumentSearching?.Element("movie-search")?.Attribute("supportedParams")?.Value);
            Assert.AreEqual("yes", xDocumentSearching?.Element("music-search")?.Attribute("available")?.Value);
            Assert.AreEqual("q,album,artist,label,year", xDocumentSearching?.Element("music-search")?.Attribute("supportedParams")?.Value);
            Assert.AreEqual("yes", xDocumentSearching?.Element("audio-search")?.Attribute("available")?.Value);
            Assert.AreEqual("q,album,artist,label,year", xDocumentSearching?.Element("audio-search")?.Attribute("supportedParams")?.Value);
            Assert.AreEqual("yes", xDocumentSearching?.Element("book-search")?.Attribute("available")?.Value);
            Assert.AreEqual("q,title,author", xDocumentSearching?.Element("book-search")?.Attribute("supportedParams")?.Value);

            // test categories
            torznabCaps = new TorznabCapabilities();
            torznabCaps.Categories.AddCategoryMapping("c1", TorznabCatType.MoviesSD); // child category
            xDocument = torznabCaps.GetXDocument();
            var xDocumentCategories = xDocument.Root?.Element("categories")?.Elements("category").ToList();
            Assert.AreEqual(1, xDocumentCategories?.Count);
            Assert.AreEqual(TorznabCatType.MoviesSD.ID.ToString(), xDocumentCategories?.First().Attribute("id")?.Value);
            Assert.AreEqual(TorznabCatType.MoviesSD.Name, xDocumentCategories?.First().Attribute("name")?.Value);

            // TODO: child category is duplicated. should we add just parent and child without other subcats?
            torznabCaps = new TorznabCapabilities();
            torznabCaps.Categories.AddCategoryMapping("c1", TorznabCatType.Movies); // parent and child category
            torznabCaps.Categories.AddCategoryMapping("c2", TorznabCatType.MoviesSD);
            xDocument = torznabCaps.GetXDocument();
            xDocumentCategories = xDocument.Root?.Element("categories")?.Elements("category").ToList();
            Assert.AreEqual(2, xDocumentCategories?.Count);
            Assert.AreEqual(TorznabCatType.Movies.ID.ToString(), xDocumentCategories?.First().Attribute("id")?.Value);
            Assert.AreEqual(TorznabCatType.Movies.Name, xDocumentCategories?.First().Attribute("name")?.Value);
            Assert.AreEqual(TorznabCatType.MoviesSD.ID.ToString(), xDocumentCategories?[1].Attribute("id")?.Value);
            Assert.AreEqual(TorznabCatType.MoviesSD.Name, xDocumentCategories?[1].Attribute("name")?.Value);
            var xDocumentSubCategories = xDocumentCategories?.First()?.Elements("subcat").ToList();
            Assert.AreEqual(9, xDocumentSubCategories?.Count);
            Assert.AreEqual(TorznabCatType.MoviesForeign.ID.ToString(), xDocumentSubCategories?.First().Attribute("id")?.Value);
            Assert.AreEqual(TorznabCatType.MoviesForeign.Name, xDocumentSubCategories?.First().Attribute("name")?.Value);

            torznabCaps = new TorznabCapabilities();
            torznabCaps.Categories.AddCategoryMapping("c1", new TorznabCategory(100001, "CustomCat")); // custom category
            torznabCaps.Categories.AddCategoryMapping("c2", TorznabCatType.MoviesSD);
            xDocument = torznabCaps.GetXDocument();
            xDocumentCategories = xDocument.Root?.Element("categories")?.Elements("category").ToList();
            Assert.AreEqual(2, xDocumentCategories?.Count);
            Assert.AreEqual("100001", xDocumentCategories?[0].Attribute("id")?.Value); // custom cats are first in the list
            Assert.AreEqual("CustomCat", xDocumentCategories?[0].Attribute("name")?.Value);
            Assert.AreEqual(TorznabCatType.MoviesSD.ID.ToString(), xDocumentCategories?[1].Attribute("id")?.Value);
            Assert.AreEqual(TorznabCatType.MoviesSD.Name, xDocumentCategories?[1].Attribute("name")?.Value);
        }

        [Test]
        public void TestTorznabCapsCategories()
        {
            var torznabCaps = new TorznabCapabilities();
            TestCategories.AddTestCategories(torznabCaps.Categories);

            // test Torznab caps (XML) => more in Common.Model.TorznabCapabilitiesTests
            var xDocument = torznabCaps.GetXDocument();
            var xDocumentCategories = xDocument.Root?.Element("categories")?.Elements("category").ToList();
            Assert.AreEqual(7, xDocumentCategories?.Count);
            Assert.AreEqual("100044", xDocumentCategories?[0].Attribute("id")?.Value);
            Assert.AreEqual("100045", xDocumentCategories?[1].Attribute("id")?.Value);
            Assert.AreEqual("1030", xDocumentCategories?[2].Attribute("id")?.Value);
            Assert.AreEqual("1040", xDocumentCategories?[3].Attribute("id")?.Value);
            Assert.AreEqual("2000", xDocumentCategories?[4].Attribute("id")?.Value); // Movies
            Assert.AreEqual("2030", xDocumentCategories?[5].Attribute("id")?.Value);
            Assert.AreEqual("7030", xDocumentCategories?[6].Attribute("id")?.Value);
            Assert.AreEqual(9, xDocumentCategories?[4]?.Elements("subcat").ToList().Count); // Movies
        }

        [Test]
        public void TestTorznabConcat()
        {
            var torznabCaps1 = new TorznabCapabilities();
            var torznabCaps2 = new TorznabCapabilities();
            var res = TorznabCapabilities.Concat(torznabCaps1, torznabCaps2);

            Assert.True(res.SearchAvailable);
            Assert.IsEmpty(res.TvSearchParams);
            Assert.IsEmpty(res.MovieSearchParams);
            Assert.IsEmpty(res.MusicSearchParams);
            Assert.IsEmpty(res.BookSearchParams);
            Assert.IsEmpty(res.Categories.GetTorznabCategories());

            torznabCaps1 = new TorznabCapabilities
            {
                SearchAvailable = false,
                TvSearchParams = new List<TvSearchParam> {TvSearchParam.Q},
                MovieSearchParams = new List<MovieSearchParam> {MovieSearchParam.Q},
                MusicSearchParams = new List<MusicSearchParam> {MusicSearchParam.Q},
                BookSearchParams = new List<BookSearchParam> {BookSearchParam.Q}
            };
            torznabCaps1.Categories.AddCategoryMapping("1", TorznabCatType.Movies);
            torznabCaps1.Categories.AddCategoryMapping("c1", new TorznabCategory(100001, "CustomCat1"));
            torznabCaps2 = new TorznabCapabilities
            {
                SearchAvailable = false,
                TvSearchParams = new List<TvSearchParam> {TvSearchParam.Season},
                MovieSearchParams = new List<MovieSearchParam> {MovieSearchParam.ImdbId},
                MusicSearchParams = new List<MusicSearchParam> {MusicSearchParam.Artist},
                BookSearchParams = new List<BookSearchParam> {BookSearchParam.Title}
            };
            torznabCaps2.Categories.AddCategoryMapping("2", TorznabCatType.TVAnime);
            torznabCaps2.Categories.AddCategoryMapping("c2", new TorznabCategory(100002, "CustomCat2"));
            res = TorznabCapabilities.Concat(torznabCaps1, torznabCaps2);

            Assert.False(res.SearchAvailable);
            Assert.True(res.TvSearchParams.Count == 2);
            Assert.True(res.MovieSearchParams.Count == 2);
            Assert.True(res.MusicSearchParams.Count == 2);
            Assert.True(res.BookSearchParams.Count == 2);
            Assert.True(res.Categories.GetTorznabCategories().Count == 3); // only CustomCat2 is removed
        }
    }
}
