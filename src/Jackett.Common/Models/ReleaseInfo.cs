using System;
using System.Collections.Generic;
using Jackett.Common.Indexers;
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
        public long? TVMazeId { get; set; }
        public long? TraktId { get; set; }
        public long? DoubanId { get; set; }
        public ICollection<string> Genres { get; set; }
        public ICollection<string> Languages { get; set; }
        public ICollection<string> Subs { get; set; }
        public long? Year { get; set; }
        public string Author { get; set; }
        public string BookTitle { get; set; }
        public string Publisher { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public string Label { get; set; }
        public string Track { get; set; }
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
            Category = new List<int>();
            Languages = new List<string>();
            Subs = new List<string>();
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
            TVDBId = copyFrom.TVDBId;
            Imdb = copyFrom.Imdb;
            TMDb = copyFrom.TMDb;
            TVMazeId = copyFrom.TVMazeId;
            TraktId = copyFrom.TraktId;
            DoubanId = copyFrom.DoubanId;
            Genres = copyFrom.Genres;
            Languages = copyFrom.Languages;
            Subs = copyFrom.Subs;
            Year = copyFrom.Year;
            Author = copyFrom.Author;
            BookTitle = copyFrom.BookTitle;
            Publisher = copyFrom.Publisher;
            Artist = copyFrom.Artist;
            Album = copyFrom.Album;
            Label = copyFrom.Label;
            Track = copyFrom.Track;
            Seeders = copyFrom.Seeders;
            Peers = copyFrom.Peers;
            Poster = copyFrom.Poster;
            InfoHash = copyFrom.InfoHash;
            MagnetUri = copyFrom.MagnetUri;
            MinimumRatio = copyFrom.MinimumRatio;
            MinimumSeedTime = copyFrom.MinimumSeedTime;
            DownloadVolumeFactor = copyFrom.DownloadVolumeFactor;
            UploadVolumeFactor = copyFrom.UploadVolumeFactor;
            Origin = copyFrom.Origin;
        }

        public virtual object Clone() => new ReleaseInfo(this);

        public override string ToString() => $"[ReleaseInfo: Title={Title}, Guid={Guid}, Link={Link}, Details={Details}, PublishDate={PublishDate}, Category={Category}, Size={Size}, Files={Files}, Grabs={Grabs}, Description={Description}, RageID={RageID}, TVDBId={TVDBId}, Imdb={Imdb}, TMDb={TMDb}, TVMazeId={TVMazeId}, TraktId={TraktId}, DoubanId={DoubanId}, Seeders={Seeders}, Peers={Peers}, Poster={Poster}, InfoHash={InfoHash}, MagnetUri={MagnetUri}, MinimumRatio={MinimumRatio}, MinimumSeedTime={MinimumSeedTime}, DownloadVolumeFactor={DownloadVolumeFactor}, UploadVolumeFactor={UploadVolumeFactor}, Gain={Gain}]";
    }
}
