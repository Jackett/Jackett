using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Jackett.Common.Indexers.Definitions.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class iAnon : GazelleTracker
    {
        public override string Id => "ianon";
        public override string Name => "iAnon";
        public override string Description => "MacOS software tracker";
        public override string SiteLink { get; protected set; } = "https://ianon.app/";
        public override string Language => "en-US";
        public override string Type => "private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        protected override string AuthorizationFormat => "token {0}";
        protected override int ApiKeyLength => 118;

        public iAnon(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps, ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cs: cs,
                   supportsFreeleechTokens: true,
                   useApiKey: true,
                   instructionMessageOptional: "<ol><li>Go to iAnon's site and open your account settings.</li><li>Go to <b>Access Settings</b> tab use the <b>API Keys: click here to create a new token</b> link.</li><li>Give it a name and click <b>Generate</b>.</li><li>Finally, copy/paste the token to your Jackett config APIKey input box.</li></ol>"
                )
        {
            configData.AddDynamic("Account Inactivity", new DisplayInfoConfigurationItem("Account Inactivity", "To keep your account active, sign in and browse the site at least once every 120 days. Seeding torrents does not count as account activity, so in order to remain active you need to sign in and browse the site. Power Users (and above) are immune to the inactivity timer, but logging in regularly is recommended to learn about special events and new features. Donors are exempt from automatic account disabling due to inactivity. If you wish to always maintain an active account consider donating."));
        }

        protected override string FlipOptionalTokenString(string requestLink) => requestLink.Replace("&usetoken=1", "");

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                MusicSearchParams = new List<MusicSearchParam>
                {
                    MusicSearchParam.Q
                }
            };

            caps.Categories.AddCategoryMapping(1, TorznabCatType.PCMac, "Applications");
            caps.Categories.AddCategoryMapping(2, TorznabCatType.PCGames, "Games");
            caps.Categories.AddCategoryMapping(3, TorznabCatType.PCMobileiOS, "IOS Applications");
            caps.Categories.AddCategoryMapping(4, TorznabCatType.PCMobileiOS, "IOS Games");
            caps.Categories.AddCategoryMapping(5, TorznabCatType.Other, "Graphics");
            caps.Categories.AddCategoryMapping(6, TorznabCatType.Audio, "Audio");
            caps.Categories.AddCategoryMapping(7, TorznabCatType.Other, "Tutorials");
            caps.Categories.AddCategoryMapping(8, TorznabCatType.Other, "Other");

            return caps;
        }

        protected override Uri GetDownloadUrl(int torrentId, bool canUseToken)
        {
            return new Uri($"{SiteLink}ajax.php?action=download{(useTokens && canUseToken ? "&usetoken=1" : "")}&id={torrentId}");
        }
    }
}
