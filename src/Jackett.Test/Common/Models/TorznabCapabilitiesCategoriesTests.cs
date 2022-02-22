using System.Collections.Generic;
using System.Linq;
using Jackett.Common.Models;
using Jackett.Test.TestHelpers;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Jackett.Test.Common.Models
{
    [TestFixture]
    public class TorznabCapabilitiesCategoriesTests
    {
        [Test]
        public void TestConstructor()
        {
            var tcc = new TorznabCapabilitiesCategories();
            Assert.IsEmpty(tcc.GetTorznabCategoryTree());
            Assert.IsEmpty(tcc.GetTrackerCategories());
        }

        [Test]
        public void TestGetTrackerCategories()
        {
            var tcc = CreateTestDataset();
            var trackerCats = tcc.GetTrackerCategories();
            Assert.AreEqual(6, trackerCats.Count);
            Assert.AreEqual("1", trackerCats[0]);
        }

        [Test]
        public void TestGetTorznabCategoryTree()
        {
            var tcc = CreateTestDataset();

            // unsorted tree
            var cats = tcc.GetTorznabCategoryTree();
            var expected = new List<TorznabCategory>
            {
                TorznabCatType.Movies.CopyWithoutSubCategories(),
                TorznabCatType.Books.CopyWithoutSubCategories(),
                TorznabCatType.Console.CopyWithoutSubCategories(),
                new TorznabCategory(100044, "Console/Xbox_c"),
                new TorznabCategory(137107, "Console/Wii_c"),
                new TorznabCategory(100040, "Console/Xbox_c2")
            };
            expected[0].SubCategories.Add(TorznabCatType.MoviesSD.CopyWithoutSubCategories());
            expected[1].SubCategories.Add(TorznabCatType.BooksComics.CopyWithoutSubCategories());
            expected[2].SubCategories.Add(TorznabCatType.ConsoleXBox.CopyWithoutSubCategories());
            expected[2].SubCategories.Add(TorznabCatType.ConsoleWii.CopyWithoutSubCategories());
            TestCategories.CompareCategoryTrees(expected, cats);

            // sorted tree
            cats = tcc.GetTorznabCategoryTree(true);
            expected = new List<TorznabCategory>
            {
                TorznabCatType.Console.CopyWithoutSubCategories(),
                TorznabCatType.Movies.CopyWithoutSubCategories(),
                TorznabCatType.Books.CopyWithoutSubCategories(),
                new TorznabCategory(137107, "Console/Wii_c"),
                new TorznabCategory(100044, "Console/Xbox_c"),
                new TorznabCategory(100040, "Console/Xbox_c2")
            };
            expected[0].SubCategories.Add(TorznabCatType.ConsoleWii.CopyWithoutSubCategories());
            expected[0].SubCategories.Add(TorznabCatType.ConsoleXBox.CopyWithoutSubCategories());
            expected[1].SubCategories.Add(TorznabCatType.MoviesSD.CopyWithoutSubCategories());
            expected[2].SubCategories.Add(TorznabCatType.BooksComics.CopyWithoutSubCategories());
            TestCategories.CompareCategoryTrees(expected, cats);
        }

        [Test]
        public void TestGetTorznabCategoryList()
        {
            var tcc = CreateTestDataset();

            // unsorted list
            var cats = tcc.GetTorznabCategoryList();
            var expected = new List<TorznabCategory>
            {
                TorznabCatType.Movies.CopyWithoutSubCategories(),
                TorznabCatType.MoviesSD.CopyWithoutSubCategories(),
                TorznabCatType.Books.CopyWithoutSubCategories(),
                TorznabCatType.BooksComics.CopyWithoutSubCategories(),
                TorznabCatType.Console.CopyWithoutSubCategories(),
                TorznabCatType.ConsoleXBox.CopyWithoutSubCategories(),
                TorznabCatType.ConsoleWii.CopyWithoutSubCategories(),
                new TorznabCategory(100044, "Console/Xbox_c"),
                new TorznabCategory(137107, "Console/Wii_c"),
                new TorznabCategory(100040, "Console/Xbox_c2")
            };
            TestCategories.CompareCategoryTrees(expected, cats);

            // sorted list
            cats = tcc.GetTorznabCategoryList(true);
            expected = new List<TorznabCategory>
            {
                TorznabCatType.Console.CopyWithoutSubCategories(),
                TorznabCatType.ConsoleWii.CopyWithoutSubCategories(),
                TorznabCatType.ConsoleXBox.CopyWithoutSubCategories(),
                TorznabCatType.Movies.CopyWithoutSubCategories(),
                TorznabCatType.MoviesSD.CopyWithoutSubCategories(),
                TorznabCatType.Books.CopyWithoutSubCategories(),
                TorznabCatType.BooksComics.CopyWithoutSubCategories(),
                new TorznabCategory(137107, "Console/Wii_c"),
                new TorznabCategory(100044, "Console/Xbox_c"),
                new TorznabCategory(100040, "Console/Xbox_c2")
            };
            TestCategories.CompareCategoryTrees(expected, cats);
        }

        [Test]
        public void TestAddCategoryMapping()
        {
            var tcc = new TorznabCapabilitiesCategories();
            var cats = tcc.GetTorznabCategoryTree();

            // add "int" category (parent category)
            // + Movies
            tcc.AddCategoryMapping("1", TorznabCatType.Movies);
            var expected = new List<TorznabCategory>
            {
                TorznabCatType.Movies.CopyWithoutSubCategories()
            };
            TestCategories.CompareCategoryTrees(expected, cats);

            // add "string" category (child category)
            // - Movies
            //   + MoviesSD
            tcc.AddCategoryMapping("mov_sd", TorznabCatType.MoviesSD);
            expected[0].SubCategories.Add(TorznabCatType.MoviesSD.CopyWithoutSubCategories());
            TestCategories.CompareCategoryTrees(expected, cats);

            // add subcategory of books (child category)
            // - Movies
            //   - MoviesSD
            // + Books
            //   + BooksComics
            tcc.AddCategoryMapping("33", TorznabCatType.BooksComics);
            expected.Add(TorznabCatType.Books.CopyWithoutSubCategories());
            expected[1].SubCategories.Add(TorznabCatType.BooksComics.CopyWithoutSubCategories());
            TestCategories.CompareCategoryTrees(expected, cats);

            // add int category with description => custom category. it's converted into 2 different categories
            // - Movies
            //   - MoviesSD
            // - Books
            //   - BooksComics
            // + Console
            //   + ConsoleXBox
            // + Custom Cat "Console/Xbox_c"
            tcc.AddCategoryMapping("44", TorznabCatType.ConsoleXBox, "Console/Xbox_c");
            expected.Add(TorznabCatType.Console.CopyWithoutSubCategories());
            expected[2].SubCategories.Add(TorznabCatType.ConsoleXBox.CopyWithoutSubCategories());
            expected.Add(new TorznabCategory(100044, "Console/Xbox_c"));
            TestCategories.CompareCategoryTrees(expected, cats);

            // add string category with description => custom category. it's converted into 2 different categories
            // - Movies
            //   - MoviesSD
            // - Books
            //   - BooksComics
            // - Console
            //   - ConsoleXBox
            //   + ConsoleWii
            // - Custom Cat "Console/Xbox_c"
            // + Custom Cat "Console/Wii_c"
            tcc.AddCategoryMapping("con_wii", TorznabCatType.ConsoleWii, "Console/Wii_c");
            expected[2].SubCategories.Add(TorznabCatType.ConsoleWii.CopyWithoutSubCategories());
            expected.Add(new TorznabCategory(137107, "Console/Wii_c"));
            TestCategories.CompareCategoryTrees(expected, cats);

            // add another int category with description that maps to ConsoleXbox (there are 2 tracker cats => 1 torznab cat)
            // - Movies
            //   - MoviesSD
            // - Books
            //   - BooksComics
            // - Console
            //   - ConsoleXBox (this is not added again)
            //   - ConsoleWii
            // - Custom Cat "Console/Xbox_c"
            // - Custom Cat "Console/Wii_c"
            // + Custom Cat "Console/Xbox_c2"
            tcc.AddCategoryMapping("45", TorznabCatType.ConsoleXBox, "Console/Xbox_c2");
            expected.Add(new TorznabCategory(100045, "Console/Xbox_c2"));
            TestCategories.CompareCategoryTrees(expected, cats);
        }

        [Test]
        public void TestMapTorznabCapsToTrackers()
        {
            // MapTorznabCapsToTrackers: maps TorznabQuery cats => Tracker cats
            var tcc = CreateTestDataset();

            var query = new TorznabQuery(); // no cats
            var trackerCats = tcc.MapTorznabCapsToTrackers(query);
            Assert.AreEqual(0, trackerCats.Count);

            query = new TorznabQuery // int category with subcategories (parent cat)
            {
                Categories = new[] { TorznabCatType.Movies.ID }
            };
            trackerCats = tcc.MapTorznabCapsToTrackers(query);
            Assert.AreEqual(2, trackerCats.Count);
            Assert.AreEqual("1", trackerCats[0]); // Movies
            Assert.AreEqual("mov_sd", trackerCats[1]); // Movies SD

            query = new TorznabQuery // string child category
            {
                Categories = new[] { TorznabCatType.MoviesSD.ID }
            };
            trackerCats = tcc.MapTorznabCapsToTrackers(query);
            Assert.AreEqual(1, trackerCats.Count);
            Assert.AreEqual("mov_sd", trackerCats[0]); // Movies SD
            trackerCats = tcc.MapTorznabCapsToTrackers(query, true); // get parent
            Assert.AreEqual(2, trackerCats.Count);
            Assert.AreEqual("1", trackerCats[0]); // Movies
            Assert.AreEqual("mov_sd", trackerCats[1]); // Movies SD

            query = new TorznabQuery // duplicate category (1 torznab cat => 2 indexer cats)
            {
                Categories = new[] { TorznabCatType.ConsoleXBox.ID }
            };
            trackerCats = tcc.MapTorznabCapsToTrackers(query);
            Assert.AreEqual(2, trackerCats.Count);
            Assert.AreEqual("44", trackerCats[0]);
            Assert.AreEqual("40", trackerCats[1]);

            query = new TorznabQuery // custom cat "integer"
            {
                Categories = new[] { 100044 } // Console/Xbox_c
            };
            trackerCats = tcc.MapTorznabCapsToTrackers(query);
            Assert.AreEqual(1, trackerCats.Count);
            Assert.AreEqual("44", trackerCats[0]); // Console/Xbox_c

            query = new TorznabQuery // custom cat "string"
            {
                Categories = new[] { 137107 } // Console/Wii_c
            };
            trackerCats = tcc.MapTorznabCapsToTrackers(query);
            Assert.AreEqual(1, trackerCats.Count);
            Assert.AreEqual("con_wii", trackerCats[0]); // Console/Wii_c

            query = new TorznabQuery // unknown category
            {
                Categories = new[] { 9999 }
            };
            trackerCats = tcc.MapTorznabCapsToTrackers(query);
            Assert.AreEqual(0, trackerCats.Count);

            query = new TorznabQuery // unknown custom cat
            {
                Categories = new[] { 100001 }
            };
            trackerCats = tcc.MapTorznabCapsToTrackers(query);
            Assert.AreEqual(0, trackerCats.Count);
        }

        [Test]
        public void TestMapTrackerCatToNewznab()
        {
            // MapTrackerCatToNewznab: maps Tracker cat ID => Torznab cats
            var tcc = CreateTestDataset();

            var torznabCats = tcc.MapTrackerCatToNewznab("1").ToList(); // "integer" id without custom cat
            Assert.AreEqual(1, torznabCats.Count);
            Assert.AreEqual(2000, torznabCats[0]);

            torznabCats = tcc.MapTrackerCatToNewznab("mov_sd").ToList(); // "string" id without custom cat
            Assert.AreEqual(1, torznabCats.Count);
            Assert.AreEqual(2030, torznabCats[0]);

            // "integer" id with custom cats. 44 and 40 maps to ConsoleXbox but different custom cat
            torznabCats = tcc.MapTrackerCatToNewznab("44").ToList();
            Assert.AreEqual(2, torznabCats.Count);
            Assert.AreEqual(1040, torznabCats[0]);
            Assert.AreEqual(100044, torznabCats[1]);
            torznabCats = tcc.MapTrackerCatToNewznab("40").ToList();
            Assert.AreEqual(2, torznabCats.Count);
            Assert.AreEqual(1040, torznabCats[0]);
            Assert.AreEqual(100040, torznabCats[1]);

            // "string" id with custom cats
            torznabCats = tcc.MapTrackerCatToNewznab("con_wii").ToList();
            Assert.AreEqual(2, torznabCats.Count);
            Assert.AreEqual(1030, torznabCats[0]);
            Assert.AreEqual(137107, torznabCats[1]);

            torznabCats = tcc.MapTrackerCatToNewznab("9999").ToList(); // unknown cat
            Assert.AreEqual(0, torznabCats.Count);

            torznabCats = tcc.MapTrackerCatToNewznab("con_unknown").ToList(); // unknown custom cat
            Assert.AreEqual(0, torznabCats.Count);

            torznabCats = tcc.MapTrackerCatToNewznab(null).ToList(); // null
            Assert.AreEqual(0, torznabCats.Count);
        }

        [Test]
        public void TestMapTrackerCatDescToNewznab()
        {
            // MapTrackerCatDescToNewznab: maps Tracker cat Description => Torznab cats
            var tcc = CreateTestDataset();

            // "integer" id with custom cats
            // Console/Xbox_c and Console/Xbox_c2 maps to ConsoleXbox but different custom cat
            var torznabCats = tcc.MapTrackerCatDescToNewznab("Console/Xbox_c").ToList();
            Assert.AreEqual(2, torznabCats.Count);
            Assert.AreEqual(1040, torznabCats[0]);
            Assert.AreEqual(100044, torznabCats[1]);
            torznabCats = tcc.MapTrackerCatDescToNewznab("Console/Xbox_c2").ToList();
            Assert.AreEqual(2, torznabCats.Count);
            Assert.AreEqual(1040, torznabCats[0]);
            Assert.AreEqual(100040, torznabCats[1]);

            // "string" id with custom cats
            torznabCats = tcc.MapTrackerCatDescToNewznab("Console/Wii_c").ToList();
            Assert.AreEqual(2, torznabCats.Count);
            Assert.AreEqual(1030, torznabCats[0]);
            Assert.AreEqual(137107, torznabCats[1]);

            torznabCats = tcc.MapTrackerCatDescToNewznab("Unknown/Cat").ToList(); // unknown cat
            Assert.AreEqual(0, torznabCats.Count);

            torznabCats = tcc.MapTrackerCatDescToNewznab(null).ToList(); // null
            Assert.AreEqual(0, torznabCats.Count);
        }

        [Test]
        public void TestSupportedCategories()
        {
            var tcc = CreateTestDataset();

            Assert.AreEqual(new[] { TorznabCatType.Movies.ID }, // parent cat
                tcc.SupportedCategories(new[] { TorznabCatType.Movies.ID }));
            Assert.AreEqual(new[] { TorznabCatType.MoviesSD.ID }, // child cat
                tcc.SupportedCategories(new[] { TorznabCatType.MoviesSD.ID }));
            Assert.AreEqual(new[] { TorznabCatType.Movies.ID, TorznabCatType.MoviesSD.ID }, //  parent & child cat
                tcc.SupportedCategories(new[] { TorznabCatType.Movies.ID, TorznabCatType.MoviesSD.ID }));
            Assert.AreEqual(new[] { 100040 }, // custom cat
                tcc.SupportedCategories(new[] { 100040 }));
            Assert.AreEqual(new[] { TorznabCatType.Movies.ID }, // mixed good and bad
                tcc.SupportedCategories(new[] { TorznabCatType.Movies.ID, 9999 }));
            Assert.AreEqual(new int[] { }, // not supported child cat
                tcc.SupportedCategories(new[] { TorznabCatType.Movies3D.ID }));
            Assert.AreEqual(new int[] { }, // unknown cat
                tcc.SupportedCategories(new[] { 9999 }));
            Assert.AreEqual(new int[] { }, // unknown custom cat
                tcc.SupportedCategories(new[] { 100001 }));
            Assert.AreEqual(new int[] { }, // empty list
                tcc.SupportedCategories(new int[] { }));
            Assert.AreEqual(new int[] { }, // null
                tcc.SupportedCategories(null));
        }

        [Test]
        public void TestConcat()
        {
            var lhs = new TorznabCapabilitiesCategories();
            var rhs = CreateTestDataset();
            lhs.Concat(rhs);
            var expected = new List<TorznabCategory>
            {
                TorznabCatType.Movies.CopyWithoutSubCategories(),
                TorznabCatType.Books.CopyWithoutSubCategories(),
                TorznabCatType.Console.CopyWithoutSubCategories()
            };
            expected[0].SubCategories.Add(TorznabCatType.MoviesSD.CopyWithoutSubCategories());
            expected[1].SubCategories.Add(TorznabCatType.BooksComics.CopyWithoutSubCategories());
            expected[2].SubCategories.Add(TorznabCatType.ConsoleXBox.CopyWithoutSubCategories());
            expected[2].SubCategories.Add(TorznabCatType.ConsoleWii.CopyWithoutSubCategories());
            TestCategories.CompareCategoryTrees(expected, lhs.GetTorznabCategoryTree()); // removed custom cats
            Assert.AreEqual(0, lhs.GetTrackerCategories().Count); // removed tracker mapping

            lhs = CreateTestDataset();
            rhs = CreateTestDataset();
            lhs.Concat(rhs);
            expected = new List<TorznabCategory>
            {
                TorznabCatType.Movies.CopyWithoutSubCategories(),
                TorznabCatType.Books.CopyWithoutSubCategories(),
                TorznabCatType.Console.CopyWithoutSubCategories(),
                new TorznabCategory(100044, "Console/Xbox_c"),
                new TorznabCategory(137107, "Console/Wii_c"),
                new TorznabCategory(100040, "Console/Xbox_c2")
            };
            expected[0].SubCategories.Add(TorznabCatType.MoviesSD.CopyWithoutSubCategories());
            expected[1].SubCategories.Add(TorznabCatType.BooksComics.CopyWithoutSubCategories());
            expected[2].SubCategories.Add(TorznabCatType.ConsoleXBox.CopyWithoutSubCategories());
            expected[2].SubCategories.Add(TorznabCatType.ConsoleWii.CopyWithoutSubCategories());
            TestCategories.CompareCategoryTrees(expected, lhs.GetTorznabCategoryTree()); // check there are not duplicates
        }

        private static TorznabCapabilitiesCategories CreateTestDataset()
        {
            var tcc = new TorznabCapabilitiesCategories();
            TestCategories.AddTestCategories(tcc);
            return tcc;
        }
    }
}
