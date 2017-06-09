using Jackett.Models;
using NLog;
using Jackett.Services;
using Jackett.Utils.Clients;
using Jackett.Indexers.Abstract;

namespace Jackett.Indexers
{
    public class HDForever : GazelleTracker, IIndexer
    {
        public HDForever(IIndexerManagerService indexerManager, IWebClient webClient, Logger logger, IProtectionService protectionService)
            : base(name: "HD-Forever",
                desc: null,
                link: "https://hdf.world/",
                indexerManager: indexerManager,
                logger: logger,
                protectionService: protectionService,
                webClient: webClient
                )
        {
            Language = "fr-fr";
            Type = "private";

            AddCategoryMapping(1, TorznabCatType.MoviesHD, "Movies/HD");
        }
    }
}