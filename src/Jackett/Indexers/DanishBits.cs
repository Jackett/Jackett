using Jackett.Models;
using Jackett.Services;
using Jackett.Utils.Clients;
using NLog;
using System.Text;
using Jackett.Indexers.Abstract;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Jackett.Indexers
{
    public class DanishBits : CouchPotatoTracker
    {
        public DanishBits(IIndexerConfigurationService configService, IWebClient c, Logger l, IProtectionService ps)
            : base(name: "DanishBits",
                description: "A danish closed torrent tracker",
                link: "https://danishbits.org/",
                endpoint: "couchpotato.php",
                configService: configService,
                client: c,
                logger: l,
                p: ps
                )
        {
            Encoding = Encoding.GetEncoding("UTF-8");
            Language = "da-dk";
            Type = "private";

            AddCategoryMapping("movie", TorznabCatType.Movies);
            AddCategoryMapping("tv", TorznabCatType.TV);
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var newQuery = query;
            if (string.IsNullOrEmpty(query.SearchTerm) && string.IsNullOrEmpty(query.ImdbID))
            { 
                newQuery = query.Clone();
                newQuery.SearchTerm = "%";
            }
            return await base.PerformQuery(newQuery);
        }
    }
}
