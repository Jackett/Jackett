using Jackett.Common.Indexers.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;

namespace Jackett.Common.Indexers
{
    public class Psytorrents : GazelleTracker
    {
        public Psytorrents(IIndexerConfigurationService configService, WebClient webClient, Logger logger, IProtectionService protectionService)
            : base(name: "Psytorrents",
                desc: "Psytorrents (PSY) is a Private Torrent Tracker for ELECTRONIC MUSIC",
                link: "https://psytorrents.info/",
                configService: configService,
                logger: logger,
                protectionService: protectionService,
                webClient: webClient,
                supportsFreeleechTokens: true
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
