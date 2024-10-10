using System;
using System.Collections.Generic;
using Jackett.Common.Indexers;
using Jackett.Common.Models;

namespace Jackett.Common.Services.Interfaces
{
    public interface ICacheService
    {
        List<ReleaseInfo> Search(IIndexer indexer, TorznabQuery query);
        void CacheResults(IIndexer indexer, TorznabQuery query, List<ReleaseInfo> releases);
        IReadOnlyList<TrackerCacheResult> GetCachedResults();
        void CleanIndexerCache(IIndexer indexer);
        void CleanCache();
        TimeSpan CacheTTL { get; }
        void UpdateCacheConnectionString(string cacheconnectionString);
        void ClearCacheConnectionString();
        string GetCacheConnectionString { get; }
    }
}
