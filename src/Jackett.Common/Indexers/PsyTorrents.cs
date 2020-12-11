using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Jackett.Common.Indexers.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class PsyTorrents : GazelleTracker
    {
        public PsyTorrents(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "psytorrents",
                   name: "Psytorrents",
                   description: "Psytorrents (PSY) is a Private Torrent Tracker for ELECTRONIC MUSIC",
                   link: "https://psytorrents.info/",
                   caps: new TorznabCapabilities
                   {
                       MovieSearchParams = new List<MovieSearchParam>
                       {
                           MovieSearchParam.Q
                       },
                       MusicSearchParams = new List<MusicSearchParam>
                       {
                           MusicSearchParam.Q
                       }
                   },
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cs: cs,
                   supportsFreeleechTokens: true)
        {
            Language = "en-us";
            Type = "private";

            webclient.AddTrustedCertificate(new Uri(SiteLink).Host, "B52C043ABDE7AFB2231E162B1DD468758AEEE307");
            webclient.AddTrustedCertificate(new Uri(SiteLink).Host, "AAA3E062739F3733FE659BA4A89E55E4EB48063B");

            AddCategoryMapping(1, TorznabCatType.Audio, "Music");
            AddCategoryMapping(2, TorznabCatType.Movies, "Movies");
            AddCategoryMapping(3, TorznabCatType.PC0day, "App");
        }
    }
}
