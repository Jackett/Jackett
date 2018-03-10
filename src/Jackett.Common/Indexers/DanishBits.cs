using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jackett.Common.Indexers.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;

namespace Jackett.Common.Indexers
{
    public class DanishBits : CouchPotatoTracker
    {
        public override string[] LegacySiteLinks { get; protected set; } = new string[] {
            "http://danishbits.org/",
        };

        private new ConfigurationDataUserPasskey configData
        {
            get { return (ConfigurationDataUserPasskey)base.configData; }
            set { base.configData = value; }
        }

        public DanishBits(IIndexerConfigurationService configService, WebClient c, Logger l, IProtectionService ps)
            : base(name: "DanishBits",
                description: "A danish closed torrent tracker",
                link: "https://danishbits.org/",
                endpoint: "couchpotato.php",
                configService: configService,
                client: c,
                logger: l,
                p: ps,
                configData: new ConfigurationDataUserPasskey("Note about Passkey: This is not your login Password. Find the Passkey by logging into DanishBits with your Browser, and under your account page you'll see your passkey under the 'Personal' section on the left side.")
            )
        {
            Encoding = Encoding.UTF8;
            Language = "da-dk";
            Type = "private";

            AddCategoryMapping("movie", TorznabCatType.Movies);
            AddCategoryMapping("tv", TorznabCatType.TV);
            AddCategoryMapping("blandet", TorznabCatType.Other); // e.g. games
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
