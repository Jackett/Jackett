using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Jackett.Common.Indexers.Definitions.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Newtonsoft.Json.Linq;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class Redacted : GazelleTracker
    {
        public override string Id => "redacted";
        public override string[] Replaces => new[] { "passtheheadphones" };
        public override string Name => "Redacted";
        public override string Description => "A music tracker";
        // Status: https://red.trackerstatus.info/
        public override string SiteLink { get; protected set; } = "https://redacted.sh/";
        public override string[] LegacySiteLinks => new[] { "https://redacted.ch/" };
        public override string Language => "en-US";
        public override string Type => "private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        public Redacted(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps, ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cs: cs,
                   supportsFreeleechTokens: true,
                   supportsFreeloadOnly: true,
                   has2Fa: false,
                   useApiKey: true,
                   instructionMessageOptional: "<ol><li>Go to Redacted's site and open your account settings.</li><li>Go to <b>Access Settings</b> tab and copy the API Key.</li><li>Ensure that you've checked <b>Confirm API Key</b>.</li><li>Finally, click <b>Save Profile</b>.</li></ol>"
                )
        {
            webclient.EmulateBrowser = false; // Issue #9751
            configData.AddDynamic("Account Inactivity", new DisplayInfoConfigurationItem("Account Inactivity", "To keep your account active, sign in and browse the site at least once every 120 days. Seeding torrents does not count as account activity, so in order to remain active you need to sign in and browse the site. Some scripts or automated tools may be detected as activity on your account, but it is best to sign in with a browser from time to time just to make sure you will not be flagged as inactive. Donors and certain user classes (Power User+) are exempt from automatic account disabling due to inactivity. If you wish to always maintain an active account consider donating or reaching a higher user class."));
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q, MovieSearchParam.Genre
                },
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
            caps.Categories.AddCategoryMapping(3, TorznabCatType.Books, "E-Books");
            caps.Categories.AddCategoryMapping(4, TorznabCatType.AudioAudiobook, "Audiobooks");
            caps.Categories.AddCategoryMapping(5, TorznabCatType.Movies, "E-Learning Videos");
            caps.Categories.AddCategoryMapping(6, TorznabCatType.Audio, "Comedy");
            caps.Categories.AddCategoryMapping(7, TorznabCatType.Books, "Comics");

            return caps;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = await base.PerformQuery(query);

            // results must contain search terms
            releases = releases.Where(release => query.MatchQueryStringAND(release.Title));

            return releases;
        }

        protected override bool ShouldSkipRelease(JObject torrent)
        {
            var isFreeload = bool.TryParse((string)torrent["isFreeload"], out var freeload) && freeload;

            if (configData.FreeloadOnly is { Value: true } && !isFreeload)
            {
                return true;
            }

            return base.ShouldSkipRelease(torrent);
        }

        protected override Uri GetDownloadUrl(int torrentId, bool canUseToken)
        {
            return new Uri($"{SiteLink}ajax.php?action=download{(useTokens && canUseToken ? "&usetoken=1" : "")}&id={torrentId}");
        }
    }
}
