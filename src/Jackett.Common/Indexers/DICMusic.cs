using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Jackett.Common.Indexers.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class DICMusic : GazelleTracker
    {
        public DICMusic(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(id: "dicmusic",
                   name: "DICMusic",
                   description: "DICMusic is a CHINESE Private Torrent Tracker for MUSIC",
                   link: "https://dicmusic.club/",
                   caps: new TorznabCapabilities
                   {
                       MusicSearchParams = new List<MusicSearchParam>
                       {
                           MusicSearchParam.Q, MusicSearchParam.Album, MusicSearchParam.Artist, MusicSearchParam.Label, MusicSearchParam.Year
                       }
                   },
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   supportsFreeleechTokens: true,
                   has2Fa: true)
        {
            Language = "zh-cn";
            Type = "private";

            AddCategoryMapping(1, TorznabCatType.Audio, "Music");
            AddCategoryMapping(2, TorznabCatType.PC, "Applications");
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
