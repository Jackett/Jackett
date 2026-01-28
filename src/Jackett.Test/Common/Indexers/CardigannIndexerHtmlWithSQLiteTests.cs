using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Autofac;
using Dapper;
using Jackett.Common.Indexers.Definitions;
using Jackett.Common.Models;
using Jackett.Common.Models.Config;
using Jackett.Common.Services.Cache;
using Jackett.Common.Utils;
using Jackett.Server.Controllers;
using Jackett.Test.TestHelpers;
using Microsoft.Data.Sqlite;
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
    public class CardigannIndexerHtmlWithSQLiteTests
    {
        private readonly TestWebClient _webClient = new TestWebClient();
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private IContainer _container;
        private ServerConfig _serverConfig;

        [SetUp]
        public void Setup()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<SQLiteCacheService>().AsSelf().SingleInstance();
            builder.RegisterType<CacheServiceFactory>().AsSelf().SingleInstance();
            builder.RegisterType<CacheManager>().AsSelf().SingleInstance();
            builder.RegisterType<ServerConfigurationController>().AsSelf().InstancePerDependency();
            builder.RegisterType<ServerConfig>().AsSelf().SingleInstance();

            var applicationFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            var testbase = Path.Combine(Path.Combine(Path.Combine(applicationFolder, "Resources"), "testhtml.db"));
            _serverConfig =
                new ServerConfig(new RuntimeSettings()) { CacheType = CacheType.SqLite, CacheConnectionString = testbase };
            builder.Register(ctx =>
            {
                var logger = _logger;
                return new SQLiteCacheService(logger, _serverConfig.CacheConnectionString, _serverConfig);
            }).AsSelf().SingleInstance();
            SqlMapper.RemoveTypeMap(typeof(DateTime));
            SqlMapper.RemoveTypeMap(typeof(DateTime?));
            SqlMapper.RemoveTypeMap(typeof(string));
            SqlMapper.AddTypeHandler(new NullableDateTimeHandler(_logger));
            SqlMapper.AddTypeHandler(new StringHandler(_logger));
            SqlMapper.AddTypeHandler(new UriHandler(_logger));
            SqlMapper.AddTypeHandler(new ICollectionIntHandler(_logger));
            SqlMapper.AddTypeHandler(new FloatHandler(_logger));
            SqlMapper.AddTypeHandler(new LongHandler(_logger));
            SqlMapper.AddTypeHandler(new DoubleHandler(_logger));
            SqlMapper.AddTypeHandler(new ICollectionStringHandler(_logger));
            SqlMapper.AddTypeHandler(new DateTimeHandler(_logger));
            _container = builder.Build();
        }

        [Test]
        public async Task TestCardigannHtmlWithSQLiteCacheAsync()
        {
            var cacheServiceFactory = _container.Resolve<CacheServiceFactory>();
            var cacheManager = new CacheManager(cacheServiceFactory, _serverConfig);
            DeleteTestTablesFromFile();

            _webClient.RegisterRequestCallback("https://www.testdefinition1.cc/search?query=ubuntu&sort=created", "html-response1.html");
            var definition = LoadTestDefinition("html-definition1.yml");

            var indexer = new CardigannIndexer(null, _webClient, _logger, null, cacheManager, definition);

            var query = new TorznabQuery
            {
                QueryType = "search",
                SearchTerm = "ubuntu",
            };

            var result = await indexer.ResultsForQuery(query, false);
            Assert.AreEqual(false, result.IsFromCache);

            result = await indexer.ResultsForQuery(query, false);
            Assert.AreEqual(true, result.IsFromCache);

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
            Assert.AreEqual(2024, firstRelease.PublishDate.Year);
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

        private void DeleteTestTablesFromFile()
        {
            try
            {
                var applicationFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
                var testbase = Path.Combine(Path.Combine(applicationFolder, "Resources"), "testhtml.db");

                using (var connection = new SqliteConnection("Data Source=" + testbase))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = @"
                                DELETE FROM TrackerCacheQueryReleaseInfos;
                                DELETE FROM TrackerCacheQueries;
                                DELETE FROM TrackerCaches;                                
                                DELETE FROM ReleaseInfos;";
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }
    }
}
