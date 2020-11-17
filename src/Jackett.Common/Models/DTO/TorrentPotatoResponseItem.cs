namespace Jackett.Common.Models.DTO
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
        public long leechers { get; set; }
        public long seeders { get; set; }
        public string publish_date { get; set; }
    }
}
