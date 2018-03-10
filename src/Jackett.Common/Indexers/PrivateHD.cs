using Jackett.Common.Indexers.Abstract;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;

namespace Jackett.Common.Indexers
{
    public class PrivateHD : AvistazTracker, IIndexer
    {
        public PrivateHD(IIndexerConfigurationService configService, WebClient webClient, Logger logger, IProtectionService protectionService)
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
