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
        private readonly List<TrackerCache> _cache = new List<TrackerCache>();
        private readonly int _maxResultsPerTracker = 1000;
        private readonly TimeSpan _ageLimit = new TimeSpan(0, 1, 0, 0);

        public void CacheRssResults(IIndexer indexer, IEnumerable<ReleaseInfo> releases)
        {
            lock (_cache)
            {
                var trackerCache = _cache.Where(c => c.TrackerId == indexer.ID).FirstOrDefault();
                if (trackerCache == null)
                {
                    trackerCache = new TrackerCache { TrackerId = indexer.ID, TrackerName = indexer.DisplayName };
                    _cache.Add(trackerCache);
                }

                foreach (var release in releases.OrderByDescending(i => i.PublishDate))
                {
                    var existingItem = trackerCache.Results.Where(i => i.Result.Guid == release.Guid).FirstOrDefault();
                    if (existingItem == null)
                    {
                        existingItem = new CachedResult { Created = DateTime.Now };
                        trackerCache.Results.Add(existingItem);
                    }

                    existingItem.Result = release;
                }

                // Prune cache
                foreach (var tracker in _cache)
                    tracker.Results = tracker.Results.Where(x => x.Created > DateTime.Now.Subtract(_ageLimit))
                                             .OrderByDescending(i => i.Created).Take(_maxResultsPerTracker).ToList();
            }
        }

        public int GetNewItemCount(IIndexer indexer, IEnumerable<ReleaseInfo> releases)
        {
            lock (_cache)
            {
                var newItemCount = 0;
                var trackerCache = _cache.Where(c => c.TrackerId == indexer.ID).FirstOrDefault();
                if (trackerCache != null)
                {
                    foreach (var release in releases)
                        if (trackerCache.Results.Where(i => i.Result.Guid == release.Guid).Count() == 0)
                            newItemCount++;
                }
                else
                    newItemCount++;

                return newItemCount;
            }
        }

        public List<TrackerCacheResult> GetCachedResults()
        {
            lock (_cache)
            {
                var results = new List<TrackerCacheResult>();
                foreach (var tracker in _cache)
                    foreach (var release in tracker.Results.OrderByDescending(i => i.Result.PublishDate).Take(300))
                    {
                        var item = Mapper.Map<TrackerCacheResult>(release.Result);
                        item.FirstSeen = release.Created;
                        item.Tracker = tracker.TrackerName;
                        item.TrackerId = tracker.TrackerId;
                        item.Peers -= item.Seeders; // Use peers as leechers
                        results.Add(item);
                    }

                return results.Take(3000).OrderByDescending(i => i.PublishDate).ToList();
            }
        }
    }
}
