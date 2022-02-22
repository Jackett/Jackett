using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Jackett.Common.Indexers.Abstract;
using Jackett.Common.Models;
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
                   caps: new TorznabCapabilities
                   {
                       MovieSearchParams = new List<MovieSearchParam>
                       {
                           MovieSearchParam.Q
                       }
                   },
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cs: cs
                   )
        {
            AddCategoryMapping(1, TorznabCatType.XXXx264);
            AddCategoryMapping(2, TorznabCatType.XXXPack);
            AddCategoryMapping(3, TorznabCatType.XXXPack);
            AddCategoryMapping(4, TorznabCatType.XXXPack);
            AddCategoryMapping(5, TorznabCatType.XXXDVD);
            AddCategoryMapping(6, TorznabCatType.XXXx264);
            AddCategoryMapping(7, TorznabCatType.XXXImageSet);
            AddCategoryMapping(8, TorznabCatType.XXXImageSet);
        }

        protected override List<KeyValuePair<string, string>> GetSearchQueryParameters(TorznabQuery query)
        {
            var categoryMapping = MapTorznabCapsToTrackers(query).Distinct().ToList();
            var qc = new List<KeyValuePair<string, string>> // NameValueCollection don't support cat[]=19&cat[]=6
            {
                {"in", "1"},
                {"category", categoryMapping.Any() ? categoryMapping.First() : "0"},
                {"search", GetSearchTerm(query).Trim()}
            };

            return qc;
        }

        protected override List<int> ParseCategories(TorznabQuery query, JToken row)
        {
            var cat = row.Value<JObject>("category").Properties().First().Name;
            return MapTrackerCatToNewznab(cat).ToList();
        }
    }
}
