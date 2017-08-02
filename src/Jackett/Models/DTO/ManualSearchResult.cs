using Jackett.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models.DTO
{
    public class ManualSearchResult
    {
        public IEnumerable<TrackerCacheResult> Results { get; set; }
        public IEnumerable<string> Indexers { get; set; }
    }
}
