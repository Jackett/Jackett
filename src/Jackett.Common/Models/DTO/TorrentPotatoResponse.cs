using System.Collections.Generic;
using System.Linq;

namespace Jackett.Common.Models.DTO
{
    public class TorrentPotatoResponse
    {
        public IEnumerable<TorrentPotatoResponseItem> results { get; set; }

        public int total_results => results?.Count() ?? 0;
    }
}
