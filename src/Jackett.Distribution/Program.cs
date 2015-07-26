using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
using Octokit;
using Octokit.Internal;

namespace Jackett.Distribution
{
    class Program
    {
        static readonly string repoOwner = "zone117x";
        static readonly string repoName = "Jackett";
        static readonly string buildZipFile = "JackettBuild.zip";
        static readonly string installFile = Path.Combine("Output", "setup.exe");

        static GitHubClient github;
        static Version localVersion;

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                throw new Exception("Missing github API token argument");
            }
            var token = args[0];
            github = new GitHubClient(new ProductHeaderValue("Jackett"));
            github.Credentials = new Credentials(token);

            localVersion = GetJackettVersion();
            var latestReleaseVersion = LatestGithubRelease().Result;
            if (localVersion <= latestReleaseVersion)
            {
                Console.WriteLine("Latest Github release is {0}, will not upload local version {1}", latestReleaseVersion, localVersion);
                return;
            }

            Console.WriteLine("Zipping release build " + localVersion);
            ZippingReleaseBuild();

            UploadRelease().Wait();
        }

        static Version GetJackettVersion()
        {
            var assemblyVersion = AssemblyName.GetAssemblyName(Path.Combine("Build", "Jackett.dll")).Version;
            return new Version(assemblyVersion.Major, assemblyVersion.Minor, assemblyVersion.Build);
        }

        static async Task<Version> LatestGithubRelease()
        {
            var releases = await github.Release.GetAll(repoOwner, repoName);
            var latest = releases.Where(t => t.PublishedAt != null).OrderByDescending(t => t.TagName).FirstOrDefault();
            var version = Version.Parse(latest.TagName.Replace("v", ""));
            return version;
        }

        static void ZippingReleaseBuild()
        {
            if (File.Exists(buildZipFile))
                File.Delete(buildZipFile);
            ZipFile.CreateFromDirectory("Build", buildZipFile);
        }

        static async Task UploadRelease()
        {
            // get last master commit to tag
            /*var masterBranch = await github.Repository.GetBranch(repoOwner, repoName, "master");
            var lastCommit = masterBranch.Commit.Sha;

            // create tag
            var tagName = "v" + localVersion.ToString();
            var tag = new NewTag
            {
                Message = "Tagging a new release of Jackett",
                Tag = tagName,
                Object = lastCommit,
                Type = TaggedType.Commit,
                Tagger = new SignatureResponse("DistributionBot", "zone117x@gmail.com", DateTime.UtcNow)
            };
            var tagResult = await github.GitDatabase.Tag.Create(repoOwner, repoName, tag);*/

            // create release entry
            var newRelease = new NewRelease("v" + localVersion.ToString());
            newRelease.Name = "Beta Release";
            newRelease.Body = "";
            newRelease.Draft = true;
            newRelease.Prerelease = false;

            var releaseResult = await github.Release.Create(repoOwner, repoName, newRelease);


            var buildZipAsset = new ReleaseAssetUpload()
            {
                FileName = string.Format("Jackett.v{0}.zip", localVersion),
                ContentType = "application/zip",
                RawData = File.OpenRead(buildZipFile)
            };
            var buildZipAssetResult = await github.Release.UploadAsset(releaseResult, buildZipAsset);

            var installFileAsset = new ReleaseAssetUpload()
            {
                FileName = string.Format("Jackett.v{0}.Windows.Installer.exe", localVersion),
                ContentType = "application/octet-stream",
                RawData = File.OpenRead(installFile)
            };
            var installFileAssetResult = await github.Release.UploadAsset(releaseResult, installFileAsset);

        }
    }
}
