using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Jackett.Common.Indexers;
using Jackett.Common.Models;
using Jackett.Common.Models.Config;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Newtonsoft.Json;
using NLog;

namespace Jackett.Common.Services
{
    /// <summary>
    /// This service is in charge of Jackett cache. In simple words, when you make a request in Jackett, the results are
    /// saved in memory (cache). The next request will return results form the cache improving response time and making
    /// fewer requests to the sites.
    /// * We assume all indexers/sites are stateless, the same request return the same response. If you change the
    ///   search term, categories or something in the query Jackett has to make a live request to the indexer.
    /// * There are some situations when we don't want to use the cache:
    ///   * When we are testing the indexers => if query.IsTest results are not cached
    ///   * When the user updates the configuration of one indexer => We call CleanIndexerCache to remove cached results
    ///     before testing the configuration
    ///   * When there is some error/exception in the indexer => The results are not cached so we can retry in the
    ///     next request
    ///   * When the user changes proxy configuration => We call CleanCache to remove all cached results. The user will
    ///     be able to test the proxy
    /// * We want to limit the memory usage, so we try to remove elements from cache ASAP:
    ///   * Each indexer can have a maximum number of results in memory. If the limit is exceeded we remove old results
    ///   * Cached results expire after some time
    /// * Users can configure the cache or even disable it
    /// </summary>
    public class CacheService : ICacheService
    {
        private readonly Logger _logger;
        private readonly ServerConfig _serverConfig;
        private readonly SHA256Managed _sha256 = new SHA256Managed();
        private readonly Dictionary<string, TrackerCache> _cache = new Dictionary<string, TrackerCache>();

        public CacheService(Logger logger, ServerConfig serverConfig)
        {
            _logger = logger;
            _serverConfig = serverConfig;
        }

        public void CacheResults(IIndexer indexer, TorznabQuery query, List<ReleaseInfo> releases)
        {
            // do not cache test queries!
            if (query.IsTest)
                return;

            lock (_cache)
            {
                if (!IsCacheEnabled())
                    return;

                if (!_cache.ContainsKey(indexer.Id))
                {
                    _cache.Add(indexer.Id, new TrackerCache
                    {
                        TrackerId = indexer.Id,
                        TrackerName = indexer.Name,
                        TrackerType = indexer.Type
                    });
                }

                var trackerCacheQuery = new TrackerCacheQuery
                {
                    Created = DateTime.Now,
                    Results = releases.Select(r => (ReleaseInfo)r.Clone()).ToList()
                };

                var trackerCache = _cache[indexer.Id];
                var queryHash = GetQueryHash(query);
                if (trackerCache.Queries.ContainsKey(queryHash))
                    trackerCache.Queries[queryHash] = trackerCacheQuery; // should not happen, just in case
                else
                    trackerCache.Queries.Add(queryHash, trackerCacheQuery);

                _logger.Debug($"CACHE CacheResults / Indexer: {trackerCache.TrackerId} / Added: {releases.Count} releases");

                PruneCacheByMaxResultsPerIndexer(trackerCache); // remove old results if we exceed the maximum limit
            }
        }

        public List<ReleaseInfo> Search(IIndexer indexer, TorznabQuery query)
        {
            lock (_cache)
            {
                if (!IsCacheEnabled())
                    return null;

                PruneCacheByTtl(); // remove expired results

                if (!_cache.ContainsKey(indexer.Id))
                    return null;

                var trackerCache = _cache[indexer.Id];
                var queryHash = GetQueryHash(query);
                var cacheHit = trackerCache.Queries.ContainsKey(queryHash);

                if (_logger.IsDebugEnabled)
                    _logger.Debug($"CACHE Search / Indexer: {trackerCache.TrackerId} / CacheHit: {cacheHit} / Query: {GetSerializedQuery(query)}");

                if (!cacheHit)
                    return null;

                var releases = trackerCache.Queries[queryHash].Results;
                _logger.Debug($"CACHE Search Hit / Indexer: {trackerCache.TrackerId} / Found: {releases.Count} releases");

                return releases;
            }
        }

