using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Jackett.Common.Indexers;
using Jackett.Common.Services.Interfaces;
using Newtonsoft.Json.Linq;

namespace Jackett.Test.TestHelpers
{
    internal class TestIndexerManagerServiceHelper : IIndexerManagerService
    {
        public JToken LastSavedConfig { get; set; }

        public void DeleteIndexer(string name) => throw new NotImplementedException();

        public IEnumerable<IIndexer> GetAllIndexers() => throw new NotImplementedException();

        public IIndexer GetIndexer(string name) => throw new NotImplementedException();

        public IWebIndexer GetWebIndexer(string name) => throw new NotImplementedException();

        public void InitIndexers(IEnumerable<string> path) => throw new NotImplementedException();

        public void SaveConfig(IIndexer indexer, JToken obj) => LastSavedConfig = obj;

        public Task TestIndexer(string name) => throw new NotImplementedException();

        public void InitMetaIndexers() => throw new NotImplementedException();
    }
}
