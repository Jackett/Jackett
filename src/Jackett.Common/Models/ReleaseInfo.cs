using System;
using System.Collections.Generic;
using System.Linq;
using Jackett.Common.Indexers;
using Jackett.Common.Utils;
using Newtonsoft.Json;

namespace Jackett.Common.Models
{

    public class ReleaseInfo : ICloneable
    {
        public string Title { get; set; }
        public Uri Guid { get; set; }
        public Uri Link { get; set; }
        public Uri Details { get; set; }
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
        public string Author { get; set; }
        public string BookTitle { get; set; }
        public long? Seeders { get; set; }
        public long? Peers { get; set; }
        public Uri Poster { get; set; }
        public string InfoHash { get; set; }
        public Uri MagnetUri { get; set; }
        public double? MinimumRatio { get; set; }
        public long? MinimumSeedTime { get; set; }
        public double? DownloadVolumeFactor { get; set; }
        public double? UploadVolumeFactor { get; set; }
        [JsonIgnore] // don't export the Origin to the manual search API, otherwise each result line contains a full recursive indexer JSON structure
        public IIndexer Origin;


        public static double? GigabytesFromBytes(double? size) => size / 1024.0 / 1024.0 / 1024.0;
        public double? Gain => Seeders * GigabytesFromBytes(Size);

        public ReleaseInfo()
        {
        }

        protected ReleaseInfo(ReleaseInfo copyFrom)
        {
            Title = copyFrom.Title;
            Guid = copyFrom.Guid;
            Link = copyFrom.Link;
            Details = copyFrom.Details;
            PublishDate = copyFrom.PublishDate;
            Category = copyFrom.Category;
            Size = copyFrom.Size;
            Files = copyFrom.Files;
            Grabs = copyFrom.Grabs;
            Description = copyFrom.Description;
            RageID = copyFrom.RageID;
            Imdb = copyFrom.Imdb;
            TMDb = copyFrom.TMDb;
            Author = copyFrom.Author;
            BookTitle = copyFrom.BookTitle;
            Seeders = copyFrom.Seeders;
            Peers = copyFrom.Peers;
            Poster = copyFrom.Poster;
            InfoHash = copyFrom.InfoHash;
            MagnetUri = copyFrom.MagnetUri;
            MinimumRatio = copyFrom.MinimumRatio;
            MinimumSeedTime = copyFrom.MinimumSeedTime;
            DownloadVolumeFactor = copyFrom.DownloadVolumeFactor;
            UploadVolumeFactor = copyFrom.UploadVolumeFactor;
        }

        public virtual object Clone() => new ReleaseInfo(this);

        // ex: " 3.5  gb   " -> "3758096384" , "3,5GB" -> "3758096384" ,  "296,98 MB" -> "311406100.48" , "1.018,29 MB" -> "1067754455.04"
        // ex:  "1.018.29mb" -> "1067754455.04" , "-" -> "0" , "---" -> "0"
        public static long GetBytes(string str)
        {
            var valStr = new string(str.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
            valStr = (valStr.Length == 0) ? "0" : valStr.Replace(",", ".");
            if (valStr.Count(c => c == '.') > 1)
            {
                var lastOcc = valStr.LastIndexOf('.');
                valStr = valStr.Substring(0, lastOcc).Replace(".", string.Empty) + valStr.Substring(lastOcc);
            }
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

        public static long BytesFromTB(float tb) => BytesFromGB(tb * 1024f);

        public static long BytesFromGB(float gb) => BytesFromMB(gb * 1024f);

        public static long BytesFromMB(float mb) => BytesFromKB(mb * 1024f);

        public static long BytesFromKB(float kb) => (long)(kb * 1024f);

        public override string ToString() =>
            $"[ReleaseInfo: Title={Title}, Guid={Guid}, Link={Link}, Details={Details}, PublishDate={PublishDate}, Category={Category}, Size={Size}, Files={Files}, Grabs={Grabs}, Description={Description}, RageID={RageID}, TVDBId={TVDBId}, Imdb={Imdb}, TMDb={TMDb}, Seeders={Seeders}, Peers={Peers}, Poster={Poster}, InfoHash={InfoHash}, MagnetUri={MagnetUri}, MinimumRatio={MinimumRatio}, MinimumSeedTime={MinimumSeedTime}, DownloadVolumeFactor={DownloadVolumeFactor}, UploadVolumeFactor={UploadVolumeFactor}, Gain={Gain}]";
    }
}
