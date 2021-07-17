using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using BenchmarkDotNet.Attributes;
using Jackett.Common.Indexers;
using Jackett.Common.Indexers.Meta;
using Jackett.Common.Models;
using Jackett.Common.Models.Config;
using Jackett.Common.Services;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using Jackett.Performance.Services;
using Jackett.Performance.Utils.Clients;
using Jackett.Server.Services;

using NLog;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Jackett.Performance
{
    [ShortRunJob]
    [MemoryDiagnoser]
    public class IndexerBenchmarks
    {
        private IndexerManager indexerManager = new IndexerManager();
        public IndexerBenchmarks()
        {
            var applicationFolder = Path.GetDirectoryName(typeof(IndexerBenchmarks).Assembly.Location);
            indexerManager.Init();
            var indexers = indexerManager.GetIndexers();
            Indexers = indexers.ToArray();
        }

        public IIndexer[] Indexers { get; }

        [GlobalSetup]
        [ArgumentsSource(nameof(Indexers))]
        public Task InitAsync(IIndexer indexer)
        {
            return indexerManager.SetupAsync(indexer);
        }

        [Benchmark(Description = "TestIndexer")]
        [ArgumentsSource(nameof(Indexers))]
        public async Task<IndexerResult> TestAsync(IIndexer indexer)
        {
            var query = new TorznabQuery { QueryType = "search", SearchTerm = "", IsTest = true };
            return await indexer.ResultsForQuery(query);
        }
    }
}
