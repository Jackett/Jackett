using System.Collections.Generic;
using Jackett.Indexers.Abstract;
using Jackett.Models;
using Jackett.Services.Interfaces;
using Jackett.Utils.Clients;
using NLog;

namespace Jackett.Indexers
{
    public class Apollo : GazelleTracker
    {
        public Apollo(IIndexerConfigurationService configService, WebClient webClient, Logger logger, IProtectionService protectionService)
            : base(name: "Apollo",
                desc: "A music tracker",
                link: "https://apollo.rip/",
                configService: configService,
                logger: logger,
                protectionService: protectionService,
                webClient: webClient
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