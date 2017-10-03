using Jackett.Models;
using Jackett.Services;
using Jackett.Utils.Clients;
using NLog;
using System.Text;
using Jackett.Indexers.Abstract;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

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

        protected override string GetSearchString(TorznabQuery query)
        {
            if (string.IsNullOrEmpty(query.SearchTerm) && string.IsNullOrEmpty(query.ImdbID))
            {
                return "%";
            }
            var searchString = query.GetQueryString();
            Regex ReplaceRegex = new Regex("[^a-zA-Z0-9]+");
            searchString = ReplaceRegex.Replace(searchString, "%");
            return searchString;
        }

        protected override async Task<WebClientByteResult> RequestBytesWithCookies(string url, string cookieOverride = null, RequestType method = RequestType.GET, string referer = null, IEnumerable<KeyValuePair<string, string>> data = null, Dictionary<string, string> headers = null)
        {
            CookieHeader = null; // Download fill fail with cookies set
            return await base.RequestBytesWithCookies(url, cookieOverride, method, referer, data, headers);
        }
    }
}
