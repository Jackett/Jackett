using System;
using System.Collections.Generic;
using System.Linq;
using Jackett.Common.Models;
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
            var xDoumentSearching = xDocument.Root?.Element("searching");
            Assert.AreEqual("no", xDoumentSearching?.Element("search")?.Attribute("available")?.Value);
            Assert.AreEqual("q", xDoumentSearching?.Element("search")?.Attribute("supportedParams")?.Value);
            Assert.AreEqual("no", xDoumentSearching?.Element("tv-search")?.Attribute("available")?.Value);
            Assert.AreEqual("q", xDoumentSearching?.Element("tv-search")?.Attribute("supportedParams")?.Value);
            Assert.AreEqual("no", xDoumentSearching?.Element("movie-search")?.Attribute("available")?.Value);
            Assert.AreEqual("q", xDoumentSearching?.Element("movie-search")?.Attribute("supportedParams")?.Value);
            Assert.AreEqual("no", xDoumentSearching?.Element("music-search")?.Attribute("available")?.Value);
            Assert.AreEqual("q", xDoumentSearching?.Element("music-search")?.Attribute("supportedParams")?.Value);
            Assert.AreEqual("no", xDoumentSearching?.Element("audio-search")?.Attribute("available")?.Value);
            Assert.AreEqual("q", xDoumentSearching?.Element("audio-search")?.Attribute("supportedParams")?.Value);
            Assert.AreEqual("no", xDoumentSearching?.Element("book-search")?.Attribute("available")?.Value);
            Assert.AreEqual("q", xDoumentSearching?.Element("book-search")?.Attribute("supportedParams")?.Value);

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
            xDoumentSearching = xDocument.Root?.Element("searching");
            Assert.AreEqual("yes", xDoumentSearching?.Element("search")?.Attribute("available")?.Value);
            Assert.AreEqual("q", xDoumentSearching?.Element("search")?.Attribute("supportedParams")?.Value);
            Assert.AreEqual("yes", xDoumentSearching?.Element("tv-search")?.Attribute("available")?.Value);
            Assert.AreEqual("q,season,ep,tvdbid,rid", xDoumentSearching?.Element("tv-search")?.Attribute("supportedParams")?.Value);
            Assert.AreEqual("yes", xDoumentSearching?.Element("movie-search")?.Attribute("available")?.Value);
            Assert.AreEqual("q,imdbid,tmdbid", xDoumentSearching?.Element("movie-search")?.Attribute("supportedParams")?.Value);
            Assert.AreEqual("yes", xDoumentSearching?.Element("music-search")?.Attribute("available")?.Value);
            Assert.AreEqual("q,album,artist,label,year", xDoumentSearching?.Element("music-search")?.Attribute("supportedParams")?.Value);
            Assert.AreEqual("yes", xDoumentSearching?.Element("audio-search")?.Attribute("available")?.Value);
            Assert.AreEqual("q,album,artist,label,year", xDoumentSearching?.Element("audio-search")?.Attribute("supportedParams")?.Value);
            Assert.AreEqual("yes", xDoumentSearching?.Element("book-search")?.Attribute("available")?.Value);
            Assert.AreEqual("q,title,author", xDoumentSearching?.Element("book-search")?.Attribute("supportedParams")?.Value);

            // test categories
            torznabCaps = new TorznabCapabilities();
            torznabCaps.Categories.AddCategoryMapping("c1", TorznabCatType.MoviesSD); // child category
            xDocument = torznabCaps.GetXDocument();
            var xDoumentCategories = xDocument.Root?.Element("categories")?.Elements("category").ToList();
            Assert.AreEqual(1, xDoumentCategories?.Count);
            Assert.AreEqual(TorznabCatType.MoviesSD.ID.ToString(), xDoumentCategories?.First().Attribute("id")?.Value);
            Assert.AreEqual(TorznabCatType.MoviesSD.Name, xDoumentCategories?.First().Attribute("name")?.Value);

            // TODO: child category is duplicated. should we add just parent and child without other subcats?
            torznabCaps = new TorznabCapabilities();
            torznabCaps.Categories.AddCategoryMapping("c1", TorznabCatType.Movies); // parent and child category
            torznabCaps.Categories.AddCategoryMapping("c2", TorznabCatType.MoviesSD);
            xDocument = torznabCaps.GetXDocument();
            xDoumentCategories = xDocument.Root?.Element("categories")?.Elements("category").ToList();
            Assert.AreEqual(2, xDoumentCategories?.Count);
            Assert.AreEqual(TorznabCatType.Movies.ID.ToString(), xDoumentCategories?.First().Attribute("id")?.Value);
            Assert.AreEqual(TorznabCatType.Movies.Name, xDoumentCategories?.First().Attribute("name")?.Value);
            Assert.AreEqual(TorznabCatType.MoviesSD.ID.ToString(), xDoumentCategories?[1].Attribute("id")?.Value);
            Assert.AreEqual(TorznabCatType.MoviesSD.Name, xDoumentCategories?[1].Attribute("name")?.Value);
            var xDoumentSubCategories = xDoumentCategories?.First()?.Elements("subcat").ToList();
            Assert.AreEqual(9, xDoumentSubCategories?.Count);
            Assert.AreEqual(TorznabCatType.MoviesForeign.ID.ToString(), xDoumentSubCategories?.First().Attribute("id")?.Value);
            Assert.AreEqual(TorznabCatType.MoviesForeign.Name, xDoumentSubCategories?.First().Attribute("name")?.Value);

            torznabCaps = new TorznabCapabilities();
            torznabCaps.Categories.AddCategoryMapping("c1", new TorznabCategory(100001, "CustomCat")); // custom category
            torznabCaps.Categories.AddCategoryMapping("c2", TorznabCatType.MoviesSD);
            xDocument = torznabCaps.GetXDocument();
            xDoumentCategories = xDocument.Root?.Element("categories")?.Elements("category").ToList();
            Assert.AreEqual(2, xDoumentCategories?.Count);
            Assert.AreEqual("100001", xDoumentCategories?[0].Attribute("id")?.Value); // custom cats are first in the list
            Assert.AreEqual("CustomCat", xDoumentCategories?[0].Attribute("name")?.Value);
            Assert.AreEqual(TorznabCatType.MoviesSD.ID.ToString(), xDoumentCategories?[1].Attribute("id")?.Value);
            Assert.AreEqual(TorznabCatType.MoviesSD.Name, xDoumentCategories?[1].Attribute("name")?.Value);
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
