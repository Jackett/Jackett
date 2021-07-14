using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Jackett.Common.Indexers.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using NLog;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class RetroFlix : SpeedAppTracker
    {
        protected override string ItemsPerPage => "90";
        protected override bool UseP2PReleaseName => true;

        public override string[] LegacySiteLinks { get; protected set; } = {
            "https://retroflix.net/"
        };

        public RetroFlix(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(
                id: "retroflix",
                name: "RetroFlix",
                description: "Private Torrent Tracker for Classic Movies / TV / General Releases",
                link: "https://retroflix.club/",
                caps: new TorznabCapabilities
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
                },
                configService: configService,
                client: wc,
                logger: l,
                p: ps,
                cs: cs)
        {
            Encoding = Encoding.UTF8;
            Language = "en-US";
            Type = "private";

            // requestDelay for API Limit (1 request per 2 seconds)
            webclient.requestDelay = 2.1;

            AddCategoryMapping(401, TorznabCatType.Movies, "Movies");
            AddCategoryMapping(402, TorznabCatType.TV, "TV Series");
            AddCategoryMapping(406, TorznabCatType.AudioVideo, "Music Videos");
            AddCategoryMapping(407, TorznabCatType.TVSport, "Sports");
            AddCategoryMapping(409, TorznabCatType.Books, "Books");
            AddCategoryMapping(408, TorznabCatType.Audio, "HQ Audio");
        }
    }
}
