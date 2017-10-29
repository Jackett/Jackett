using Jackett.Services.Interfaces;
using Jackett.Utils.Clients;
using NLog;

namespace Jackett.Indexers
{
    public class PrivateHD : AvistazTracker, IIndexer
    {
        public PrivateHD(IIndexerConfigurationService configService, IWebClient webClient, Logger logger, IProtectionService protectionService)
            : base(name: "PrivateHD",
                desc: "BitTorrent site for High Quality, High Definition (HD) movies and TV Shows",
                link: "https://privatehd.to/",
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
