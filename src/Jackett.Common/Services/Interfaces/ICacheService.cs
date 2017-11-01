using Jackett.Indexers;
using Jackett.Models;
using System.Collections.Generic;

namespace Jackett.Services.Interfaces
{
    public interface ICacheService
    {
        void CacheRssResults(IIndexer indexer, IEnumerable<ReleaseInfo> releases);
        List<TrackerCacheResult> GetCachedResults();
        int GetNewItemCount(IIndexer indexer, IEnumerable<ReleaseInfo> releases);
    }
}
