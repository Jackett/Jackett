using System.Collections.Generic;
using Jackett.Common.Indexers.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    public class HDOnly : GazelleTracker
    {
        public HDOnly(IIndexerConfigurationService configService, WebClient webClient, Logger logger, IProtectionService protectionService)
            : base(name: "HD-Only",
                desc: "HD-Only (HD-O) is a FRENCH Private Torrent Tracker for HD MOVIES / TV",
                link: "https://hd-only.org/",
                configService: configService,
                logger: logger,
                protectionService: protectionService,
                webClient: webClient,
                supportsFreeleechTokens: true // not verified
                )
        {
            Language = "fr-fr";
            Type = "private";

            // a few releases have "category":"S\u00e9ries" set
            AddCategoryMapping(null, TorznabCatType.TV, "Séries");

            // releaseType mappings
            AddCategoryMapping(1, TorznabCatType.Movies, "Film");
            AddCategoryMapping(3, TorznabCatType.TVAnime, "Dessin animé");
            AddCategoryMapping(5, TorznabCatType.TV, "Série");
            AddCategoryMapping(6, TorznabCatType.TVAnime, "Série Animée");
            AddCategoryMapping(7, TorznabCatType.MoviesOther, "Film d'animation");
            AddCategoryMapping(9, TorznabCatType.AudioVideo, "Concert");
            AddCategoryMapping(11, TorznabCatType.TVDocumentary, "Documentaire");
            AddCategoryMapping(13, TorznabCatType.MoviesOther, "Court-métrage");
            AddCategoryMapping(14, TorznabCatType.MoviesOther, "Clip");
            AddCategoryMapping(15, TorznabCatType.MoviesOther, "Démonstration");
            AddCategoryMapping(16, TorznabCatType.MoviesOther, "Bonus de BD");
            AddCategoryMapping(21, TorznabCatType.Other, "Autre");
        }

        protected override bool ReleaseInfoPostParse(ReleaseInfo release, JObject torrent, JObject result)
        {
            // releaseType is used for categories
            var category = (string)result["category"];
            if (category == null)
            {
                var releaseType = (string)result["releaseType"];
                release.Category = MapTrackerCatDescToNewznab(releaseType);
            }
            return true;
        }

        protected override List<string> MapTorznabCapsToTrackers(TorznabQuery query, bool mapChildrenCatsToParent = false)
        {
            // don't use category filtering
            return new List<string>();
        }
    }
}
