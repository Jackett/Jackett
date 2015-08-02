using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models
{
   public class TorrentPotatoResponse
    {
        public TorrentPotatoResponse()
        {
            results = new List<TorrentPotatoResponseItem>();
        }
        public List<TorrentPotatoResponseItem> results { get; set; }

        public int total_results
        {
            get { return results.Count; }
        }
    }
}
