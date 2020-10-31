using System;
using System.Collections.Generic;
using System.Linq;

namespace Jackett.Common.Models
{
    public class TorznabCapabilitiesCategories
    {
        private readonly List<CategoryMapping> _categoryMapping = new List<CategoryMapping>();
        private readonly List<TorznabCategory> _torznabCategoryTree = new List<TorznabCategory>();

        public List<string> GetTrackerCategories() => _categoryMapping.Select(x => x.TrackerCategory).ToList();

        public List<TorznabCategory> GetTorznabCategoryTree(bool sorted = false)
        {
            if (!sorted)
                return _torznabCategoryTree;

            // we build a new tree, original is unsorted
            // first torznab categories ordered by id and then custom cats ordered by name
            var sortedTree = _torznabCategoryTree
                .Select(c =>
                {
                    var sortedSubCats = c.SubCategories.OrderBy(x => x.ID);
                    var newCat = new TorznabCategory(c.ID, c.Name);
                    newCat.SubCategories.AddRange(sortedSubCats);
                    return newCat;
                }).OrderBy(x => x.ID > 100000 ? "zzz" + x.Name : x.ID.ToString()).ToList();

            return sortedTree;
        }

        public List<TorznabCategory> GetTorznabCategoryList(bool sorted = false)
        {
            var tree = GetTorznabCategoryTree(sorted);

            // create a flat list (without subcategories)
            var newFlatList = new List<TorznabCategory>();
            foreach (var cat in tree)
            {
                newFlatList.Add(cat.CopyWithoutSubCategories());
                newFlatList.AddRange(cat.SubCategories);
            }
            return newFlatList;
        }

        public void AddCategoryMapping(string trackerCategory, TorznabCategory torznabCategory, string trackerCategoryDesc = null)
        {
            // add torznab cat
            _categoryMapping.Add(new CategoryMapping(trackerCategory, trackerCategoryDesc, torznabCategory.ID));
            AddTorznabCategoryTree(torznabCategory);

            // TODO: fix this. it's only working for integer "trackerCategory"
            // create custom cats (1:1 categories)
            if (trackerCategoryDesc != null && trackerCategory != null)
            {
                //TODO convert to int.TryParse() to avoid using throw as flow control
                try
                {
                    var trackerCategoryInt = int.Parse(trackerCategory);
                    var customCat = new TorznabCategory(trackerCategoryInt + 100000, trackerCategoryDesc);
                    AddTorznabCategoryTree(customCat);
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
            var subCategories = _torznabCategoryTree.SelectMany(c => c.SubCategories);
            var allCategories = _torznabCategoryTree.Concat(subCategories);
            var supportsCategory = allCategories.Any(i => categories.Any(c => c == i.ID));
            return supportsCategory;
        }

        public void Concat(TorznabCapabilitiesCategories rhs)
        {
            // exclude indexer specific categories (>= 100000)
            // we don't concat _categoryMapping because it makes no sense for the aggregate indexer
            rhs.GetTorznabCategoryList().Where(x => x.ID < 100000).ToList().ForEach(AddTorznabCategoryTree);
        }

        private void AddTorznabCategoryTree(TorznabCategory torznabCategory)
        {
            // build the category tree
            if (TorznabCatType.ParentCats.Contains(torznabCategory))
            {
                // parent cat
                if (!_torznabCategoryTree.Contains(torznabCategory))
                    _torznabCategoryTree.Add(torznabCategory.CopyWithoutSubCategories());
            }
            else
            {
                // child or custom cat
                var parentCat = TorznabCatType.ParentCats.FirstOrDefault(c => c.Contains(torznabCategory));
                if (parentCat != null)
                {
                    // child cat
                    var nodeCat = _torznabCategoryTree.FirstOrDefault(c => c.Equals(parentCat));
                    if (nodeCat != null)
                    {
                        // parent cat already exists
                        if (!nodeCat.Contains(torznabCategory))
                            nodeCat.SubCategories.Add(torznabCategory);
                    }
                    else
                    {
                        // create parent cat and add child
                        nodeCat = parentCat.CopyWithoutSubCategories();
                        nodeCat.SubCategories.Add(torznabCategory);
                        _torznabCategoryTree.Add(nodeCat);
                    }
                }
                else
                    // custom cat
                    _torznabCategoryTree.Add(torznabCategory);
            }
        }
    }
}
