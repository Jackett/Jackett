using System.Collections.Generic;
using Jackett.Common.Indexers.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;

namespace Jackett.Common.Indexers
{
    public class notwhatcd : GazelleTracker
    {
        public notwhatcd(IIndexerConfigurationService configService, WebClient webClient, Logger logger, IProtectionService protectionService)
            : base(name: "notwhat.cd",
                desc: "A music tracker",
                link: "https://notwhat.cd/",
                configService: configService,
                logger: logger,
                protectionService: protectionService,
                webClient: webClient,
                supportsFreeleechTokens: true
                )
        {
            Language = "en-us";
            Type = "private";
            TorznabCaps.SupportedMusicSearchParamsList = new List<string>() { "q", "album", "artist", "label", "year" };

            AddCategoryMapping(1, TorznabCatType.Audio, "Music");
            AddCategoryMapping(2, TorznabCatType.PC, "Applications");
            AddCategoryMapping(3, TorznabCatType.Books, "E-Books");
            AddCategoryMapping(4, TorznabCatType.AudioAudiobook, "Audiobooks");
            AddCategoryMapping(5, TorznabCatType.Movies, "E-Learning Videos");
            AddCategoryMapping(6, TorznabCatType.TV, "Comedy");
            AddCategoryMapping(7, TorznabCatType.Books, "Comics");
        }
    }
}