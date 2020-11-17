using System.Collections.Generic;
using Jackett.Common.Models;
using NUnit.Framework;

namespace Jackett.Test.TestHelpers
{
    public static class TestCategories
    {
        public static void AddTestCategories(TorznabCapabilitiesCategories tcc)
        {
            // these categories are chosen to test all kind of category types:
            // - with integer and string id
            // - with and without description (with description we generate custom cats)
            // - parent and child categories
            // - custom categories are not added automatically but they are created from other categories automatically
            // - categories and subcategories are unsorted to test the sort when required
            tcc.AddCategoryMapping("1", TorznabCatType.Movies);
            tcc.AddCategoryMapping("mov_sd", TorznabCatType.MoviesSD);
            tcc.AddCategoryMapping("33", TorznabCatType.BooksComics);
            tcc.AddCategoryMapping("44", TorznabCatType.ConsoleXBox, "Console/Xbox_c");
            tcc.AddCategoryMapping("con_wii", TorznabCatType.ConsoleWii, "Console/Wii_c");
            tcc.AddCategoryMapping("40", TorznabCatType.ConsoleXBox, "Console/Xbox_c2");
        }

        public static void CompareCategoryTrees(List<TorznabCategory> tree1, List<TorznabCategory> tree2)
        {
            Assert.AreEqual(tree1.Count, tree2.Count);
            for (var i = 0; i < tree1.Count; i++)
            {
                Assert.AreEqual(tree1[i].ID, tree2[i].ID);
                Assert.AreEqual(tree1[i].Name, tree2[i].Name);
                CompareCategoryTrees(tree1[i].SubCategories, tree2[i].SubCategories);
            }
        }
    }
}
