using System.Collections.Generic;
using System.Linq;

namespace Jackett.Common.Helpers
{
    public static class SearchPathHelpers
    {
        public static List<string> GetApplicableCategories(List<string> categories, List<string> mappedCategories)
        {
            var invertMatch = categories[0] == "!";
            var intersectedCategories = categories.Intersect(mappedCategories);

            return invertMatch ? mappedCategories.Except(intersectedCategories).ToList() : intersectedCategories.ToList();
        }
    }
}
