using System;
using System.Collections.Generic;
using Jackett.Common.Indexers;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;

namespace Jackett.Test.TestHelpers
{
    public class TestCacheService : ICacheService
    {
        public void CacheResults(IIndexer indexer, TorznabQuery query, List<ReleaseInfo> releases)
        {
        }

        public List<ReleaseInfo> Search(IIndexer indexer, TorznabQuery query) => null;

        public List<TrackerCacheResult> GetCachedResults() => new List<TrackerCacheResult>();

        public void CleanIndexerCache(IIndexer indexer)
        {
        }

        public void CleanCache()
        {
        }

        public TimeSpan CacheTTL => TimeSpan.FromSeconds(0);
    }
}
