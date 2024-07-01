using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Jackett.Common.Indexers.Definitions.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class RetroFlix : SpeedAppTracker
    {
        public override string Id => "retroflix";
        public override string Name => "RetroFlix";
        public override string Description => "Private Torrent Tracker for Classic Movies / TV / General Releases";
        public override string SiteLink { get; protected set; } = "https://retroflix.club/";
        public override string[] LegacySiteLinks => new[]
        {
            "https://retroflix.net/"
        };
        public override string Language => "en-US";
        public override string Type => "private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        protected override int MinimumSeedTime => 432000; // 120h

        public RetroFlix(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cs: cs)
        {
            // requestDelay for API Limit (1 request per 2 seconds)
            webclient.requestDelay = 2.1;
            configData.AddDynamic("Account Inactivity", new DisplayInfoConfigurationItem("Account Inactivity", "Any account not logged into after 150 days will be deleted. Parked accounts will be deleted after 150 days of inactivity."));
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId
                },
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q, MovieSearchParam.ImdbId
                },
                MusicSearchParams = new List<MusicSearchParam>
                {
                    MusicSearchParam.Q
                },
                BookSearchParams = new List<BookSearchParam>
                {
                    BookSearchParam.Q
                }
            };

            caps.Categories.AddCategoryMapping(401, TorznabCatType.Movies, "Movies");
            caps.Categories.AddCategoryMapping(402, TorznabCatType.TV, "TV Series");
            caps.Categories.AddCategoryMapping(406, TorznabCatType.AudioVideo, "Music Videos");
            caps.Categories.AddCategoryMapping(407, TorznabCatType.TVSport, "Sports");
            caps.Categories.AddCategoryMapping(409, TorznabCatType.Books, "Books");
            caps.Categories.AddCategoryMapping(408, TorznabCatType.Audio, "HQ Audio");

            return caps;
        }
    }
}
