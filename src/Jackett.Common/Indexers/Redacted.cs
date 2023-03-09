using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Jackett.Common.Indexers.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using NLog;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class Redacted : GazelleTracker
    {
        public override string Id => "redacted";
        public override string Name => "Redacted";
        public override string Description => "A music tracker";
        public override string SiteLink { get; protected set; } = "https://redacted.ch/";
        public override string Language => "en-US";
        public override string Type => "private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        protected override string DownloadUrl => SiteLink + "ajax.php?action=download&usetoken=" + (useTokens ? "1" : "0") + "&id=";

        public Redacted(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps, ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cs: cs,
                   supportsFreeleechTokens: true,
                   has2Fa: false,
                   useApiKey: true,
                   instructionMessageOptional: "<ol><li>Go to Redacted's site and open your account settings.</li><li>Go to <b>Access Settings</b> tab and copy the API Key.</li><li>Ensure that you've checked <b>Confirm API Key</b>.</li><li>Finally, click <b>Save Profile</b>.</li></ol>"
                )
        {
            webclient.EmulateBrowser = false; // Issue #9751
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
            var results = await base.PerformQuery(query);
            // results must contain search terms
            results = results.Where(release => query.MatchQueryStringAND(release.Title));
            return results;
        }

    }
}
