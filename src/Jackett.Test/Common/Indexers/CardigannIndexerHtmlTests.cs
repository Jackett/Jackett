using System;
using System.Linq;
using System.Threading.Tasks;
using Jackett.Common.Indexers.Definitions;
using Jackett.Common.Models;
using Jackett.Test.TestHelpers;
using NLog;
using NUnit.Framework;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

// todo: add all fields from the search block (poster, imdbid, ...)
// todo: add definition with post
// todo: test download block
// todo: test login block
// todo: test settings block
// todo: test other search modes
// todo: review coverage, too many things missing (headers, encoding, ...)
namespace Jackett.Test.Common.Indexers
{
    [TestFixture]
    public class CardigannIndexerHtmlTests
    {
        private readonly TestWebClient _webClient = new TestWebClient();
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly TestCacheService _cacheService = new TestCacheService();

        [Test]
        public async Task TestCardigannHtmlAsync()
        {
            _webClient.RegisterRequestCallback("https://www.testdefinition1.cc/search?query=ubuntu&sort=created", "html-response1.html");
            var definition = LoadTestDefinition("html-definition1.yml");
            var indexer = new CardigannIndexer(null, _webClient, _logger, null, _cacheService, definition);

            var query = new TorznabQuery
            {
                QueryType = "search",
                SearchTerm = "ubuntu",
            };

            var result = await indexer.ResultsForQuery(query, false);
            Assert.AreEqual(false, result.IsFromCache);

            var releases = result.Releases.ToList();
            Assert.AreEqual(25, releases.Count);

            var firstRelease = releases.First();
            Assert.AreEqual(1, firstRelease.Category.Count);
            Assert.AreEqual(8000, firstRelease.Category.First());
            Assert.AreEqual("ubuntu-19.04-desktop-amd64.iso", firstRelease.Title);
            Assert.AreEqual("https://www.testdefinition1.cc/torrent/d540fc48eb12f2833163eed6421d449dd8f1ce1f", firstRelease.Details.ToString());
            Assert.AreEqual("http://itorrents.org/torrent/d540fc48eb12f2833163eed6421d449dd8f1ce1f.torrent", firstRelease.Link.ToString());
            Assert.AreEqual("http://itorrents.org/torrent/d540fc48eb12f2833163eed6421d449dd8f1ce1f.torrent", firstRelease.Guid.ToString());
            Assert.AreEqual("magnet:?xt=urn:btih:d540fc48eb12f2833163eed6421d449dd8f1ce1f&dn=ubuntu-19.04-desktop-amd64.iso",
                             firstRelease.MagnetUri.ToString().Split(new[] { "&tr" }, StringSplitOptions.None).First());
            Assert.AreEqual("d540fc48eb12f2833163eed6421d449dd8f1ce1f", firstRelease.InfoHash);
            Assert.AreEqual(2023, firstRelease.PublishDate.Year);
            Assert.AreEqual(2097152000, firstRelease.Size);
            Assert.AreEqual(12, firstRelease.Seeders);
            Assert.AreEqual(13, firstRelease.Peers);
            Assert.AreEqual(1, firstRelease.DownloadVolumeFactor);
            Assert.AreEqual(2, firstRelease.UploadVolumeFactor);
            Assert.AreEqual(23.4375, firstRelease.Gain);
        }

        private static IndexerDefinition LoadTestDefinition(string fileName)
        {
            var definitionString = TestUtil.LoadTestFile(fileName);
            var deserializer = new DeserializerBuilder()
                               .WithNamingConvention(CamelCaseNamingConvention.Instance)
                               .Build();
            return deserializer.Deserialize<IndexerDefinition>(definitionString);
        }
    }
}
