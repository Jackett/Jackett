using LiteDB;
using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Website.Models;
using Release = Website.Models.Release;

namespace Website.Services
{
    public class DatabaseService
    {
        private static string GetDBPath()
        {
            return Path.Combine(HttpRuntime.AppDomainAppPath, "App_data", "web.db");
        }

        public async Task Sync()
        {
            var github = new GitHubClient(new ProductHeaderValue("Jackett"));
            var releases = await github.Release.GetAll("zone117x", "Jackett");

            if (releases.Count > 0)
            {
                using (var db = new LiteDatabase(GetDBPath()))
                {
                    var releaseCollection = db.GetCollection<Release>("Releases");
                    releaseCollection.Drop();
                    releaseCollection.EnsureIndex(x => x.When);

                    foreach (var release in releases)
                    {
                        releaseCollection.Insert(new Release()
                        {
                            When = release.PublishedAt.Value.DateTime,
                            Description = release.Body,
                            Title = release.Name,
                            Url = release.HtmlUrl,
                            Version = release.TagName
                        });
                    }
                }
            }
        }

        public static List<Release> GetReleases()
        {
            using (var db = new LiteDatabase(GetDBPath()))
            {
                var releaseCollection = db.GetCollection<Release>("Releases");
                return releaseCollection.FindAll().OrderByDescending(x => x.When).ToList();
            }
        }

        public static void RecordDownload(string version, string file)
        {
            using (var db = new LiteDatabase(GetDBPath()))
            {
                var countCollection = db.GetCollection<DownloadCount>("DownloadCount");
                var counter = countCollection.Find(x => x.File == file && x.Version == version).FirstOrDefault();
                if (counter == null)
                {
                    counter = new DownloadCount()
                    {
                        File = file,
                        Version = version,
                        Count = 1
                    };
                    countCollection.Insert(counter);
                }
                else
                {
                    counter.Count++;
                    countCollection.Update(counter);
                }
            }
        }

        public static int GetDownloadCount(string version, string file)
        {
            using (var db = new LiteDatabase(GetDBPath()))
            {
                var countCollection = db.GetCollection<DownloadCount>("DownloadCount");
                var counter = countCollection.Find(x => x.File == file && x.Version == version).FirstOrDefault();
                if (counter == null)
                    return 0;
                return counter.Count;
            }
        }
    }
}
