using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Jackett.Common.Indexers.Definitions.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class DICMusic : GazelleTracker
    {
        public override string Id => "dicmusic";
        public override string Name => "DICMusic";
        public override string Description => "DICMusic is a CHINESE Private Torrent Tracker for MUSIC";
        public override string SiteLink { get; protected set; } = "https://dicmusic.com/";
        public override string[] LegacySiteLinks => new[] { "https://dicmusic.club/" };
        public override string Language => "zh-CN";
        public override string Type => "private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        public DICMusic(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
                        ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cs: cs,
                   supportsFreeleechTokens: true,
                   supportsFreeleechOnly: true,
                   has2Fa: true)
        {
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                MusicSearchParams = new List<MusicSearchParam>
                {
                    MusicSearchParam.Q, MusicSearchParam.Album, MusicSearchParam.Artist, MusicSearchParam.Label, MusicSearchParam.Year
                }
            };

            caps.Categories.AddCategoryMapping(1, TorznabCatType.Audio, "Music");
            caps.Categories.AddCategoryMapping(2, TorznabCatType.PC, "Applications");

            return caps;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var results = await base.PerformQuery(query);
            // results must contain search terms
            results = results.Where(release => query.MatchQueryStringAND(release.Title));
            return results;
        }
    }
}
