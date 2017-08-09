using System.Collections.Generic;
using System.Linq;

namespace Jackett.Models.DTO
{
    public class TorrentPotatoResponse
    {
        public IEnumerable<TorrentPotatoResponseItem> results { get; set; }

        public int total_results
        {
            get
            {
                if (results == null)
                    return 0;
                return results.Count();
            }
        }
    }
}
