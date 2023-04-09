using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Jackett.Common.Indexers.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class Orpheus : GazelleTracker
    {
        public override string Id => "orpheus";
        public override string Name => "Orpheus";
        public override string Description => "A music tracker";
        public override string SiteLink { get; protected set; } = "https://orpheus.network/";
        public override string Language => "en-US";
        public override string Type => "private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        // API Reference: https://github.com/OPSnet/Gazelle/wiki/JSON-API-Documentation
        protected override string DownloadUrl => SiteLink + "ajax.php?action=download" + (useTokens ? "&usetoken=1" : "") + "&id=";
        protected override string AuthorizationFormat => "token {0}";
        protected override int ApiKeyLength => 118;
        protected override string FlipOptionalTokenString(string requestLink) => requestLink.Replace("usetoken=1", "");
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
    }
}
