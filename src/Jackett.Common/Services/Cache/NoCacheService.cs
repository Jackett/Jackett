using System;
using System.Collections.Generic;
using Jackett.Common.Indexers;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using NLog;

namespace Jackett.Common.Services.Cache
{
    public class NoCacheService : ICacheService
    {
        private readonly Logger _logger;
        public NoCacheService(Logger logger)
        {
            _logger = logger;
        }

        public void CacheResults(IIndexer indexer, TorznabQuery query, List<ReleaseInfo> releases)
        {
            // No operation
        }

        public List<ReleaseInfo> Search(IIndexer indexer, TorznabQuery query)
        {
            // No operation
            return null;
        }

        public IReadOnlyList<TrackerCacheResult> GetCachedResults()
        {
            // No operation
            return Array.Empty<TrackerCacheResult>();
        }

        public void CleanIndexerCache(IIndexer indexer)
        {
            // No operation
        }

        public void CleanCache()
        {
            // No operation
        }

        public TimeSpan CacheTTL => TimeSpan.Zero; // No cache expiration
        public void UpdateCacheConnectionString(string cacheconnectionString)
        {
            _logger.Info("Cache Disabled");
        }
        public void ClearCacheConnectionString()
        {

        }

        public string GetCacheConnectionString { get; }
    }
}
