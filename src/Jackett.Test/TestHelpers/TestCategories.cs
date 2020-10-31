using Jackett.Common.Models;

namespace Jackett.Test.TestHelpers
{
    public static class TestCategories
    {
        public static void AddTestCategories(TorznabCapabilitiesCategories tcc)
        {
            // these categories are chosen to test all kind of category types:
            // - with integer and string id
            // - with and without description
            // - parent and child categories
            // - custom categories are not added automatically but they are created from other categories automatically
            tcc.AddCategoryMapping("1", TorznabCatType.Movies);
            tcc.AddCategoryMapping("mov_sd", TorznabCatType.MoviesSD);
            tcc.AddCategoryMapping("33", TorznabCatType.BooksComics);
            tcc.AddCategoryMapping("44", TorznabCatType.ConsoleXBox, "Console/Xbox_c");
            tcc.AddCategoryMapping("con_wii", TorznabCatType.ConsoleWii, "Console/Wii_c");
            tcc.AddCategoryMapping("45", TorznabCatType.ConsoleXBox, "Console/Xbox_c2");
        }
    }
}
