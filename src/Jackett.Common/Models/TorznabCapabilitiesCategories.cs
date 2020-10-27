using System;
using System.Collections.Generic;
using System.Linq;

namespace Jackett.Common.Models
{
    public class TorznabCapabilitiesCategories
    {
        private readonly List<TorznabCategory> _categories = new List<TorznabCategory>();
        private readonly List<CategoryMapping> _categoryMapping = new List<CategoryMapping>();

        public List<TorznabCategory> GetTorznabCategories() => _categories;

        public List<string> GetTrackerCategories() => _categoryMapping.Select(x => x.TrackerCategory).ToList();

        public void AddCategoryMapping(string trackerCategory, TorznabCategory torznabCategory, string trackerCategoryDesc = null)
        {
            _categoryMapping.Add(new CategoryMapping(trackerCategory, trackerCategoryDesc, torznabCategory.ID));

            if (!_categories.Contains(torznabCategory))
                _categories.Add(torznabCategory);

            // add 1:1 categories
            if (trackerCategoryDesc != null && trackerCategory != null)
            {
                //TODO convert to int.TryParse() to avoid using throw as flow control
                try
                {
                    var trackerCategoryInt = int.Parse(trackerCategory);
                    var customCat = new TorznabCategory(trackerCategoryInt + 100000, trackerCategoryDesc);
                    if (!_categories.Contains(customCat))
                        _categories.Add(customCat);
                }
                catch (FormatException)
                {
                    // trackerCategory is not an integer, continue
                }
            }
        }

        public List<string> MapTorznabCapsToTrackers(TorznabQuery query, bool mapChildrenCatsToParent = false)
        {
            var result = new List<string>();
            foreach (var cat in query.Categories)
            {
                // use 1:1 mapping to tracker categories for newznab categories >= 100000
                if (cat >= 100000)
                {
                    result.Add((cat - 100000).ToString());
                    continue;
                }

                var queryCats = new List<int> { cat };
                var newznabCat = TorznabCatType.AllCats.FirstOrDefault(c => c.ID == cat);
                if (newznabCat != null)
                {
                    queryCats.AddRange(newznabCat.SubCategories.Select(c => c.ID));
                }

                if (mapChildrenCatsToParent)
                {
                    var parentNewznabCat = TorznabCatType.AllCats.FirstOrDefault(c => c.SubCategories.Contains(newznabCat));
                    if (parentNewznabCat != null)
                    {
                        queryCats.Add(parentNewznabCat.ID);
                    }
                }

                foreach (var mapping in _categoryMapping.Where(c => queryCats.Contains(c.NewzNabCategory)))
                {
                    result.Add(mapping.TrackerCategory);
                }
            }

            return result.Distinct().ToList();
        }

        public ICollection<int> MapTrackerCatToNewznab(string input)
        {
            if (input == null)
                return new List<int>();

            var cats = _categoryMapping
                       .Where(m => m.TrackerCategory != null && m.TrackerCategory.ToLowerInvariant() == input.ToLowerInvariant())
                       .Select(c => c.NewzNabCategory).ToList();

            // 1:1 category mapping
            try
            {
                var trackerCategoryInt = int.Parse(input);
                cats.Add(trackerCategoryInt + 100000);
            }
            catch (FormatException)
            {
                // input is not an integer, continue
            }

            return cats;
        }

        public ICollection<int> MapTrackerCatDescToNewznab(string input)
        {
            var cats = new List<int>();
            if (null != input)
            {
                var mapping = _categoryMapping
                    .FirstOrDefault(m => m.TrackerCategoryDesc != null && m.TrackerCategoryDesc.ToLowerInvariant() == input.ToLowerInvariant());
                if (mapping != null)
                {
                    cats.Add(mapping.NewzNabCategory);

                    if (mapping.TrackerCategory != null)
                    {
                        // 1:1 category mapping
                        try
                        {
                            var trackerCategoryInt = int.Parse(mapping.TrackerCategory);
                            cats.Add(trackerCategoryInt + 100000);
                        }
                        catch (FormatException)
                        {
                            // mapping.TrackerCategory is not an integer, continue
                        }
                    }
                }
            }
            return cats;
        }

        public bool SupportsCategories(int[] categories)
        {
            if (categories == null)
                return false;
            var subCategories = _categories.SelectMany(c => c.SubCategories);
            var allCategories = _categories.Concat(subCategories);
            var supportsCategory = allCategories.Any(i => categories.Any(c => c == i.ID));
            return supportsCategory;
        }

        public void Concat(TorznabCapabilitiesCategories rhs) =>
            // exclude indexer specific categories (>= 100000)
            // we don't concat _categoryMapping because it makes no sense for the aggregate indexer
            _categories.AddRange(rhs._categories.Where(x => x.ID < 100000).Except(_categories));
    }
}
