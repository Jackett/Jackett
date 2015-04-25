using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett
{

    public class ReleaseInfo
    {
        public string Title { get; set; }
        public Uri Guid { get; set; }
        public Uri Link { get; set; }
        public Uri Comments { get; set; }
        public DateTime PublishDate { get; set; }
        public string Category { get; set; }
        public long? Size { get; set; }
        public string Description { get; set; }
        public long? RageID { get; set; }
        public long? Imdb { get; set; }
        public int? Seeders { get; set; }
        public int? Peers { get; set; }
        public Uri ConverUrl { get; set; }
        public Uri BannerUrl { get; set; }
        public string InfoHash { get; set; }
        public Uri MagnetUri { get; set; }
        public double? MinimumRatio { get; set; }
        public long? MinimumSeedTime { get; set; }


        public static long GetBytes(string unit, float value)
        {
            switch (unit.ToLower())
            {
                case "kb":
                case "kib":
                    return BytesFromKB(value);
                case "mb":
                case "mib":
                    return BytesFromMB(value);
                case "gb":
                case "gib":
                    return BytesFromGB(value);
                default:
                    return 0;
            }
        }

        public static long BytesFromGB(float gb)
        {
            return BytesFromMB(gb * 1024f);
        }

        public static long BytesFromMB(float mb)
        {
            return BytesFromKB(mb * 1024f);
        }

        public static long BytesFromKB(float kb)
        {
            return (long)(kb * 1024f);
        }

    }
}
