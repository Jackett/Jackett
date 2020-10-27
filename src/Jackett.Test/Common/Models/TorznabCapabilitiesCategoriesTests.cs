using System.Linq;
using Jackett.Common.Models;
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
            Assert.IsEmpty(tcc.GetTorznabCategories());
            Assert.IsEmpty(tcc.GetTrackerCategories());
        }

        [Test]
        public void TestGetTorznabCategories()
        {
            var tcc = CreateTestDataset();
            var cats = tcc.GetTorznabCategories();
            Assert.AreEqual(7, cats.Count);
            Assert.AreEqual(2000, cats[0].ID);
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
        public void TestAddCategoryMapping()
        {
            var tcc = new TorznabCapabilitiesCategories();
            var cats = tcc.GetTorznabCategories();

            // add "int" category (parent category)
            tcc.AddCategoryMapping("1", TorznabCatType.Movies);
            Assert.AreEqual(1, cats.Count);
            Assert.AreEqual(2000, cats[0].ID);

            // add "string" category (child category)
            tcc.AddCategoryMapping("mov_sd", TorznabCatType.MoviesSD);
            Assert.AreEqual(2, cats.Count);
            Assert.AreEqual(2030, cats[1].ID);

            // add subcategory of books (child category)
            tcc.AddCategoryMapping("33", TorznabCatType.BooksComics);
            Assert.AreEqual(3, cats.Count);
            Assert.AreEqual(7030, cats[2].ID);

            // add int category with description => custom category. it's converted into 2 different categories
            tcc.AddCategoryMapping("44", TorznabCatType.ConsoleXBox, "Console/Xbox_c");
            Assert.AreEqual(5, cats.Count);
            Assert.AreEqual(1040, cats[3].ID);
            Assert.AreEqual(100044, cats[4].ID);

            // TODO: we should add a way to add custom categories for string categories
            // https://github.com/Sonarr/Sonarr/wiki/Implementing-a-Torznab-indexer#caps-endpoint
            // add string category with description. it's converted into 1 category
            tcc.AddCategoryMapping("con_wii", TorznabCatType.ConsoleWii, "Console/Wii_c");
            Assert.AreEqual(6, cats.Count);
            Assert.AreEqual(1030, cats[5].ID);

            // add another int category with description that maps to ConsoleXbox (there are 2 tracker cats => 1 torznab cat)
            tcc.AddCategoryMapping("45", TorznabCatType.ConsoleXBox, "Console/Xbox_c2");
            Assert.AreEqual(7, cats.Count);
            Assert.AreEqual(100045, cats[6].ID); // 1040 is duplicated and it is not added
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
                Categories = new [] { TorznabCatType.Movies.ID }
            };
            trackerCats = tcc.MapTorznabCapsToTrackers(query);
            Assert.AreEqual(2, trackerCats.Count);
            Assert.AreEqual("1", trackerCats[0]); // Movies
            Assert.AreEqual("mov_sd", trackerCats[1]); // Movies SD

            query = new TorznabQuery // string child category
            {
                Categories = new [] { TorznabCatType.MoviesSD.ID }
            };
            trackerCats = tcc.MapTorznabCapsToTrackers(query);
            Assert.AreEqual(1, trackerCats.Count);
            Assert.AreEqual("mov_sd", trackerCats[0]); // Movies SD
            trackerCats = tcc.MapTorznabCapsToTrackers(query, true); // get parent
            Assert.AreEqual(2, trackerCats.Count);
            Assert.AreEqual("1", trackerCats[0]); // Movies
            Assert.AreEqual("mov_sd", trackerCats[1]); // Movies SD

            query = new TorznabQuery // duplicate category (1 toznab cat => 2 indexer cats)
            {
                Categories = new [] { TorznabCatType.ConsoleXBox.ID }
            };
            trackerCats = tcc.MapTorznabCapsToTrackers(query);
            Assert.AreEqual(2, trackerCats.Count);
            Assert.AreEqual("44", trackerCats[0]);
            Assert.AreEqual("45", trackerCats[1]);

            query = new TorznabQuery // custom cat
            {
                Categories = new [] { 100001 } // Movies
            };
            trackerCats = tcc.MapTorznabCapsToTrackers(query);
            Assert.AreEqual(1, trackerCats.Count);
            Assert.AreEqual("1", trackerCats[0]); // Movies

            query = new TorznabQuery // unknown category
            {
                Categories = new [] { 9999 }
            };
            trackerCats = tcc.MapTorznabCapsToTrackers(query);
            Assert.AreEqual(0, trackerCats.Count);
        }

        [Test]
        public void TestMapTrackerCatToNewznab()
        {
            // MapTrackerCatToNewznab: maps Tracker cat ID => Torznab cats
            var tcc = CreateTestDataset();

            // TODO: this is wrong, custom cat 100001 doesn't exists (it's not defined by us)
            var torznabCats = tcc.MapTrackerCatToNewznab("1").ToList();
            Assert.AreEqual(2, torznabCats.Count);
            Assert.AreEqual(2000, torznabCats[0]);
            Assert.AreEqual(100001, torznabCats[1]);

            torznabCats = tcc.MapTrackerCatToNewznab("mov_sd").ToList();
            Assert.AreEqual(1, torznabCats.Count);
            Assert.AreEqual(2030, torznabCats[0]);

            torznabCats = tcc.MapTrackerCatToNewznab("44").ToList(); // 44 and 45 maps to ConsoleXbox but different custom cat
            Assert.AreEqual(2, torznabCats.Count);
            Assert.AreEqual(1040, torznabCats[0]);
            Assert.AreEqual(100044, torznabCats[1]);
            torznabCats = tcc.MapTrackerCatToNewznab("45").ToList();
            Assert.AreEqual(2, torznabCats.Count);
            Assert.AreEqual(1040, torznabCats[0]);
            Assert.AreEqual(100045, torznabCats[1]);

            // TODO: this is wrong, we are returning cat 109999 which doesn't exist
            //torznabCats = tcc.MapTrackerCatToNewznab("9999").ToList(); // unknown cat
            //Assert.AreEqual(0, torznabCats.Count);

            torznabCats = tcc.MapTrackerCatToNewznab(null).ToList(); // null
            Assert.AreEqual(0, torznabCats.Count);
        }

        [Test]
        public void TestMapTrackerCatDescToNewznab()
        {
            // MapTrackerCatDescToNewznab: maps Tracker cat Description => Torznab cats
            var tcc = CreateTestDataset();

            var torznabCats = tcc.MapTrackerCatDescToNewznab("Console/Xbox_c").ToList(); // Console/Xbox_c and Console/Xbox_c2 maps to ConsoleXbox but different custom cat
            Assert.AreEqual(2, torznabCats.Count);
            Assert.AreEqual(1040, torznabCats[0]);
            Assert.AreEqual(100044, torznabCats[1]);

            torznabCats = tcc.MapTrackerCatDescToNewznab("Console/Xbox_c2").ToList();
            Assert.AreEqual(2, torznabCats.Count);
            Assert.AreEqual(1040, torznabCats[0]);
            Assert.AreEqual(100045, torznabCats[1]);

            torznabCats = tcc.MapTrackerCatDescToNewznab("Console/Wii_c").ToList();
            Assert.AreEqual(1, torznabCats.Count);
            Assert.AreEqual(1030, torznabCats[0]);

            torznabCats = tcc.MapTrackerCatDescToNewznab("9999").ToList(); // unknown cat
            Assert.AreEqual(0, torznabCats.Count);

            torznabCats = tcc.MapTrackerCatDescToNewznab(null).ToList(); // null
            Assert.AreEqual(0, torznabCats.Count);
        }

        [Test]
        public void TestSupportsCategories()
        {
            var tcc = CreateTestDataset();

            Assert.True(tcc.SupportsCategories(new []{ TorznabCatType.Movies.ID })); // parent cat
            Assert.True(tcc.SupportsCategories(new []{ TorznabCatType.MoviesSD.ID })); // child cat
            Assert.True(tcc.SupportsCategories(new []{ TorznabCatType.Movies.ID, TorznabCatType.MoviesSD.ID })); // parent & child
            Assert.True(tcc.SupportsCategories(new []{ 100044 })); // custom cat
            // TODO: fix this
            //Assert.False(tcc.SupportsCategories(new []{ TorznabCatType.Movies3D.ID })); // not supported child cat
            Assert.False(tcc.SupportsCategories(new []{ 9999 })); // unknown cat
            Assert.False(tcc.SupportsCategories(new int[]{})); // empty list
            Assert.False(tcc.SupportsCategories(null)); // null
        }

        [Test]
        public void TestConcat()
        {
            var lhs = new TorznabCapabilitiesCategories();
            var rhs = CreateTestDataset();

            lhs.Concat(rhs);
            Assert.AreEqual(5, lhs.GetTorznabCategories().Count); // removed custom cats
            Assert.AreEqual(0, lhs.GetTrackerCategories().Count); // removed tracker mapping
        }

        private static TorznabCapabilitiesCategories CreateTestDataset()
        {
            var tcc = new TorznabCapabilitiesCategories();
            tcc.AddCategoryMapping("1", TorznabCatType.Movies);
            tcc.AddCategoryMapping("mov_sd", TorznabCatType.MoviesSD);
            tcc.AddCategoryMapping("33", TorznabCatType.BooksComics);
            tcc.AddCategoryMapping("44", TorznabCatType.ConsoleXBox, "Console/Xbox_c");
            tcc.AddCategoryMapping("con_wii", TorznabCatType.ConsoleWii, "Console/Wii_c");
            tcc.AddCategoryMapping("45", TorznabCatType.ConsoleXBox, "Console/Xbox_c2");
            return tcc;
        }
    }
}
