using System.Diagnostics.CodeAnalysis;
using Jackett.Common.Indexers.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class PrivateHD : AvistazTracker
    {
        public PrivateHD(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base("PrivateHD",
                   description: "BitTorrent site for High Quality, High Definition (HD) movies and TV Shows",
                   link: "https://privatehd.to/",
                   caps: new TorznabCapabilities
                   {
                       SupportsImdbMovieSearch = true
                   },
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps)
            => Type = "private";
    }
}
