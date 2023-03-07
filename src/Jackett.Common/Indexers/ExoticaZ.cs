using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Jackett.Common.Indexers.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class ExoticaZ : AvistazTracker
    {
        private new ConfigurationDataAvistazTracker configData => (ConfigurationDataAvistazTracker)base.configData;

        public override string[] LegacySiteLinks { get; protected set; } =
        {
            "https://torrents.yourexotic.com/"
        };

        public ExoticaZ(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "exoticaz",
                   name: "ExoticaZ",
                   description: "ExoticaZ (YourExotic) is a Private Torrent Tracker for 3X",
                   link: "https://exoticaz.to/",
                   caps: new TorznabCapabilities(),
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cs: cs
                   )
        {
            AddCategoryMapping(1, TorznabCatType.XXXx264, "Video Clip");
            AddCategoryMapping(2, TorznabCatType.XXXPack, "Video Pack");
            AddCategoryMapping(3, TorznabCatType.XXXPack, "Siterip Pack");
            AddCategoryMapping(4, TorznabCatType.XXXPack, "Pornstar Pack");
            AddCategoryMapping(5, TorznabCatType.XXXDVD, "DVD");
            AddCategoryMapping(6, TorznabCatType.XXXx264, "BluRay");
            AddCategoryMapping(7, TorznabCatType.XXXImageSet, "Photo Pack");
            AddCategoryMapping(8, TorznabCatType.XXXImageSet, "Books & Magazines");
        }

        protected override List<KeyValuePair<string, string>> GetSearchQueryParameters(TorznabQuery query)
        {
            var categoryMapping = MapTorznabCapsToTrackers(query).Distinct().ToList();
            var qc = new List<KeyValuePair<string, string>> // NameValueCollection don't support cat[]=19&cat[]=6
            {
                { "in", "1" },
                { "category", categoryMapping.FirstIfSingleOrDefault("0") },
                { "limit", "50" },
                { "search", GetSearchTerm(query).Trim() }
            };

            if (query.Limit > 0 && query.Offset > 0)
            {
                var page = query.Offset / query.Limit + 1;
                qc.Add("page", page.ToString());
            }

            if (configData.Freeleech.Value)
                qc.Add("discount[]", "1");

            return qc;
        }

        protected override List<int> ParseCategories(TorznabQuery query, JToken row)
        {
            var cat = row.Value<JObject>("category").Properties().First().Name;
            return MapTrackerCatToNewznab(cat).ToList();
        }
    }
}
