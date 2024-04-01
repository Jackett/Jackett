using System;
using System.Collections.Generic;
using System.Linq;
using Jackett.Common.Helpers;
using Jackett.Common.Models;
using Jackett.Test.TestHelpers;
using NLog;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Jackett.Test.Common.Indexers
{
    [TestFixture]
    public class BaseWebIndexerTests
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        [Test]
        public void TestConstructor()
        {
            var indexer = new TestWebIndexer(_logger);
            var caps = indexer.TorznabCaps;

            Assert.True(caps.SearchAvailable);
            Assert.IsEmpty(caps.TvSearchParams);
            Assert.False(caps.TvSearchAvailable);
            Assert.False(caps.TvSearchSeasonAvailable);
            Assert.False(caps.TvSearchEpAvailable);
            Assert.False(caps.TvSearchImdbAvailable);
            Assert.False(caps.TvSearchTvdbAvailable);
            Assert.False(caps.TvSearchTvRageAvailable);
            Assert.IsEmpty(caps.MovieSearchParams);
            Assert.False(caps.MovieSearchAvailable);
            Assert.False(caps.MovieSearchImdbAvailable);
            Assert.False(caps.MovieSearchTmdbAvailable);
            Assert.IsEmpty(caps.MusicSearchParams);
            Assert.False(caps.MusicSearchAvailable);
            Assert.False(caps.MusicSearchAlbumAvailable);
            Assert.False(caps.MusicSearchArtistAvailable);
            Assert.False(caps.MusicSearchLabelAvailable);
            Assert.False(caps.MusicSearchYearAvailable);
            Assert.IsEmpty(caps.BookSearchParams);
            Assert.False(caps.BookSearchAvailable);
            Assert.False(caps.BookSearchTitleAvailable);
            Assert.False(caps.BookSearchAuthorAvailable);
            Assert.AreEqual(0, caps.Categories.GetTorznabCategoryTree().Count);
        }

        [Test]
        public void TestFilterResultsCategories()
        {
            var indexer = new TestWebIndexer(_logger);
            indexer.AddTestCategories();

            var results = new List<ReleaseInfo>
            {
                new ReleaseInfo
                {
                    Title = "Release 1",
                    Category = new List<int> { TorznabCatType.Movies.ID },
                    Size = 2.Gigabytes()
                },
                new ReleaseInfo
                {
                    Title = "Release 2",
                    Category = new List<int> { TorznabCatType.MoviesSD.ID },
                    Size = 2.Gigabytes()
                },
                new ReleaseInfo
                {
                    Title = "Release 3",
                    Category = new List<int> { TorznabCatType.BooksEBook.ID, 100004 }, // torznab (mandatory) + custom cat
                    Size = 2.Gigabytes()
                },
                new ReleaseInfo
                {
                    Title = "Release 4",
                    Category = new List<int> { TorznabCatType.AudioLossless.ID }, // unsupported category in this indexer
                    Size = 2.Gigabytes()
                },
                new ReleaseInfo
                {
                    Title = "Release 5"
                }
            };

            var query = new TorznabQuery(); // without categories
            var filteredResults = indexer._FilterResults(query, results).ToList();
            Assert.AreEqual(4, filteredResults.Count);

            query = new TorznabQuery // with child category
            {
                Categories = new[] { TorznabCatType.MoviesSD.ID }
            };
            filteredResults = indexer._FilterResults(query, results).ToList();
            Assert.AreEqual(1, filteredResults.Count);
            Assert.AreEqual(TorznabCatType.MoviesSD.ID, filteredResults[0].Category.First());

            query = new TorznabQuery // with parent category
            {
                Categories = new[] { TorznabCatType.Movies.ID }
            };
            filteredResults = indexer._FilterResults(query, results).ToList();
            Assert.AreEqual(2, filteredResults.Count);
            Assert.AreEqual(TorznabCatType.Movies.ID, filteredResults[0].Category.First());
            Assert.AreEqual(TorznabCatType.MoviesSD.ID, filteredResults[1].Category.First());

            query = new TorznabQuery // with custom category
            {
                Categories = new[] { 100004 }
            };
            filteredResults = indexer._FilterResults(query, results).ToList();
            Assert.AreEqual(1, filteredResults.Count);
            Assert.AreEqual(TorznabCatType.BooksEBook.ID, filteredResults[0].Category.First());
        }

        [Test]
        public void TestFilterResultsLimit()
        {
            var indexer = new TestWebIndexer(_logger);

            var results = new List<ReleaseInfo>
            {
                new ReleaseInfo
                {
                    Title = "Release 1",
                    Category = new List<int> { TorznabCatType.Movies.ID },
                    Size = 2.Gigabytes()
                },
                new ReleaseInfo
                {
                    Title = "Release 2",
                    Category = new List<int> { TorznabCatType.Movies.ID },
                    Size = 2.Gigabytes()
                }
            };

            var query = new TorznabQuery();
            var filteredResults = indexer._FilterResults(query, results).ToList();
            Assert.AreEqual(2, filteredResults.Count);

            query = new TorznabQuery
            {
                Limit = 1
            };
            filteredResults = indexer._FilterResults(query, results).ToList();
            Assert.AreEqual(1, filteredResults.Count);
        }

        [Test]
        public void TestFixResultsOriginPublishDate()
        {
            var indexer = new TestWebIndexer(_logger);
            var query = new TorznabQuery();
            var results = new List<ReleaseInfo>
            {
                new ReleaseInfo
                {
                    Title = "Release",
                    PublishDate = new DateTime(3000, 1, 1) // future date
                }
            };

            // fix origin and publish date
            Assert.AreEqual(null, results.First().Origin);
            Assert.AreEqual(3000, results.First().PublishDate.Year);
            var fixedResults = indexer._FixResults(query, results).ToList();
            Assert.AreEqual(indexer.Id, fixedResults.First().Origin.Id);
            Assert.AreEqual(DateTime.Now.Year, fixedResults.First().PublishDate.Year);
        }

        [Test]
        public void TestFixResultsMagnet()
        {
            var indexer = new TestWebIndexer(_logger);
            var query = new TorznabQuery();

            // get info_hash from magnet
            var results = new List<ReleaseInfo>
            {
                new ReleaseInfo
                {
                    Title = "Release",
                    MagnetUri = new Uri("magnet:?xt=urn:btih:3333333333333333333333333333333333333333&dn=Title&tr=udp%3A%2F%2Ftracker.com%3A6969")
                }
            };
            Assert.AreEqual(null, results.First().InfoHash);
            var fixedResults = indexer._FixResults(query, results).ToList();
            Assert.AreEqual("3333333333333333333333333333333333333333", fixedResults.First().InfoHash);

            // build magnet from info_hash (private site), not allowed
            results = new List<ReleaseInfo>
            {
                new ReleaseInfo
                {
                    Title = "Tracker Title",
                    InfoHash = "3333333333333333333333333333333333333333"
                }
            };
            Assert.AreEqual(null, results.First().MagnetUri);
            fixedResults = indexer._FixResults(query, results).ToList();
            Assert.AreEqual(null, fixedResults.First().MagnetUri);

            // build magnet from info_hash (public, semi-private sites)
            indexer.SetType("public");
            Assert.AreEqual(null, results.First().MagnetUri);
            fixedResults = indexer._FixResults(query, results).ToList();
            Assert.True(fixedResults.First().MagnetUri.ToString().Contains("3333333333333333333333333333333333333333"));
        }

        [Test]
        public void TestAddCategoryMapping()
        {
            var indexer = new TestWebIndexer(_logger);

            // you can find more complex tests in TorznabCapabilitiesCategoriesTests.cs
            indexer._AddCategoryMapping("11", TorznabCatType.MoviesSD, "MoviesSD");
            var expected = new List<TorznabCategory>
            {
                TorznabCatType.Movies.CopyWithoutSubCategories(),
                new TorznabCategory(100011, "MoviesSD")
            };
            expected[0].SubCategories.Add(TorznabCatType.MoviesSD.CopyWithoutSubCategories());
            TestCategories.CompareCategoryTrees(expected, indexer.TorznabCaps.Categories.GetTorznabCategoryTree());

            indexer._AddCategoryMapping(14, TorznabCatType.MoviesHD);
            expected[0].SubCategories.Add(TorznabCatType.MoviesHD.CopyWithoutSubCategories());
            TestCategories.CompareCategoryTrees(expected, indexer.TorznabCaps.Categories.GetTorznabCategoryTree());
        }

        [Test]
        public void TestAddMultiCategoryMapping()
        {
            var indexer = new TestWebIndexer(_logger);

            indexer._AddMultiCategoryMapping(TorznabCatType.MoviesHD, 19, 18);
            Assert.AreEqual(1, indexer.TorznabCaps.Categories.GetTorznabCategoryTree().Count);
        }

        [Test]
        public void TestMapTorznabCapsToTrackers()
        {
            var indexer = new TestWebIndexer(_logger);
            indexer.AddTestCategories();

            // you can find more complex tests in TorznabCapabilitiesCategoriesTests.cs
            var query = new TorznabQuery // int category with subcategories (parent cat)
            {
                Categories = new[] { TorznabCatType.Movies.ID }
            };
            var trackerCats = indexer._MapTorznabCapsToTrackers(query);
            Assert.AreEqual(2, trackerCats.Count);
            Assert.AreEqual("1", trackerCats[0]); // Movies
            Assert.AreEqual("mov_sd", trackerCats[1]); // Movies SD
        }

        [Test]
        public void TestMapTrackerCatToNewznab()
        {
            var indexer = new TestWebIndexer(_logger);
            indexer.AddTestCategories();

            // you can find more complex tests in TorznabCapabilitiesCategoriesTests.cs
            var torznabCats = indexer._MapTrackerCatToNewznab("1").ToList();
            Assert.AreEqual(1, torznabCats.Count);
            Assert.AreEqual(2000, torznabCats[0]);
        }

        [Test]
        public void TestMapTrackerCatDescToNewznab()
        {
            var indexer = new TestWebIndexer(_logger);
            indexer.AddTestCategories();

            // you can find more complex tests in TorznabCapabilitiesCategoriesTests.cs
            var torznabCats = indexer._MapTrackerCatDescToNewznab("Console/Wii_c").ToList();
            Assert.AreEqual(2, torznabCats.Count);
            Assert.AreEqual(1030, torznabCats[0]);
            Assert.AreEqual(137107, torznabCats[1]);
        }
    }
}
