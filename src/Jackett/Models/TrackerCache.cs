using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models
{
    class TrackerCache
    {
        public string TrackerId { set; get; }
        public string TrackerName { set; get; }

        public List<CachedResult> Results = new List<CachedResult>();
    }
}
