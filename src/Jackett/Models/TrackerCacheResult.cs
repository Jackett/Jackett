using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models
{
    public class TrackerCacheResult : ReleaseInfo
    {
        public DateTime FirstSeen { get; set; }
        public string Tracker { get; set; }
        public string TrackerId { get; set; }
        public string CategoryDesc { get; set; }
        public Uri BlackholeLink { get; set; }
    }
}
