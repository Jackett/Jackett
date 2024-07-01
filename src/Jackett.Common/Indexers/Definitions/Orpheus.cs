using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Jackett.Common.Indexers.Definitions.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class Orpheus : GazelleTracker
    {
        public override string Id => "orpheus";
        public override string Name => "Orpheus";
        public override string Description => "A music tracker";
        // Status: https://ops.trackerstatus.info/
        public override string SiteLink { get; protected set; } = "https://orpheus.network/";
        public override string Language => "en-US";
        public override string Type => "private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        // API Reference: https://github.com/OPSnet/Gazelle/wiki/JSON-API-Documentation
        protected override string AuthorizationFormat => "token {0}";
        protected override int ApiKeyLength => 116;
        protected override int ApiKeyLengthLegacy => 118;
        protected override string FlipOptionalTokenString(string requestLink) => requestLink.Replace("&usetoken=1", "");
        public Orpheus(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cs: cs,
                   supportsFreeleechTokens: true,
                   has2Fa: false,
                   useApiKey: true,
                   usePassKey: false,
                   instructionMessageOptional: "<ol><li>Go to Orpheus's site and open your account settings.</li><li>Under <b>Access Settings</b> click on 'Create a new token'</li><li>Give it a name you like and click <b>Generate</b>.</li><li>Copy the generated API Key and paste it in the above text field.</li></ol>")
        {
            configData.AddDynamic("Account Inactivity", new DisplayInfoConfigurationItem("Account Inactivity", "To keep your account active, sign in and browse the site at least once every 120 days. Seeding torrents does not count as account activity, so in order to remain active you need to sign in and browse the site. Power Users (and above) are immune to the inactivity timer, but logging in regularly is recommended to learn about special events and new features. Donors are exempt from automatic account disabling due to inactivity. If you wish to always maintain an active account consider donating."));
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                MusicSearchParams = new List<MusicSearchParam>
                {
                    MusicSearchParam.Q, MusicSearchParam.Album, MusicSearchParam.Artist, MusicSearchParam.Label, MusicSearchParam.Year, MusicSearchParam.Genre
                },
                BookSearchParams = new List<BookSearchParam>
                {
                    BookSearchParam.Q, BookSearchParam.Genre
                }
            };

            caps.Categories.AddCategoryMapping(1, TorznabCatType.Audio, "Music");
            caps.Categories.AddCategoryMapping(2, TorznabCatType.PC, "Applications");
            caps.Categories.AddCategoryMapping(3, TorznabCatType.BooksEBook, "E-Books");
            caps.Categories.AddCategoryMapping(4, TorznabCatType.AudioAudiobook, "Audiobooks");
            caps.Categories.AddCategoryMapping(5, TorznabCatType.Other, "E-Learning Videos");
            caps.Categories.AddCategoryMapping(6, TorznabCatType.Other, "Comedy");
            caps.Categories.AddCategoryMapping(7, TorznabCatType.BooksComics, "Comics");

            return caps;
        }

        protected override Uri GetDownloadUrl(int torrentId, bool canUseToken)
        {
            return new Uri($"{SiteLink}ajax.php?action=download{(useTokens && canUseToken ? "&usetoken=1" : "")}&id={torrentId}");
        }
    }
}
