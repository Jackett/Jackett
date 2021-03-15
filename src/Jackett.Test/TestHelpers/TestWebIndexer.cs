using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Jackett.Common.Indexers;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Newtonsoft.Json.Linq;

namespace Jackett.Test.TestHelpers
{
    public class TestWebIndexer: BaseWebIndexer
    {
        public TestWebIndexer():
            base(id: "test_id",
                 name: "test_name",
                 description: "test_description",
                 link: "https://test.link/",
                 caps: new TorznabCapabilities(),
                 client: null,
                 configService: null,
                 logger: null,
                 configData: new ConfigurationData(),
                 p: null,
                 cacheService: null)
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";  
        }

        public override string[] AlternativeSiteLinks { get; protected set; } = {
            "https://test.link/",
            "https://alternative-test.link/"
        };

        public override string[] LegacySiteLinks { get; protected set; } = {
            "https://legacy-test.link/"
        };

        public override Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson) =>
            throw new NotImplementedException();
        protected override Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query) =>
            throw new NotImplementedException();

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // public methods to test private methods
        public void SetType(string type) => Type = type;

        public IEnumerable<ReleaseInfo> _FilterResults(TorznabQuery query, IEnumerable<ReleaseInfo> results) =>
            FilterResults(query, results);

        public IEnumerable<ReleaseInfo> _FixResults(TorznabQuery query, IEnumerable<ReleaseInfo> results) =>
            FixResults(query, results);

        public void _AddCategoryMapping(string trackerCategory, TorznabCategory newznabCategory, string trackerCategoryDesc = null) =>
            AddCategoryMapping(trackerCategory, newznabCategory, trackerCategoryDesc);

        public void _AddCategoryMapping(int trackerCategory, TorznabCategory newznabCategory, string trackerCategoryDesc = null) =>
            AddCategoryMapping(trackerCategory, newznabCategory, trackerCategoryDesc);

        public void _AddMultiCategoryMapping(TorznabCategory newznabCategory, params int[] trackerCategories) =>
            AddMultiCategoryMapping(newznabCategory, trackerCategories);

        public List<string> _MapTorznabCapsToTrackers(TorznabQuery query, bool mapChildrenCatsToParent = false) =>
            MapTorznabCapsToTrackers(query, mapChildrenCatsToParent);

        public ICollection<int> _MapTrackerCatToNewznab(string input) =>
            MapTrackerCatToNewznab(input);

        public ICollection<int> _MapTrackerCatDescToNewznab(string input) =>
            MapTrackerCatDescToNewznab(input);

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // public methods to load sample datasets
        public void AddTestCategories()
        {
            TestCategories.AddTestCategories(TorznabCaps.Categories);
        }
    }
}
