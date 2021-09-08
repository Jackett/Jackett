using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Jackett.Common.Indexers.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class DesiTorrents : GazelleTracker
    {
        protected override string PosterUrl => SiteLink + "static/media/posters/";
        public DesiTorrents(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "desitorrents",
                   name: "DesiTorrents",
                   description: "Desitorrents is a  Private Torrent Tracker for BOLLYWOOD / TOLLYWOOD / GENERAL",
                   link: "https://desitorrents.tv/",
                   caps: new TorznabCapabilities
                   {
                       TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                       },
                       MovieSearchParams = new List<MovieSearchParam>
                       {
                           MovieSearchParam.Q
                       },
                       MusicSearchParams = new List<MusicSearchParam>
                       {
                           MusicSearchParam.Q
                       },
                       BookSearchParams = new List<BookSearchParam>
                       {
                           BookSearchParam.Q
                       }
                   },
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cs: cs,
                   supportsFreeleechTokens: false,
                   has2Fa: true
                )
        {
            Language = "en-US";
            Type = "private";

            AddCategoryMapping(1, TorznabCatType.Movies, "Movies");
            AddCategoryMapping(2, TorznabCatType.TV, "Tv shows");
            AddCategoryMapping(3, TorznabCatType.Audio, "Music");
            AddCategoryMapping(4, TorznabCatType.BooksEBook, "ebooks");
            AddCategoryMapping(5, TorznabCatType.TVSport, "Sports");
            AddCategoryMapping(6, TorznabCatType.PCGames, "Games");
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = await base.PerformQuery(query);
            foreach (var release in releases)
            {
                release.MinimumRatio = 0.6;
                release.MinimumSeedTime = 259200;
            }
            return releases;
        }

        protected override bool ReleaseInfoPostParse(ReleaseInfo release, JObject torrent, JObject result)
        {
            // Add missing category information
            var category = (string)result["category"];
            release.Category = MapTrackerCatToNewznab(category);
            return true;
        }
    }
}
