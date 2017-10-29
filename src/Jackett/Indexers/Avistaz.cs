using Jackett.Services.Interfaces;
using Jackett.Utils.Clients;
using NLog;

namespace Jackett.Indexers
{
    public class Avistaz : AvistazTracker
    {
        public Avistaz(IIndexerConfigurationService configService, IWebClient webClient, Logger logger, IProtectionService protectionService)
            : base(name: "Avistaz",
                desc: "Aka AsiaTorrents",
                link: "https://avistaz.to/",
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