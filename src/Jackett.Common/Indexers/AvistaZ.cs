using System.Diagnostics.CodeAnalysis;
using Jackett.Common.Indexers.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class AvistaZ : AvistazTracker
    {
        public AvistaZ(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(id: "avistaz",
                   name: "AvistaZ",
                   description: "Aka AsiaTorrents",
                   link: "https://avistaz.to/",
                   caps: new TorznabCapabilities
                   {
                       SupportsImdbMovieSearch = true
                       // SupportsImdbTVSearch = true (supported by the site but disabled due to #8107)
                   },
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps)
            => Type = "private";

        // Avistaz has episodes without season. eg Running Man E323
        protected override string GetSearchTerm(TorznabQuery query) =>
            !string.IsNullOrWhiteSpace(query.Episode) && query.Season == 0 ?
            $"{query.SearchTerm} E{query.Episode}" :
            $"{query.SearchTerm} {query.GetEpisodeSearchString()}";
    }
}
