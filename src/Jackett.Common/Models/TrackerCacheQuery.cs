using System;
using System.Collections.Generic;

namespace Jackett.Common.Models
{
    internal class TrackerCacheQuery
    {
        public DateTime Created
        {
            set; get;
        }

        public List<ReleaseInfo> Results = new List<ReleaseInfo>();
    }
}
