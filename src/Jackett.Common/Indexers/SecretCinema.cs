using System.Collections.Generic;
using Jackett.Common.Indexers.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    public class SecretCinema : GazelleTracker
    {
        public SecretCinema(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base("Secret Cinema",
                   description: "A tracker for rare movies.",
                   link: "https://secret-cinema.pw/",
                   caps: new TorznabCapabilities
                   {
                       SupportsImdbMovieSearch = true,
                       SupportedMusicSearchParamsList = new List<string> { "q", "album", "artist", "label", "year" }
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
            AddCategoryMapping(3, TorznabCatType.Books, "E-Books");
        }

        protected override bool ReleaseInfoPostParse(ReleaseInfo release, JObject torrent, JObject result)
        {
            var media = (string)torrent["media"];
            if (!string.IsNullOrEmpty(media))
            {
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
            }
            return true;
        }
    }
}
