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

        // API Reference: https://github.com/OPSnet/Gazelle/wiki/JSON-API-Documentation
        protected override string DownloadUrl => SiteLink + "ajax.php?action=download" + (useTokens ? "&usetoken=1" : "") + "&id=";
        protected override string AuthorizationFormat => "token {0}";
        protected override int ApiKeyLength => 118;
        protected override string FlipOptionalTokenString(string requestLink) => requestLink.Replace("usetoken=1", "");
        public Orpheus(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(
                   caps: new TorznabCapabilities
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
                   },
                   configService: configService,
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
            AddCategoryMapping(1, TorznabCatType.Audio, "Music");
            AddCategoryMapping(2, TorznabCatType.PC, "Applications");
            AddCategoryMapping(3, TorznabCatType.Books, "E-Books");
            AddCategoryMapping(4, TorznabCatType.AudioAudiobook, "Audiobooks");
            AddCategoryMapping(5, TorznabCatType.Movies, "E-Learning Videos");
            AddCategoryMapping(6, TorznabCatType.Audio, "Comedy");
            AddCategoryMapping(7, TorznabCatType.Books, "Comics");
        }
    }
}
