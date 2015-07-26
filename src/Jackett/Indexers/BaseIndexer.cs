using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jackett.Models;
using Newtonsoft.Json.Linq;
using NLog;
using Jackett.Services;
using Jackett.Utils;

namespace Jackett.Indexers
{
    public abstract class BaseIndexer
    {
        public string DisplayDescription { get; private set; }
        public string DisplayName { get; private set; }
        public string ID { get { return GetIndexerID(GetType()); } }

        public bool IsConfigured { get; protected set; }
        public Uri SiteLink { get; private set; }

        public TorznabCapabilities TorznabCaps { get; private set; }

        protected Logger logger;
        protected IIndexerManagerService indexerService;

        protected static List<CachedQueryResult> cache = new List<CachedQueryResult>();
        protected static readonly TimeSpan cacheTime = new TimeSpan(0, 9, 0);

        public static string GetIndexerID(Type type)
        {
            return StringUtil.StripNonAlphaNumeric(type.Name.ToLowerInvariant());
        }

        public BaseIndexer(string name, string description, Uri link, TorznabCapabilities caps, IIndexerManagerService manager,Logger logger)
        {
            DisplayName = name;
            DisplayDescription = description;
            SiteLink = link;
            TorznabCaps = caps;
            this.logger = logger;
            indexerService = manager;
        }

        protected void SaveConfig(JToken config)
        {
            indexerService.SaveConfig(this as IIndexer, config);
        }

        protected void OnParseError(string results, Exception ex)
        {
            var fileName = string.Format("Error on {0} for {1}.txt", DateTime.Now.ToString("yyyyMMddHHmmss"), DisplayName);
            var spacing = string.Join("", Enumerable.Repeat(Environment.NewLine, 5));
            var fileContents = string.Format("{0}{1}{2}", ex, spacing, results);
            logger.Error(fileName + fileContents);
            throw ex;
        }

        protected void CleanCache()
        {
            foreach (var expired in cache.Where(i => i.Created - DateTime.Now > cacheTime).ToList())
            {
                cache.Remove(expired);
            }
        }
    }
}
