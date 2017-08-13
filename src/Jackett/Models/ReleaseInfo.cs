using Jackett.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Jackett.Indexers;

namespace Jackett.Models
{

    public class ReleaseInfo : ICloneable
    {
        public string Title { get; set; }
        public Uri Guid { get; set; }
        public Uri Link { get; set; }
        public Uri Comments { get; set; }
        public DateTime PublishDate { get; set; }
        public ICollection<int> Category { get; set; }
        public long? Size { get; set; }
        public long? Files { get; set; }
        public long? Grabs { get; set; }
        public string Description { get; set; }
        public long? RageID { get; set; }
        public long? TVDBId { get; set; }
        public long? Imdb { get; set; }
        public long? TMDb { get; set; }
        public int? Seeders { get; set; }
        public int? Peers { get; set; }
        public Uri BannerUrl { get; set; }
        public string InfoHash { get; set; }
        public Uri MagnetUri { get; set; }
        public double? MinimumRatio { get; set; }
        public long? MinimumSeedTime { get; set; }
        public double? DownloadVolumeFactor { get; set; }
        public double? UploadVolumeFactor { get; set; }
        public IIndexer Origin;

        public double? Gain
        {
            get
            {
                var sizeInGB = Size / 1024.0 / 1024.0 / 1024.0;
                return Seeders * sizeInGB;
            }
        }

        public object Clone()
        {
            return new ReleaseInfo()
            {
                Title = Title,
                Guid = Guid,
                Link = Link,
                Comments = Comments,
                PublishDate = PublishDate,
                Category = Category,
                Size = Size,
                Files = Files,
                Grabs = Grabs,
                Description = Description,
                RageID = RageID,
                Imdb = Imdb,
                TMDb = TMDb,
                Seeders = Seeders,
                Peers = Peers,
                BannerUrl = BannerUrl,
                InfoHash = InfoHash,
                MagnetUri = MagnetUri,
                MinimumRatio = MinimumRatio,
                MinimumSeedTime = MinimumSeedTime,
                DownloadVolumeFactor = DownloadVolumeFactor,
                UploadVolumeFactor = UploadVolumeFactor
            };
        }

        // ex: " 3.5  gb   "
        public static long GetBytes(string str)
        {
            var valStr = new string(str.Where(c => char.IsDigit(c) || c == '.').ToArray());
            var unit = new string(str.Where(char.IsLetter).ToArray());
            var val = ParseUtil.CoerceFloat(valStr);
            return GetBytes(unit, val);
        }

        public static long GetBytes(string unit, float value)
        {
            unit = unit.Replace("i", "").ToLowerInvariant();
            if (unit.Contains("kb"))
                return BytesFromKB(value);
            if (unit.Contains("mb"))
                return BytesFromMB(value);
            if (unit.Contains("gb"))
                return BytesFromGB(value);
            if (unit.Contains("tb"))
                return BytesFromTB(value);
            return (long)value;
        }

        public static long BytesFromTB(float tb)
        {
            return BytesFromGB(tb * 1024f);
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

        public override string ToString()
        {
            return string.Format("[ReleaseInfo: Title={0}, Guid={1}, Link={2}, Comments={3}, PublishDate={4}, Category={5}, Size={6}, Files={7}, Grabs={8}, Description={9}, RageID={10}, TVDBId={11}, Imdb={12}, TMDb={13}, Seeders={14}, Peers={15}, BannerUrl={16}, InfoHash={17}, MagnetUri={18}, MinimumRatio={19}, MinimumSeedTime={20}, DownloadVolumeFactor={21}, UploadVolumeFactor={22}, Gain={23}]", Title, Guid, Link, Comments, PublishDate, Category, Size, Files, Grabs, Description, RageID, TVDBId, Imdb, TMDb, Seeders, Peers, BannerUrl, InfoHash, MagnetUri, MinimumRatio, MinimumSeedTime, DownloadVolumeFactor, UploadVolumeFactor, Gain);
        }
    }
}
