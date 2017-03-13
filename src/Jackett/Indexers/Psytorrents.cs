using Jackett.Models;
using NLog;
using Jackett.Services;
using Jackett.Utils.Clients;
using Jackett.Indexers.Abstract;

namespace Jackett.Indexers
{
    public class Psytorrents : GazelleTracker, IIndexer
    {
        public Psytorrents(IIndexerManagerService indexerManager, IWebClient webClient, Logger logger, IProtectionService protectionService)
            : base(name: "Psytorrents",
                desc: null,
                link: "https://psytorrents.info/",
                indexerManager: indexerManager,
                logger: logger,
                protectionService: protectionService,
                webClient: webClient
                )
        {
            Language = "en-us";
            Type = "private";

            AddCategoryMapping(1, TorznabCatType.Audio, "Music");
            AddCategoryMapping(2, TorznabCatType.Movies, "Movies");
            AddCategoryMapping(3, TorznabCatType.PC0day, "App");
        }
    }
}