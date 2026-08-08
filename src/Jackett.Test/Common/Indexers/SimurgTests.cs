using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jackett.Common.Indexers.Definitions;
using Jackett.Common.Models;
using Jackett.Test.TestHelpers;
using NLog;
using NUnit.Framework;

namespace Jackett.Test.Common.Indexers
{
    [TestFixture]
    public class SimurgTests
    {
        private readonly TestWebClient _webClient = new TestWebClient();
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly TestCacheService _cacheService = new TestCacheService();

        [Test]
        public async Task TestRecentFeedAsync()
        {
            _webClient.RegisterRequestCallback("https://simurg.world/ajax.php?action=browse&order_by=time&order_way=desc", "Simurg/recent-feed.json");

            var indexer = new Simurg(null, _webClient, _logger, null, _cacheService);

            var query = new TorznabQuery { QueryType = "search" };

            var result = await indexer.ResultsForQuery(query, false);
            result.IsFromCache.Should().BeFalse();

            var releases = result.Releases.ToList();
            releases.Should().HaveCount(2);

            var firstRelease = releases.First();
            firstRelease.Category.Should().HaveCount(2);
            firstRelease.Category.Should().BeEquivalentTo(new[] { 7020, 100003 });
            firstRelease.Title.Should().Be("A Spot of Tea and Sorcery: Volume 1");
            firstRelease.Link.Should().Be("https://simurg.world/torrents.php?action=download&id=10");
            firstRelease.Guid.Should().Be("https://simurg.world/torrents.php?torrentid=10");
            firstRelease.Size.Should().Be(3059860L);
            firstRelease.Seeders.Should().Be(1);
            firstRelease.Peers.Should().Be(1);
            firstRelease.DownloadVolumeFactor.Should().Be(1);
            firstRelease.UploadVolumeFactor.Should().Be(1);
        }
    }
}
