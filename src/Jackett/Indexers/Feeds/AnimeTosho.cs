using System;
using System.Text;
using Jackett.Models;
using Jackett.Models.IndexerConfig;
using Jackett.Services;
using Jackett.Utils.Clients;
using NLog;

namespace Jackett.Indexers.Newznab
{
    public class AnimeTosho : BaseNewznabIndexer
    {
        public AnimeTosho(IIndexerConfigurationService configService, IWebClient client, Logger logger, IProtectionService p) : base("Anime Tosho", "https://animetosho.org/", "", configService, client, logger, new ConfigurationData(), p)
        {
            // TODO
            // this might be downloaded and refreshed instead of hard-coding it
            TorznabCaps = new TorznabCapabilities
            {
                SearchAvailable = true,
                TVSearchAvailable = false,
                MovieSearchAvailable = false,
                SupportsImdbSearch = false,
                SupportsTVRageSearch = false
            };

            Encoding = Encoding.UTF8;
            Language = "en-en";
            Type = "public";
        }

        protected override Uri FeedUri => new Uri("https://animetosho.org/feed/api");
    }
}
