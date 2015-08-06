using AutoMapper;
using Jackett.Indexers;
using Jackett.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Services
{
    public interface ICacheService
    {
        int CacheRssResults(IIndexer indexer, IEnumerable<ReleaseInfo> releases);
        List<TrackerCacheResult> GetCachedResults(string serverUrl);
    }

    public class CacheService : ICacheService
    {
        private readonly List<TrackerCache> cache = new List<TrackerCache>();
        private readonly int MAX_RESULTS_PER_TRACKER = 250;
        private readonly TimeSpan AGE_LIMIT = new TimeSpan(7, 0, 0, 0);

        public int CacheRssResults(IIndexer indexer, IEnumerable<ReleaseInfo> releases)
        {
            lock (cache)
            {
                int newItemCount = 0;
                var trackerCache = cache.Where(c => c.TrackerId == indexer.ID).FirstOrDefault();
                if (trackerCache == null)
                {
                    trackerCache = new TrackerCache();
                    trackerCache.TrackerId = indexer.ID;
                    trackerCache.TrackerName = indexer.DisplayName;
                    cache.Add(trackerCache);
                }

                foreach(var release in releases.OrderByDescending(i=>i.PublishDate))
                {
                    // Skip old releases
                    if(release.PublishDate-DateTime.Now> AGE_LIMIT)
                    {
                        continue;
                    }

                    var existingItem = trackerCache.Results.Where(i => i.Result.Guid == release.Guid).FirstOrDefault();
                    if (existingItem == null)
                    {
                        existingItem = new CachedResult();
                        existingItem.Created = DateTime.Now;
                        trackerCache.Results.Add(existingItem);
                        newItemCount++;
                    }

                    existingItem.Result = release;
                }

                // Prune cache
                foreach(var tracker in cache)
                {
                    tracker.Results = tracker.Results.OrderByDescending(i => i.Created).Take(MAX_RESULTS_PER_TRACKER).ToList();
                }

                return newItemCount;
            }
        }

        public List<TrackerCacheResult> GetCachedResults(string serverUrl)
        {
            lock (cache)
            {
                var results = new List<TrackerCacheResult>();

                foreach(var tracker in cache)
                {
                    foreach(var release in tracker.Results)
                    {
                        var item = Mapper.Map<TrackerCacheResult>(release.Result);
                        item.FirstSeen = release.Created;
                        item.Tracker = tracker.TrackerName;
                        item.Peers = item.Peers - item.Seeders; // Use peers as leechers
                        item.ConvertToProxyLink(serverUrl, tracker.TrackerId);
                        results.Add(item);
                    }
                }

                return results.OrderByDescending(i=>i.PublishDate).ToList();
            }
        }
    }
}
