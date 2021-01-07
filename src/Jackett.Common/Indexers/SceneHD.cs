using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class SceneHD : BaseWebIndexer
    {
        private string SearchUrl => SiteLink + "browse.php?";
        private string DetailsUrl => SiteLink + "details.php?";
        private string DownloadUrl => SiteLink + "download.php?";

        private new ConfigurationDataPasskey configData => (ConfigurationDataPasskey)base.configData;

        public SceneHD(IIndexerConfigurationService configService, WebClient c, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "scenehd",
                   name: "SceneHD",
                   description: "SceneHD is Private site for HD TV / MOVIES",
                   link: "https://scenehd.org/",
                   configService: configService,
                   caps: new TorznabCapabilities
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
                   },
                   client: c,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataPasskey("You can find the Passkey if you generate a RSS " +
                                                            "feed link. It's the last parameter in the URL."))
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";

            webclient.AddTrustedCertificate(new Uri(SiteLink).Host, "D948487DD52462F2D1E62B990D608051E3DE5AA6");

            AddCategoryMapping(2, TorznabCatType.MoviesUHD, "Movie/2160");
            AddCategoryMapping(1, TorznabCatType.MoviesHD, "Movie/1080");
            AddCategoryMapping(4, TorznabCatType.MoviesHD, "Movie/720");
            AddCategoryMapping(8, TorznabCatType.MoviesBluRay, "Movie/BD5/9");
            AddCategoryMapping(6, TorznabCatType.TVUHD, "TV/2160");
            AddCategoryMapping(5, TorznabCatType.TVHD, "TV/1080");
            AddCategoryMapping(7, TorznabCatType.TVHD, "TV/720");
            AddCategoryMapping(22, TorznabCatType.MoviesBluRay, "Bluray/Complete");
            AddCategoryMapping(10, TorznabCatType.XXX, "XXX");
            AddCategoryMapping(16, TorznabCatType.MoviesOther, "Subpacks");
            AddCategoryMapping(13, TorznabCatType.AudioVideo, "MVID");
            AddCategoryMapping(9, TorznabCatType.Other, "Other");
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

            foreach (var cat in MapTorznabCapsToTrackers(query))
                qc.Add("categories[" + cat + "]", "1");

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
                        Imdb = ParseUtil.GetImdbID(item.Value<string>("imdbid")),
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
