using Jackett.Common.Indexers.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;
using System;

namespace Jackett.Common.Indexers
{
    public class Psytorrents : GazelleTracker
    {
        private static readonly string[] certificateHashs = new string[] {
            "8375026D07C1EAE2DCE7157ABF255357366AE341", // expired
        };

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

            foreach (var certificateHash in certificateHashs)
                webclient.AddTrustedCertificate(new Uri(SiteLink).Host, certificateHash);
        }
    }
}
