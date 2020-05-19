using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Jackett.Common.Indexers;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;

namespace Jackett.Common.Services
{

    public class CacheService : ICacheService
    {
        private readonly List<TrackerCache> cache = new List<TrackerCache>();
        private readonly int MAX_RESULTS_PER_TRACKER = 1000;
        private readonly TimeSpan AGE_LIMIT = new TimeSpan(0, 1, 0, 0);

        public void CacheRssResults(IIndexer indexer, IEnumerable<ReleaseInfo> releases)
        {
            lock (cache)
            {
                var trackerCache = cache.FirstOrDefault(c => c.TrackerId == indexer.Id);
                if (trackerCache == null)
                {
                    trackerCache = new TrackerCache
                    {
                        TrackerId = indexer.Id,
                        TrackerName = indexer.DisplayName
                    };
                    cache.Add(trackerCache);
                }

                foreach (var release in releases.OrderByDescending(i => i.PublishDate))
                {
                    var existingItem = trackerCache.Results.FirstOrDefault(i => i.Result.Guid == release.Guid);
                    if (existingItem == null)
                    {
                        existingItem = new CachedResult
                        {
                            Created = DateTime.Now
                        };
                        trackerCache.Results.Add(existingItem);
                    }

                    existingItem.Result = release;
                }

                // Prune cache
                foreach (var tracker in cache)
                {
                    tracker.Results = tracker.Results.Where(x => x.Created > DateTime.Now.Subtract(AGE_LIMIT)).OrderByDescending(i => i.Created).Take(MAX_RESULTS_PER_TRACKER).ToList();
                }
            }
        }

        public int GetNewItemCount(IIndexer indexer, IEnumerable<ReleaseInfo> releases)
        {
            lock (cache)
            {
                var newItemCount = 0;
                var trackerCache = cache.FirstOrDefault(c => c.TrackerId == indexer.Id);
                if (trackerCache != null)
                {
                    foreach (var release in releases)
                    {
                        if (trackerCache.Results.Count(i => i.Result.Guid == release.Guid) == 0)
                        {
                            newItemCount++;
                        }
                    }
                }
                else
                {
                    newItemCount++;
                }

                return newItemCount;
            }
        }

        public List<TrackerCacheResult> GetCachedResults()
        {
            lock (cache)
            {
                var results = new List<TrackerCacheResult>();

                foreach (var tracker in cache)
                {
                    foreach (var release in tracker.Results.OrderByDescending(i => i.Result.PublishDate).Take(300))
                    {
                        var item = Mapper.Map<TrackerCacheResult>(release.Result);
                        item.FirstSeen = release.Created;
                        item.Tracker = tracker.TrackerName;
                        item.TrackerId = tracker.TrackerId;
                        item.Peers = item.Peers - item.Seeders; // Use peers as leechers
                        results.Add(item);
                    }
                }

                return results.Take(3000).OrderByDescending(i => i.PublishDate).ToList();
            }
        }
    }
}
