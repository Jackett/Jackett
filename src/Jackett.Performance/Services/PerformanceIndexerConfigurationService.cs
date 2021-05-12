using System;
using Jackett.Common.Indexers;
using Jackett.Common.Services.Interfaces;
using Newtonsoft.Json.Linq;

namespace Jackett.Performance.Services
{
    public class PerformanceIndexerConfigurationService : IIndexerConfigurationService
    {
        private IProtectionService protectionService;

        public PerformanceIndexerConfigurationService(IProtectionService protectionService)
        {
            this.protectionService = protectionService;
        }

        public void Load(IIndexer indexer)
        {
            if (indexer.Type != "public")
                return;

            var configData = indexer.GetConfigurationForSetup().GetAwaiter().GetResult();
            indexer.LoadFromSavedConfiguration(configData.ToJson(protectionService));
        }

        public void Delete(IIndexer indexer) => throw new NotImplementedException();

        public void RenameIndexer(string oldId, string newId) {}

        public string GetIndexerConfigFilePath(string indexerId) => throw new NotImplementedException();

        public void Save(IIndexer indexer, JToken config) { }
    }
}
