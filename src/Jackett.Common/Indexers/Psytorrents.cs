using System;
using Jackett.Common.Indexers.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;

namespace Jackett.Common.Indexers
{
    public class Psytorrents : GazelleTracker
    {
        public Psytorrents(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base("Psytorrents",
                   description: "Psytorrents (PSY) is a Private Torrent Tracker for ELECTRONIC MUSIC",
                   link: "https://psytorrents.info/",
                   caps: new TorznabCapabilities(),
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   supportsFreeleechTokens: true)
        {
            Language = "en-us";
            Type = "private";

            webclient.AddTrustedCertificate(new Uri(SiteLink).Host, "B52C043ABDE7AFB2231E162B1DD468758AEEE307");

            AddCategoryMapping(1, TorznabCatType.Audio, "Music");
            AddCategoryMapping(2, TorznabCatType.Movies, "Movies");
            AddCategoryMapping(3, TorznabCatType.PC0day, "App");
        }
    }
}
