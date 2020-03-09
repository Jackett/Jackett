using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    public class PassThePopcorn : BaseWebIndexer
    {
        private static string SearchUrl => "https://passthepopcorn.me/torrents.php";
        private string AuthKey { get; set; }

        private new ConfigurationDataAPILoginWithUserAndPasskeyAndFilter configData
        {
            get => (ConfigurationDataAPILoginWithUserAndPasskeyAndFilter)base.configData;
            set => base.configData = value;
        }

        public PassThePopcorn(IIndexerConfigurationService configService, Utils.Clients.WebClient c, Logger l, IProtectionService ps)
            : base(name: "PassThePopcorn",
                description: "PassThePopcorn is a Private site for MOVIES / TV",
                link: "https://passthepopcorn.me/",
                caps: new TorznabCapabilities(),
                configService: configService,
                client: c,
                logger: l,
                p: ps,
                configData: new ConfigurationDataAPILoginWithUserAndPasskeyAndFilter(@"Enter filter options below to restrict search results.
                                                                        Separate options with a space if using more than one option.<br>Filter options available:
                                                                        <br><code>GoldenPopcorn</code><br><code>Scene</code><br><code>Checked</code><br><code>Free</code>"))
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";

            TorznabCaps.SupportsImdbMovieSearch = true;

            webclient.requestDelay = 2; // 0.5 requests per second

            AddCategoryMapping(1, TorznabCatType.Movies, "Feature Film");
            AddCategoryMapping(1, TorznabCatType.MoviesForeign);
            AddCategoryMapping(1, TorznabCatType.MoviesOther);
            AddCategoryMapping(1, TorznabCatType.MoviesSD);
            AddCategoryMapping(1, TorznabCatType.MoviesHD);
            AddCategoryMapping(1, TorznabCatType.Movies3D);
            AddCategoryMapping(1, TorznabCatType.MoviesBluRay);
            AddCategoryMapping(1, TorznabCatType.MoviesDVD);
            AddCategoryMapping(1, TorznabCatType.MoviesWEBDL);
            AddCategoryMapping(2, TorznabCatType.Movies, "Short Film");
            AddCategoryMapping(3, TorznabCatType.TV, "Miniseries");
            AddCategoryMapping(4, TorznabCatType.TV, "Stand-up Comedy");
            AddCategoryMapping(5, TorznabCatType.TV, "Live Performance");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            IsConfigured = false;
            try
            {
                var results = await PerformQuery(new TorznabQuery());
                if (!results.Any())
                    throw new Exception("Testing returned no results!");
                IsConfigured = true;
                SaveConfig();
            }
            catch (Exception e)
            {
                throw new ExceptionWithConfigData(e.Message, configData);
            }

            return IndexerConfigurationStatus.Completed;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var configGoldenPopcornOnly = configData.FilterString.Value.ToLowerInvariant().Contains("goldenpopcorn");
            var configSceneOnly = configData.FilterString.Value.ToLowerInvariant().Contains("scene");
            var configCheckedOnly = configData.FilterString.Value.ToLowerInvariant().Contains("checked");
            var configFreeOnly = configData.FilterString.Value.ToLowerInvariant().Contains("free");


            var movieListSearchUrl = SearchUrl;
            var queryCollection = new NameValueCollection {{"json", "noredirect"}};

            if (!string.IsNullOrEmpty(query.ImdbID))
                queryCollection.Add("searchstr", query.ImdbID);
            else if (!string.IsNullOrEmpty(query.GetQueryString()))
                queryCollection.Add("searchstr", query.GetQueryString());
            if (configFreeOnly)
                queryCollection.Add("freetorrent", "1");

            movieListSearchUrl += "?" + queryCollection.GetQueryString();

            var authHeaders = new Dictionary<string, string>()
            {
                { "ApiUser", configData.User.Value },
                { "ApiKey", configData.Key.Value }
            };

            var results = await RequestStringWithCookiesAndRetry(movieListSearchUrl, headers: authHeaders);
            if (results.IsRedirect) // untested
                results = await RequestStringWithCookiesAndRetry(movieListSearchUrl, headers: authHeaders);
            try
            {
                //Iterate over the releases for each movie
                var jsResults = JObject.Parse(results.Content);
                foreach (var movie in jsResults["Movies"])
                {
                    var movieTitle = (string)movie["Title"];
                    var year = (string)movie["Year"];
                    var movieImdbIdStr = (string)movie["ImdbId"];
                    var coverStr = (string)movie["Cover"];
                    var coverUri = !string.IsNullOrEmpty(coverStr) ? new Uri(coverStr) : null;
                    var movieImdbId = !string.IsNullOrEmpty(movieImdbIdStr) ? (long?)long.Parse(movieImdbIdStr) : null;
                    var movieGroupId = (string)movie["GroupId"];
                    foreach (var torrent in movie["Torrents"])
                    {
                        var release = new ReleaseInfo();
                        var releaseName = (string)torrent["ReleaseName"];
                        release.Title = releaseName;
                        release.Description = $"Title: {movieTitle}";
                        release.BannerUrl = coverUri;
                        release.Imdb = movieImdbId;
                        release.Comments = new Uri($"{SearchUrl}?id={WebUtility.UrlEncode(movieGroupId)}");
                        release.Size = long.Parse((string)torrent["Size"]);
                        release.Grabs = long.Parse((string)torrent["Snatched"]);
                        release.Seeders = int.Parse((string)torrent["Seeders"]);
                        release.Peers = release.Seeders + int.Parse((string)torrent["Leechers"]);
                        release.PublishDate = DateTime.ParseExact((string)torrent["UploadTime"], "yyyy-MM-dd HH:mm:ss",
                                                CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToLocalTime();

                        var releaseLinkQuery = new NameValueCollection
                        {
                            {"action", "download"},
                            {"id", (string)torrent["Id"]},
                            {"authkey", AuthKey},
                            {"torrent_pass", configData.Passkey.Value},
                        };
                        release.Link = new UriBuilder(SearchUrl) {Query = releaseLinkQuery.GetQueryString()}.Uri;
                        release.Guid = release.Link;
                        release.MinimumRatio = 1;
                        release.MinimumSeedTime = 345600;
                        var free = !(torrent["FreeleechType"] is null);
                        release.DownloadVolumeFactor = free ? 0 : 1;
                        release.UploadVolumeFactor = 1;
                        release.Category = new List<int> { 2000 };

                        bool.TryParse((string)torrent["GoldenPopcorn"], out var golden);
                        bool.TryParse((string)torrent["Scene"], out var scene);
                        bool.TryParse((string)torrent["Checked"], out var check);

                        if (configGoldenPopcornOnly && !golden)
                            continue; //Skip release if user only wants GoldenPopcorn
                        if (configSceneOnly && !scene)
                            continue; //Skip release if user only wants Scene
                        if (configCheckedOnly && !check)
                            continue; //Skip release if user only wants Checked
                        if (configFreeOnly && !free)
                            continue;

                        var titleTags = new List<string>();
                        var quality = (string)torrent["Quality"];
                        var container = (string)torrent["Container"];
                        var codec = (string)torrent["Codec"];
                        var resolution = (string)torrent["Resolution"];
                        var source = (string)torrent["Source"];

                        if (year != null)
                            release.Description += $"<br>\nYear: {year}";
                        if (quality != null)
                            release.Description += $"<br>\nQuality: {quality}";
                        if (resolution != null)
                        {
                            titleTags.Add(resolution);
                            release.Description += $"<br>\nResolution: {resolution}";
                        }
                        if (source != null)
                        {
                            titleTags.Add(source);
                            release.Description += $"<br>\nSource: {source}";
                        }
                        if (codec != null)
                        {
                            titleTags.Add(codec);
                            release.Description += $"<br>\nCodec: {codec}";
                        }
                        if (container != null)
                        {
                            titleTags.Add(container);
                            release.Description += $"<br>\nContainer: {container}";
                        }
                        if (scene)
                        {
                            titleTags.Add("Scene");
                            release.Description += "<br>\nScene";
                        }
                        if (check)
                        {
                            titleTags.Add("Checked");
                            release.Description += "<br>\nChecked";
                        }
                        if (golden)
                        {
                            titleTags.Add("Golden Popcorn");
                            release.Description += "<br>\nGolden Popcorn";
                        }

                        if (titleTags.Any())
                            release.Title += " [" + string.Join(" / ", titleTags) + "]";

                        releases.Add(release);
                    }
                }
            }
            catch (Exception ex)
            {
                OnParseError(results.Content, ex);
            }

            return releases;
        }
    }
}
