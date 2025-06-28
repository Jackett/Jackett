using System;

namespace Jackett.Common.Models.DTO.Anilibria
{
    public class AnilibriaTorrentInfo
    {
        public long Id { get; set; }
        public string Hash { get; set; }
        public long Size { get; set; }
        public string Magnet { get; set; }
        public long Seeders { get; set; }
        public long Leechers { get; set; }
        public string Label { get; set; }
        public string NameMain { get; set; }
        public string NameEnglish { get; set; }
        public string Alias { get; set; }
        public string PosterSrc { get; set; }
        public DateTime CreatedAt { get; set; }
        public int Year { get; set; }
        public long Grabs { get; set; }
        public string Category { get; set; }
    }
}
