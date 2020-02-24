using System.Collections.Generic;
using Jackett.Common.Indexers;
using Jackett.Common.Models;

namespace Jackett.Common.Services.Interfaces
{
    public interface ICacheService
    {
        void CacheRssResults(IIndexer indexer, IEnumerable<ReleaseInfo> releases);
        List<TrackerCacheResult> GetCachedResults();
        int GetNewItemCount(IIndexer indexer, IEnumerable<ReleaseInfo> releases);
    }
}
