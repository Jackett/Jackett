using System.Linq;

namespace Jackett.Models
{
    public static partial class TorznabCatType
    {

        public static bool QueryContainsParentCategory(int[] queryCats, int releaseCat)
        {
            var cat = AllCats.FirstOrDefault(c => c.ID == releaseCat);
            if (cat != null && queryCats != null)
            {
                return cat.SubCategories.Any(c => queryCats.Contains(c.ID));
            }

            return false;
        }

        public static string GetCatDesc(int newznabcat)
        {
            var cat = AllCats.FirstOrDefault(c => c.ID == newznabcat);
            if (cat != null)
            {
                return cat.Name;
            }

            return string.Empty;
        }

    }
}
