using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class SceneHD : IndexerBase
    {
        public override string Id => "scenehd";
        public override string Name => "SceneHD";
        public override string Description => "SceneHD is Private site for HD TV / MOVIES";
        public override string SiteLink { get; protected set; } = "https://scenehd.org/";
        public override string Language => "en-US";
        public override string Type => "private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private string SearchUrl => SiteLink + "browse.php?";
        private string DetailsUrl => SiteLink + "details.php?";
        private string DownloadUrl => SiteLink + "download.php?";

        private new ConfigurationDataPasskey configData => (ConfigurationDataPasskey)base.configData;

        public SceneHD(IIndexerConfigurationService configService, WebClient c, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(configService: configService,
                   client: c,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataPasskey("You can find the Passkey if you generate a RSS " +
                                                            "feed link. It's the last parameter in the URL."))
        {
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                },
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q, MovieSearchParam.ImdbId
                },
                MusicSearchParams = new List<MusicSearchParam>
                {
                    MusicSearchParam.Q
                }
            };

            caps.Categories.AddCategoryMapping(2, TorznabCatType.MoviesUHD, "Movie/2160");
            caps.Categories.AddCategoryMapping(1, TorznabCatType.MoviesHD, "Movie/1080");
            caps.Categories.AddCategoryMapping(4, TorznabCatType.MoviesHD, "Movie/720");
            caps.Categories.AddCategoryMapping(8, TorznabCatType.MoviesBluRay, "Movie/BD5/9");
            caps.Categories.AddCategoryMapping(6, TorznabCatType.TVUHD, "TV/2160");
            caps.Categories.AddCategoryMapping(5, TorznabCatType.TVHD, "TV/1080");
            caps.Categories.AddCategoryMapping(7, TorznabCatType.TVHD, "TV/720");
            caps.Categories.AddCategoryMapping(22, TorznabCatType.MoviesBluRay, "Bluray/Complete");
            caps.Categories.AddCategoryMapping(10, TorznabCatType.XXX, "XXX");
            caps.Categories.AddCategoryMapping(16, TorznabCatType.MoviesOther, "Subpacks");
            caps.Categories.AddCategoryMapping(13, TorznabCatType.AudioVideo, "MVID");
            caps.Categories.AddCategoryMapping(9, TorznabCatType.Other, "Other");

            return caps;
        }

        public override void LoadValuesFromJson(JToken jsonConfig, bool useProtectionService = false)
        {
            base.LoadValuesFromJson(jsonConfig, useProtectionService);

            webclient?.AddTrustedCertificate(new Uri(SiteLink).Host, "245621438BD9E2E4D99D753CA1F5088072FCB707");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            if (configData.Passkey.Value.Length != 32)
                throw new Exception("Invalid Passkey configured. Expected length: 32");

            var releases = await PerformQuery(new TorznabQuery());
            await ConfigureIfOK(string.Empty, releases.Any(),
                                () => throw new Exception("Could not find releases from this URL."));

            return IndexerConfigurationStatus.Completed;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var passkey = configData.Passkey.Value;

            var qc = new NameValueCollection
            {
                { "api", "" },
                { "passkey", passkey },
                { "search", query.IsImdbQuery ? query.ImdbID : query.GetQueryString() }
            };

            var categoryMapping = MapTorznabCapsToTrackers(query).Distinct().ToList();

            if (categoryMapping.Count > 0)
            {
                qc.Add("cat", string.Join(",", categoryMapping));
            }

            var searchUrl = SearchUrl + qc.GetQueryString();
            var response = await RequestWithCookiesAndRetryAsync(searchUrl);

            if (response.ContentString?.Contains("User not found or passkey not set") == true)
                throw new Exception("The passkey is invalid. Check the indexer configuration.");

            try
            {
                var jsonContent = JArray.Parse(response.ContentString);
                foreach (var item in jsonContent)
                {
                    var title = item.Value<string>("name");
                    if (!query.IsImdbQuery && !query.MatchQueryStringAND(title))
                        continue;

                    var id = item.Value<long>("id");
                    var details = new Uri(DetailsUrl + "id=" + id);
                    var link = new Uri(DownloadUrl + "id=" + id + "&passkey=" + passkey);
                    var publishDate = DateTime.ParseExact(item.Value<string>("added"), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    var dlVolumeFactor = item.Value<int>("is_freeleech") == 1 ? 0 : 1;

                    var release = new ReleaseInfo
                    {
                        Title = title,
                        Link = link,
                        Details = details,
                        Guid = details,
                        Category = MapTrackerCatToNewznab(item.Value<string>("category")),
                        PublishDate = publishDate,
                        Size = item.Value<long>("size"),
                        Grabs = item.Value<long>("times_completed"),
                        Files = item.Value<long>("numfiles"),
                        Seeders = item.Value<int>("seeders"),
                        Peers = item.Value<int>("leechers") + item.Value<int>("seeders"),
                        Imdb = ParseUtil.GetImdbId(item.Value<string>("imdbid")),
                        MinimumRatio = 1,
                        MinimumSeedTime = 0,
                        DownloadVolumeFactor = dlVolumeFactor,
                        UploadVolumeFactor = 1
                    };

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(response.ContentString, ex);
            }

            return releases;
        }
    }
}
