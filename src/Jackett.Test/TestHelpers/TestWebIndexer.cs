using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Jackett.Common.Indexers;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Test.TestHelpers
{
    public class TestWebIndexer : BaseWebIndexer
    {
        public override string Id => "test_id";
        public override string Name => "test_name";
        public override string Description => "test_description";
        public override string SiteLink => "https://test.link/";
        public override string[] AlternativeSiteLinks => new[]
        {
            "https://test.link/",
            "https://alternative-test.link/"
        };
        public override string[] LegacySiteLinks => new[]
        {
            "https://legacy-test.link/"
        };
        public override Encoding Encoding { get; protected set; } = Encoding.UTF8;
        public override string Language { get; protected set; } = "en-us";
        public override string Type { get; protected set; } = "private";

        public override TorznabCapabilities TorznabCaps { get; protected set; } = new TorznabCapabilities();

        public TestWebIndexer(Logger logger)
            : base(client: null, configService: null, logger: logger, configData: new ConfigurationData(), p: null, cacheService: null)
        {
        }

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
