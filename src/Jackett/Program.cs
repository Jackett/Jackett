using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jackett
{
    class Program
    {
        public static ManualResetEvent ExitEvent = new ManualResetEvent(false);

        static IndexerManager indexerManager;

        static void Main(string[] args)
        {
            indexerManager = new IndexerManager();

            var resultPage = new ResultPage(new ChannelInfo
            {
                Title = "HDAccess",
                Description = "Jackett for HDAccess",
                Link = new Uri("http://hdaccess.net"),
                ImageUrl = new Uri("https://hdaccess.net/logo_small.png"),
                ImageTitle = "HDAccess",
                ImageLink = new Uri("https://hdaccess.net"),
                ImageDescription = "Jackett for HDAccess"
            });

            resultPage.Releases.Add(new ReleaseInfo
            {
                Title = "Better Call Saul S01E05 Alpine Shepherd 1080p NF WEBRip DD5.1 x264",
                Guid = new Uri("https://hdaccess.net/details.php?id=11515"),
                Link = new Uri("https://hdaccess.net/download.php?torrent=11515&amp;passkey=123456"),
                Comments = new Uri("https://hdaccess.net/details.php?id=11515&amp;hit=1#comments"),
                PublishDate = DateTime.Now,
                Category = "HDTV 1080p",
                Size = 2538463390,
                Description = "Better.Call.Saul.S01E05.Alpine.Shepherd.1080p.NF.WEBRip.DD5.1.x264.torrent",
                Seeders = 7,
                Peers = 6,
                InfoHash = "63e07ff523710ca268567dad344ce1e0e6b7e8a3",
                MinimumRatio = 1.0,
                MinimumSeedTime = 172800
            });

            var f = resultPage.ToXml(new Uri("http://localhost:9117"));

            Task.Run(() =>
            {
                var server = new Server();
                server.Start();
            });
            ExitEvent.WaitOne();
        }
    }
}
