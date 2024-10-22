using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Jackett.Common.Extensions;
using Jackett.Common.Indexers.Definitions.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Newtonsoft.Json.Linq;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class CineClassics : GazelleTracker
    {
        public override string Id => "cineclassics";
        public override string Name => "CineClassics";
        public override string Description => "CineClassics is a Private site for MOVIES pre 2015";
        public override string SiteLink { get; protected set; } = "https://cineclassics.org/";
        public override string Language => "en-US";
        public override string Type => "private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        public CineClassics(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps, ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cs: cs,
                   supportsFreeleechTokens: true,
                   supportsFreeleechOnly: true,
                   imdbInTags: false,
                   has2Fa: true,
                   useApiKey: false,
                   usePassKey: false,
                   instructionMessageOptional: null)
        {
            configData.AddDynamic("showFilename", new BoolConfigurationItem("Use the first torrent filename as the title") { Value = false });
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q, MovieSearchParam.ImdbId, MovieSearchParam.Genre
                }
            };

            caps.Categories.AddCategoryMapping(1, TorznabCatType.Movies, "Movies");

            return caps;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            // Cineclassics uses imdbid in the searchstr so prevent cataloguenumber or taglist search.
            if (query.IsImdbQuery)
            {
                query.SearchTerm = query.ImdbID;
                query.ImdbID = null;
            }

            return await base.PerformQuery(query);
        }

        protected override bool ReleaseInfoPostParse(ReleaseInfo release, JObject torrent, JObject result)
        {
            // override release title
            var groupName = WebUtility.HtmlDecode((string)result["groupName"]);
            var groupSubName = WebUtility.HtmlDecode((string)result["groupSubName"]);
            var groupYear = (string)result["groupYear"];
            var title = new StringBuilder();
            title.Append(groupName);

            if (!string.IsNullOrEmpty(groupYear) && groupYear != "0")
                title.Append(" [" + groupYear + "]");


            var torrentId = torrent["torrentId"];

            var flags = new List<string>();

            var codec = (string)torrent["codec"];
            if (!string.IsNullOrEmpty(codec))
                flags.Add(codec);

            var source = (string)torrent["source"];
            if (!string.IsNullOrEmpty(source))
                flags.Add(source);

            var resolution = (string)torrent["resolution"];
            if (!string.IsNullOrEmpty(resolution))
                flags.Add(resolution);

            var container = (string)torrent["container"];
            if (!string.IsNullOrEmpty(container))
                flags.Add(container);

            var processing = (string)torrent["processing"];
            if (!string.IsNullOrEmpty(processing))
                flags.Add(processing);

            if (flags.Count > 0)
                title.Append(" " + string.Join(" / ", flags));

            release.Title = title.ToString();

            // option to overwrite the title with the first torrent filename #13646
            if (((BoolConfigurationItem)configData.GetDynamic("showFilename")).Value)
                release.Title = WebUtility.HtmlDecode((string)torrent["fileName"]);

            release.DoubanId = ParseUtil.GetLongFromString((string)result["doubanId"]);

            var isPersonalFreeleech = (bool?)torrent["isPersonalFreeleech"];
            if (isPersonalFreeleech is true)
                release.DownloadVolumeFactor = 0;

            var imdbID = (string)result["imdbId"];
            if (!string.IsNullOrEmpty(imdbID))
                release.Imdb = ParseUtil.GetImdbId(imdbID);

            release.MinimumRatio = 1;
            release.MinimumSeedTime = 172800; // 48 hours
                                              // tag each results with Movie cats.
            release.Category = new List<int> { TorznabCatType.Movies.ID };
            if (!string.IsNullOrEmpty(groupSubName))
                release.Description = groupSubName;

            return true;
        }
    }
}
