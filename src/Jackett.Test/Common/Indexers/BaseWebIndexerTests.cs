using System.Collections.Generic;
using System.Linq;
using Jackett.Common.Models;
using Jackett.Test.TestHelpers;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Jackett.Test.Common.Indexers
{
    [TestFixture]
    public class BaseWebIndexerTests
    {
        [Test]
        public void TestConstructor()
        {
            var indexer = new TestWebIndexer();
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
        public void TestFilterResults()
        {
            var indexer = new TestWebIndexer();
            var results = new List<ReleaseInfo>
            {
                new ReleaseInfo
                {
                    Category = new List<int> { TorznabCatType.Movies.ID }
                },
                new ReleaseInfo
                {
                    Category = new List<int> { TorznabCatType.MoviesSD.ID }
                },
                new ReleaseInfo
                {
                    Category = new List<int> { TorznabCatType.BooksEBook.ID, 100004 } // torznab (mandatory) + custom cat
                },
                new ReleaseInfo()
            };

            var query = new TorznabQuery(); // without categories
            var filteredResults = indexer._FilterResults(query, results).ToList();
            Assert.AreEqual(4, filteredResults.Count);

            query = new TorznabQuery // with child category
            {
                Categories = new [] { TorznabCatType.MoviesSD.ID }
            };
            filteredResults = indexer._FilterResults(query, results).ToList();
            Assert.AreEqual(2, filteredResults.Count);
            Assert.AreEqual(TorznabCatType.MoviesSD.ID, filteredResults[0].Category.First());
            Assert.AreEqual(null, filteredResults[1].Category);

            query = new TorznabQuery // with parent category
            {
                Categories = new [] { TorznabCatType.Movies.ID }
            };
            filteredResults = indexer._FilterResults(query, results).ToList();
            Assert.AreEqual(3, filteredResults.Count);
            Assert.AreEqual(TorznabCatType.Movies.ID, filteredResults[0].Category.First());
            Assert.AreEqual(TorznabCatType.MoviesSD.ID, filteredResults[1].Category.First());
            Assert.AreEqual(null, filteredResults[2].Category);

            query = new TorznabQuery // with custom category
            {
                Categories = new [] { 100004 }
            };
            filteredResults = indexer._FilterResults(query, results).ToList();
            Assert.AreEqual(2, filteredResults.Count);
            Assert.AreEqual(TorznabCatType.BooksEBook.ID, filteredResults[0].Category.First());
            Assert.AreEqual(null, filteredResults[1].Category);
        }

        [Test]
        public void TestAddCategoryMapping()
        {
            var indexer = new TestWebIndexer();

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
            var indexer = new TestWebIndexer();

            indexer._AddMultiCategoryMapping(TorznabCatType.MoviesHD,19, 18);
            Assert.AreEqual(1, indexer.TorznabCaps.Categories.GetTorznabCategoryTree().Count);
        }

        [Test]
        public void TestMapTorznabCapsToTrackers()
        {
            var indexer = new TestWebIndexer();
            indexer.AddTestCategories();

            // you can find more complex tests in TorznabCapabilitiesCategoriesTests.cs
            var query = new TorznabQuery // int category with subcategories (parent cat)
            {
                Categories = new [] { TorznabCatType.Movies.ID }
            };
            var trackerCats = indexer._MapTorznabCapsToTrackers(query);
            Assert.AreEqual(2, trackerCats.Count);
            Assert.AreEqual("1", trackerCats[0]); // Movies
            Assert.AreEqual("mov_sd", trackerCats[1]); // Movies SD
        }

        [Test]
        public void TestMapTrackerCatToNewznab()
        {
            var indexer = new TestWebIndexer();
            indexer.AddTestCategories();

            // you can find more complex tests in TorznabCapabilitiesCategoriesTests.cs
            // TODO: this is wrong, custom cat 100001 doesn't exists (it's not defined by us)
            var torznabCats = indexer._MapTrackerCatToNewznab("1").ToList();
            Assert.AreEqual(2, torznabCats.Count);
            Assert.AreEqual(2000, torznabCats[0]);
            Assert.AreEqual(100001, torznabCats[1]);
        }

        [Test]
        public void TestMapTrackerCatDescToNewznab()
        {
            var indexer = new TestWebIndexer();
            indexer.AddTestCategories();

            // you can find more complex tests in TorznabCapabilitiesCategoriesTests.cs
            var torznabCats = indexer._MapTrackerCatDescToNewznab("Console/Wii_c").ToList();
            Assert.AreEqual(1, torznabCats.Count);
            Assert.AreEqual(1030, torznabCats[0]);
        }
    }
}
