using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
    public class SecretCinema : GazelleTracker
    {
        public SecretCinema(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(id: "secretcinema",
                   name: "Secret Cinema",
                   description: "A tracker for rare movies.",
                   link: "https://secret-cinema.pw/",
                   caps: new TorznabCapabilities
                   {
                       MovieSearchParams = new List<MovieSearchParam>
                       {
                           MovieSearchParam.Q, MovieSearchParam.ImdbId
                       },
                       MusicSearchParams = new List<MusicSearchParam>
                       {
                           MusicSearchParam.Q, MusicSearchParam.Album, MusicSearchParam.Artist, MusicSearchParam.Label, MusicSearchParam.Year
                       }
                   },
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   supportsFreeleechTokens: false) // ratioless tracker
        {
            Language = "en-us";
            Type = "private";

            AddCategoryMapping(1, TorznabCatType.Movies, "Movies");
            AddCategoryMapping(2, TorznabCatType.Audio, "Music");
            // cat=3 exists but it's required a refactor in Gazelle abstract to make it work
            //AddCategoryMapping(3, TorznabCatType.Books, "E-Books");
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var results = await base.PerformQuery(query);
            // results must contain search terms
            results = results.Where(release => query.MatchQueryStringAND(release.Title));
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