        public IReadOnlyList<TrackerCacheResult> GetCachedResults()
        {
            lock (_cache)
            {
                if (!IsCacheEnabled())
                    return Array.Empty<TrackerCacheResult>();

                PruneCacheByTtl(); // remove expired results

                var result = _cache.Values.SelectMany(
                                       trackerCache => trackerCache.Queries.Values
                                                                   .OrderByDescending(q => q.Created)
                                                                   .SelectMany(
                                                                       query => query.Results.Select(release =>
                                                                           new TrackerCacheResult(release)
                                                                           {
                                                                               FirstSeen = query.Created,
                                                                               Tracker = trackerCache.TrackerName,
                                                                               TrackerId = trackerCache.TrackerId,
                                                                               TrackerType = trackerCache.TrackerType
                                                                           }))
                                                                   .GroupBy(r => r.Guid)
                                                                   .Select(y => y.First())
                                                                   .Take(300))
                                   .OrderByDescending(i => i.PublishDate)
                                   .Take(3000).ToList();

                _logger.Debug($"CACHE GetCachedResults / Results: {result.Count} (cache may contain more results)");
                PrintCacheStatus();

                return result;
            }
        }

        public void CleanIndexerCache(IIndexer indexer)
        {
            lock (_cache)
            {
                if (!IsCacheEnabled())
                    return;

                if (_cache.ContainsKey(indexer.Id))
                    _cache.Remove(indexer.Id);

                _logger.Debug($"CACHE CleanIndexerCache / Indexer: {indexer.Id}");

                PruneCacheByTtl(); // remove expired results
            }
        }

        public void CleanCache()
        {
            lock (_cache)
            {
                if (!IsCacheEnabled())
                    return;

                _cache.Clear();
                _logger.Debug("CACHE CleanCache");
            }
        }

        public TimeSpan CacheTTL => TimeSpan.FromSeconds(_serverConfig.CacheTtl);

        private bool IsCacheEnabled()
        {
            if (!_serverConfig.CacheEnabled)
            {
                // remove cached results just in case user disabled cache recently
                _cache.Clear();
                _logger.Debug("CACHE IsCacheEnabled => false");
            }
            return _serverConfig.CacheEnabled;
        }

        private void PruneCacheByTtl()
        {
            var prunedCounter = 0;
            var expirationDate = DateTime.Now.AddSeconds(-_serverConfig.CacheTtl);
            foreach (var trackerCache in _cache.Values)
            {
                // Remove expired queries
                var queriesToRemove = trackerCache.Queries
                    .Where(q => q.Value.Created < expirationDate)
                    .Select(q => q.Key).ToList();
                foreach (var queryHash in queriesToRemove)
                    trackerCache.Queries.Remove(queryHash);
                prunedCounter += queriesToRemove.Count;
            }
            if (_logger.IsDebugEnabled)
            {
                _logger.Debug($"CACHE PruneCacheByTtl / Pruned queries: {prunedCounter}");
                PrintCacheStatus();
            }
        }

        private void PruneCacheByMaxResultsPerIndexer(TrackerCache trackerCache)
        {
            // Remove queries exceeding max results per indexer
            var resultsPerQuery = trackerCache.Queries
                .OrderByDescending(q => q.Value.Created) // newest first
                .Select(q => new Tuple<string, int>(q.Key, q.Value.Results.Count)).ToList();

            var prunedCounter = 0;
            while (true)
            {
                var total = resultsPerQuery.Select(q => q.Item2).Sum();
                if (total <= _serverConfig.CacheMaxResultsPerIndexer)
                    break;
                var olderQuery = resultsPerQuery.Last();
                trackerCache.Queries.Remove(olderQuery.Item1); // remove the older
                resultsPerQuery.Remove(olderQuery);
                prunedCounter++;
            }

            if (_logger.IsDebugEnabled)
            {
                _logger.Debug($"CACHE PruneCacheByMaxResultsPerIndexer / Indexer: {trackerCache.TrackerId} / Pruned queries: {prunedCounter}");
                PrintCacheStatus();
            }
        }

        private string GetQueryHash(TorznabQuery query)
        {
            var json = GetSerializedQuery(query);
            // Compute the hash
            return BitConverter.ToString(_sha256.ComputeHash(Encoding.UTF8.GetBytes(json)));
        }

        private static string GetSerializedQuery(TorznabQuery query)
        {
            var json = JsonConvert.SerializeObject(query);

            // Changes in the query to improve cache hits
            // Both request must return the same results, if not we are breaking Jackett search
            json = json.Replace("\"SearchTerm\":null", "\"SearchTerm\":\"\"");

            return json;
        }

        private void PrintCacheStatus()
        {
            _logger.Debug($"CACHE Status / Total cached results: {_cache.Values.SelectMany(tc => tc.Queries).Select(q => q.Value.Results.Count).Sum()}");
        }
    }
}
