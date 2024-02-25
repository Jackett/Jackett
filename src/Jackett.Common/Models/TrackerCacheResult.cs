using System;
using System.Linq;

namespace Jackett.Common.Models
{
    public class TrackerCacheResult : ReleaseInfo
    {
        public DateTime FirstSeen { get; set; }
        public string Tracker { get; set; }
        public string TrackerId { get; set; }
        public string TrackerType { get; set; }
        public string CategoryDesc { get; set; }
        public Uri BlackholeLink { get; set; }

        public TrackerCacheResult(ReleaseInfo releaseInfo)
        {
            Title = releaseInfo.Title;
            Guid = releaseInfo.Guid;
            Link = releaseInfo.Link;
            Details = releaseInfo.Details;
            PublishDate = releaseInfo.PublishDate;
            Category = releaseInfo.Category;
            Size = releaseInfo.Size;
            Files = releaseInfo.Files;
            Grabs = releaseInfo.Grabs;
            Description = releaseInfo.Description;
            RageID = releaseInfo.RageID;
            TVDBId = releaseInfo.TVDBId;
            Imdb = releaseInfo.Imdb;
            TMDb = releaseInfo.TMDb;
            TVMazeId = releaseInfo.TVMazeId;
            TraktId = releaseInfo.TraktId;
            DoubanId = releaseInfo.DoubanId;
            Genres = releaseInfo.Genres;
            Year = releaseInfo.Year;
            Author = releaseInfo.Author;
            BookTitle = releaseInfo.BookTitle;
            Publisher = releaseInfo.Publisher;
            Artist = releaseInfo.Artist;
            Album = releaseInfo.Album;
            Label = releaseInfo.Label;
            Track = releaseInfo.Track;
            Seeders = releaseInfo.Seeders;
            Peers = releaseInfo.Peers;
            Poster = releaseInfo.Poster;
            InfoHash = releaseInfo.InfoHash;
            MagnetUri = releaseInfo.MagnetUri;
            MinimumRatio = releaseInfo.MinimumRatio;
            MinimumSeedTime = releaseInfo.MinimumSeedTime;
            DownloadVolumeFactor = releaseInfo.DownloadVolumeFactor;
            UploadVolumeFactor = releaseInfo.UploadVolumeFactor;

            CategoryDesc = Category != null ? string.Join(", ", Category.Select(TorznabCatType.GetCatDesc).Where(x => !string.IsNullOrEmpty(x))) : string.Empty;

            // Use peers as leechers
            Peers -= Seeders;
        }
    }
}
