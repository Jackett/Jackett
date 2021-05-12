using System;
using System.Collections.Generic;
using Jackett.Common.Indexers;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;

namespace Jackett.Performance.Services
{
    public class PerformanceCacheService : ICacheService
    {
        public List<ReleaseInfo> Search(IIndexer indexer, TorznabQuery query) => null;

        public void CacheResults(IIndexer indexer, TorznabQuery query, List<ReleaseInfo> releases) {}

        public List<TrackerCacheResult> GetCachedResults() => throw new NotImplementedException();

        public void CleanIndexerCache(IIndexer indexer) => throw new NotImplementedException();

        public void CleanCache() => throw new NotImplementedException();
    }
}
