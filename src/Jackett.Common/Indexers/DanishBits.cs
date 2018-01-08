using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jackett.Indexers.Abstract;
using Jackett.Models;
using Jackett.Models.IndexerConfig;
using Jackett.Services.Interfaces;
using Jackett.Utils.Clients;
using NLog;

namespace Jackett.Indexers
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

            AddCategoryMapping(1, TorznabCatType.TVHD, "HD TV");
            AddCategoryMapping(2, TorznabCatType.MoviesHD, "HD Movies");
            AddCategoryMapping(3, TorznabCatType.MoviesForeign, "Danske Film");
            AddCategoryMapping(4, TorznabCatType.TVFOREIGN, "Danske Tv");
            AddCategoryMapping(5, TorznabCatType.AudioAudiobook, "Lydbøger");
            AddCategoryMapping(7, TorznabCatType.PC, "PC Apps");
            AddCategoryMapping(8, TorznabCatType.MoviesBluRay, "Blu-ray Film");
            AddCategoryMapping(9, TorznabCatType.Movies, "Film Bokssæt");
            AddCategoryMapping(10, TorznabCatType.MoviesDVD, "Nordiske DVD Film");
            AddCategoryMapping(11, TorznabCatType.MoviesDVD, "DVD Film");

            AddCategoryMapping(20, TorznabCatType.TV, "TV");
            AddCategoryMapping(21, TorznabCatType.TV, "TV Bokssæt");
            AddCategoryMapping(21, TorznabCatType.MovieHD, "HD x264 Film");
            AddCategoryMapping(24, TorznabCatType.MovieSD, "SD Film");
            AddCategoryMapping(25, TorznabCatType.XXX, "XXX");

			AddCategoryMapping(28, TorznabCatType.MoviesDVD, "DVD Film (UNiTY)");
			AddCategoryMapping(29, TorznabCatType.MovieHD, "HD Film (UNiTY)");
			AddCategoryMapping(30, TorznabCatType.TV, "TV (Substance)");            
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
