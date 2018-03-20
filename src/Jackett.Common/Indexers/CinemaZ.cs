using Jackett.Common.Indexers.Abstract;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;

namespace Jackett.Common.Indexers
{
    public class CinemaZ : AvistazTracker
    {
        public CinemaZ(IIndexerConfigurationService configService, WebClient webClient, Logger logger, IProtectionService protectionService)
            : base(name: "CinemaZ",
                desc: "Part of the Avistaz network.",
                link: "https://cinemaz.to/",
                configService: configService,
                logger: logger,
                protectionService: protectionService,
                webClient: webClient
                )
        {
            Type = "private";
        }
    }
}