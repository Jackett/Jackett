using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Jackett.Common.Indexers.Definitions.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class ExoticaZ : AvistazTracker
    {
        public override string Id => "exoticaz";
        public override string[] Replaces => new[] { "yourexotic" };
        public override string Name => "ExoticaZ";
        public override string Description => "ExoticaZ (YourExotic) is a Private Torrent Tracker for 3X";
        public override string SiteLink { get; protected set; } = "https://exoticaz.to/";
        public override string[] LegacySiteLinks => new[]
        {
            "https://torrents.yourexotic.com/"
        };

        protected override string TimezoneOffset => "+02:00";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private new ConfigurationDataAvistazTracker configData => (ConfigurationDataAvistazTracker)base.configData;

        public ExoticaZ(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cs: cs)
        {
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                LimitsDefault = 50,
                LimitsMax = 50
            };

            caps.Categories.AddCategoryMapping(1, TorznabCatType.XXXx264, "Video Clip");
            caps.Categories.AddCategoryMapping(2, TorznabCatType.XXXPack, "Video Pack");
            caps.Categories.AddCategoryMapping(3, TorznabCatType.XXXPack, "Siterip Pack");
            caps.Categories.AddCategoryMapping(4, TorznabCatType.XXXPack, "Pornstar Pack");
            caps.Categories.AddCategoryMapping(5, TorznabCatType.XXXDVD, "DVD");
            caps.Categories.AddCategoryMapping(6, TorznabCatType.XXXx264, "BluRay");
            caps.Categories.AddCategoryMapping(7, TorznabCatType.XXXImageSet, "Photo Pack");
            caps.Categories.AddCategoryMapping(8, TorznabCatType.XXXImageSet, "Books & Magazines");

            return caps;
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
