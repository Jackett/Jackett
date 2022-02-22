
using System.Collections.Generic;

namespace Jackett.Common.Models
{
    internal class TrackerCache
    {
        public string TrackerId { set; get; }
        public string TrackerName { set; get; }
        public string TrackerType { set; get; }
        public Dictionary<string, TrackerCacheQuery> Queries = new Dictionary<string, TrackerCacheQuery>();
    }
}
