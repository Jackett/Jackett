using Jackett.Services;
using Jackett.Utils.Clients;
using NLog;

namespace Jackett.Indexers
{
    public class CinemaZ : AvistazTracker
    {
        public CinemaZ(IIndexerConfigurationService configService, IWebClient webClient, Logger logger, IProtectionService protectionService)
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