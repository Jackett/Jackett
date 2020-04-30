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
        private string SearchUrl => SiteLink + "browse.php";

        private new ConfigurationDataCookie configData
        {
            get => (ConfigurationDataCookie)base.configData;
            set => base.configData = value;
        }

        public SceneHD(IIndexerConfigurationService configService, WebClient c, Logger l, IProtectionService ps)
            : base(name: "SceneHD",
                description: "SceneHD is Private site for HD TV / MOVIES",
                link: "https://scenehd.org/",
                configService: configService,
                caps: new TorznabCapabilities
                {
                    SupportsImdbMovieSearch = true
                },
                client: c,
                logger: l,
                p: ps,
                configData: new ConfigurationDataCookie())
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";

            webclient.EmulateBrowser = false;
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

            // TODO: implement captcha
            CookieHeader = configData.Cookie.Value;
            try
            {
                var results = await PerformQuery(new TorznabQuery());
                if (results.Count() == 0)
                {
                    throw new Exception("Found 0 results in the tracker");
                }

                IsConfigured = true;
                SaveConfig();
                return IndexerConfigurationStatus.Completed;
            }
            catch (Exception e)
            {
                IsConfigured = false;
                throw new Exception("Your cookie did not work: " + e.Message);
            }
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var startTransition = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 3, 0, 0), 3, 5, DayOfWeek.Sunday);
            var endTransition = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 4, 0, 0), 10, 5, DayOfWeek.Sunday);
            var delta = new TimeSpan(1, 0, 0);
            var adjustment = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(new DateTime(1999, 10, 1), DateTime.MaxValue.Date, delta, startTransition, endTransition);
            TimeZoneInfo.AdjustmentRule[] adjustments = { adjustment };
            var Tz = TimeZoneInfo.CreateCustomTimeZone("custom", new TimeSpan(1, 0, 0), "custom", "custom", "custom", adjustments);

            var releases = new List<ReleaseInfo>();

            var qParams = new NameValueCollection
            {
                { "api", "" }
            };
            if (query.ImdbIDShort != null)
                qParams.Add("imdb", query.ImdbIDShort);
            else
                qParams.Add("search", query.SearchTerm);

            foreach (var cat in MapTorznabCapsToTrackers(query))
            {
                qParams.Add("categories[" + cat + "]", "1");
            }

            var urlSearch = SearchUrl;
            urlSearch += "?" + qParams.GetQueryString();

            var response = await RequestStringWithCookiesAndRetry(urlSearch);
            if (response.IsRedirect)
                throw new Exception("not logged in");

            try
            {
                var jsonContent = JArray.Parse(response.Content);
                var sitelink = new Uri(SiteLink);

                foreach (var item in jsonContent)
                {
                    //TODO convert to initializer
                    var release = new ReleaseInfo();

                    var id = item.Value<long>("id");
                    release.Title = item.Value<string>("name");

                    var imdbid = item.Value<string>("imdbid");
                    if (!string.IsNullOrEmpty(imdbid))
                        release.Imdb = long.Parse(imdbid);

                    var category = item.Value<string>("category");
                    release.Category = MapTrackerCatToNewznab(category);

                    release.Link = new Uri(sitelink, "/download.php?id=" + id);
                    release.Comments = new Uri(sitelink, "/details.php?id=" + id);
                    release.Guid = release.Comments;

                    var dateStr = item.Value<string>("added");
                    var dateTime = DateTime.SpecifyKind(DateTime.ParseExact(dateStr, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), DateTimeKind.Unspecified);
                    var pubDateUtc = TimeZoneInfo.ConvertTimeToUtc(dateTime, Tz);
                    release.PublishDate = pubDateUtc;

                    release.Grabs = item.Value<long>("times_completed");
                    release.Files = item.Value<long>("numfiles");
                    release.Seeders = item.Value<int>("seeders");
                    release.Peers = item.Value<int>("leechers") + release.Seeders;
                    var size = item.Value<string>("size");
                    release.Size = ReleaseInfo.GetBytes(size);
                    var is_freeleech = item.Value<int>("is_freeleech");

                    if (is_freeleech == 1)
                        release.DownloadVolumeFactor = 0;
                    else
                        release.DownloadVolumeFactor = 1;
                    release.UploadVolumeFactor = 1;

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(response.Content, ex);
            }

            return releases;
        }
    }
}
