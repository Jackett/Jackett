using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models
{
    public class TorrentPotatoResponseItem
    {
        public string release_name { get; set; }
        public string torrent_id { get; set; }
        public string details_url { get; set; }
        public string download_url { get; set; }
        public string imdb_id { get; set; }
        public bool freeleech { get; set; }
        public string type { get; set; }
        public long size { get; set; }
        public int leechers { get; set; }
        public int seeders { get; set; }
        public string publish_date { get; set; }
    }
}
