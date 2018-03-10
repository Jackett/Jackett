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
        public SecretCinema(IIndexerConfigurationService configService, WebClient webClient, Logger logger, IProtectionService protectionService)
            : base(name: "Secret Cinema",
                desc: "A tracker for rare movies.",
                link: "https://secret-cinema.pw/",
                configService: configService,
                logger: logger,
                protectionService: protectionService,
                webClient: webClient,
                supportsFreeleechTokens: false // ratio free tracker
                )
        {
            Language = "en-us";
            Type = "private";
            TorznabCaps.SupportedMusicSearchParamsList = new List<string>() { "q", "album", "artist", "label", "year" };
            TorznabCaps.SupportsImdbSearch = true;

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