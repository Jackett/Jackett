using Jackett.Common.Indexers.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;

namespace Jackett.Common.Indexers
{
    public class HDForever : GazelleTracker
    {
        public HDForever(IIndexerConfigurationService configService, WebClient webClient, Logger logger, IProtectionService protectionService)
            : base(name: "HD-Forever",
                desc: "HD-Forever (HD-F) is a FRENCH Private Torrent Tracker for HD MOVIES",
                link: "https://hdf.world/",
                configService: configService,
                logger: logger,
                protectionService: protectionService,
                webClient: webClient,
                supportsFreeleechTokens: true
                )
        {
            Language = "fr-fr";
            Type = "private";

            AddCategoryMapping(1, TorznabCatType.MoviesHD, "Movies/HD");
        }
    }
}
