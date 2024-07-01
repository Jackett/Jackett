using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Jackett.Common.Indexers.Definitions.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class SecretCinema : GazelleTracker
    {
        public override string Id => "secretcinema";
        public override string Name => "Secret Cinema";
        public override string Description => "A tracker for rare movies.";
        public override string SiteLink { get; protected set; } = "https://secret-cinema.pw/";
        public override string Language => "en-US";
        public override string Type => "private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        public SecretCinema(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
                            ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cs: cs,
                   supportsFreeleechTokens: false) // ratioless tracker
        {
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q, MovieSearchParam.ImdbId
                },
                MusicSearchParams = new List<MusicSearchParam>
                {
                    MusicSearchParam.Q, MusicSearchParam.Album, MusicSearchParam.Artist, MusicSearchParam.Label, MusicSearchParam.Year
                }
            };

            caps.Categories.AddCategoryMapping(1, TorznabCatType.Movies, "Movies");
            caps.Categories.AddCategoryMapping(2, TorznabCatType.Audio, "Music");
            // cat=3 exists but it's required a refactor in Gazelle abstract to make it work
            //caps.Categories.AddCategoryMapping(3, TorznabCatType.Books, "E-Books");

            return caps;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var results = await base.PerformQuery(query);
            // results must contain search terms
            results = results.Where(release => query.MatchQueryStringAND(release.Title));
            foreach (var release in results)
            {
                // SecretCinema loads artist with the movie director and the gazelleTracker abstract
                // places it in front of the movie separated with a dash.
                // We need to strip it or Radarr will not get a title match for automatic DL
                var artistEndsAt = release.Title.IndexOf(" - ");
                if (artistEndsAt > -1)
                {
                    release.Title = release.Title.Substring(artistEndsAt + 3);
                }
            }
            return results;
        }

        protected override bool ReleaseInfoPostParse(ReleaseInfo release, JObject torrent, JObject result)
        {
            var media = (string)torrent["media"];
            if (string.IsNullOrEmpty(media))
                return true;
            switch (media)
            {
                case "SD":
                    release.Category.Remove(TorznabCatType.Movies.ID);
                    release.Category.Add(TorznabCatType.MoviesSD.ID);
                    break;
                case "720p":
                case "1080p":
                case "4k": // not verified
                    release.Category.Remove(TorznabCatType.Movies.ID);
                    release.Category.Add(TorznabCatType.MoviesHD.ID);
                    break;
                case "DVD-R":
                    release.Category.Remove(TorznabCatType.Movies.ID);
                    release.Category.Add(TorznabCatType.MoviesDVD.ID);
                    break;
                case "BDMV":
                    release.Category.Remove(TorznabCatType.Movies.ID);
                    release.Category.Add(TorznabCatType.MoviesBluRay.ID);
                    break;
            }
            return true;
        }
    }
}
