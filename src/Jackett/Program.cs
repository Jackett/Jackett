using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Jackett
{
    class Program
    {
        public static string AppConfigDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Jackett");

        public static ManualResetEvent ExitEvent = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            //var descRegex = new Regex("Uploaded (?<month>.*?)-(?<day>.*?) (?<year>.*?), Size (?<size>.*?) (?<unit>.*?), ULed");
            var descRegex = new Regex("Uploaded (?<month>.*?)-(?<day>.*?) (?<year>.*?), Size (?<size>.*?) (?<unit>.*?), ULed by");
            var m = descRegex.Match(("Uploaded 06-03 2013, Size 329.84 MiB, ULed by"));
            List<string> matches = new List<string>();
            var date = m.Groups["month"].Value;
            for (var i = 0; i < m.Groups.Count; i++)
            {
                var group = m.Groups[i];
                matches.Add(group.Value); ;
            }
            //Uploaded 08-02&nbsp;2007, Size 47.15&nbsp;MiB, ULed
            //Uploaded (<date>.*?)&nbsp;2007, Size 47.15&nbsp;MiB, ULed

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
