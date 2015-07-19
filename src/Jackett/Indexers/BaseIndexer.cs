using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jackett.Models;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Indexers
{
    public abstract class BaseIndexer: IndexerInterface
    {
        public string DisplayDescription { get; }
        public string DisplayName { get; }
        public bool IsConfigured { get; protected set; }
        public Uri SiteLink { get; }

        public abstract Task ApplyConfiguration(JToken configJson);
        public abstract Task<byte[]> Download(Uri link);
        public abstract Task<ConfigurationData> GetConfigurationForSetup();
        public abstract void LoadFromSavedConfiguration(JToken jsonConfig);
        public abstract Task<ReleaseInfo[]> PerformQuery(TorznabQuery query);

        private Logger logger;

        public BaseIndexer(string name, string description, Uri link, Logger logger)
        {
            DisplayName = name;
            DisplayDescription = description;
            SiteLink = link;
            this.logger = logger;
        }

        protected void LogParseError(string results, Exception ex)
        {
            var fileName = string.Format("Error on {0} for {1}.txt", DateTime.Now.ToString("yyyyMMddHHmmss"), DisplayName);
            var spacing = string.Join("", Enumerable.Repeat(Environment.NewLine, 5));
            var fileContents = string.Format("{0}{1}{2}", ex, spacing, results);
            logger.Error(fileName + fileContents);
        }
    }
}
