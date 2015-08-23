using Jackett.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett
{
    public class ManualSearchResult
    {
        public List<TrackerCacheResult> Results { get; set; }
        public List<string> Indexers { get; set; }
    }
}
