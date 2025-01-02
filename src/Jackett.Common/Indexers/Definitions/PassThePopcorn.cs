using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jackett.Common.Extensions;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class PassThePopcorn : IndexerBase
    {
        public override string Id => "passthepopcorn";
        public override string Name => "PassThePopcorn";
        public override string Description => "PassThePopcorn (PTP) is a Private site for MOVIES / TV";
        // Status: https://ptp.trackerstatus.info/
        public override string SiteLink { get; protected set; } = "https://passthepopcorn.me/";
        public override string Language => "en-US";
        public override string Type => "private";

        public override bool SupportsPagination => true;

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private static string SearchUrl => "https://passthepopcorn.me/torrents.php";
        private string AuthKey { get; set; }
        private string PassKey { get; set; }

        // TODO: merge ConfigurationDataAPILoginWithUserAndPasskeyAndFilter class with with ConfigurationDataUserPasskey
        private new ConfigurationDataAPILoginWithUserAndPasskeyAndFilter configData
        {
            get => (ConfigurationDataAPILoginWithUserAndPasskeyAndFilter)base.configData;
            set => base.configData = value;
        }

        public PassThePopcorn(IIndexerConfigurationService configService, Utils.Clients.WebClient c, Logger l,
            IProtectionService ps, ICacheService cs)
            : base(configService: configService,
                   client: c,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataAPILoginWithUserAndPasskeyAndFilter(@"Enter filter options below to restrict search results.
                                                                        Separate options with a space if using more than one option.<br>Filter options available:
                                                                        <br><code>GoldenPopcorn</code><br><code>Scene</code><br><code>Checked</code><br><code>Free</code>"))
        {
            webclient.requestDelay = 4;
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                LimitsDefault = 50,
                LimitsMax = 50,
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId
                },
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q, MovieSearchParam.ImdbId
                }
            };

            caps.Categories.AddCategoryMapping(1, TorznabCatType.Movies, "Feature Film");
            caps.Categories.AddCategoryMapping(2, TorznabCatType.Movies, "Short Film");
            caps.Categories.AddCategoryMapping(3, TorznabCatType.TV, "Miniseries");
            caps.Categories.AddCategoryMapping(4, TorznabCatType.Movies, "Stand-up Comedy");
            caps.Categories.AddCategoryMapping(5, TorznabCatType.Movies, "Live Performance");
            caps.Categories.AddCategoryMapping(6, TorznabCatType.Movies, "Movie Collection");

            return caps;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            IsConfigured = false;
            try
            {
                var results = await PerformQuery(new TorznabQuery());
                if (!results.Any())
                {
                    throw new Exception("Testing returned no results!");
                }

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
            var queryCollection = new NameValueCollection
            {
                { "action", "advanced" },
                { "json", "noredirect" },
                { "grouping", "0" },
            };

            if (configFreeOnly)
            {
                queryCollection.Set("freetorrent", "1");
            }

            if (configGoldenPopcornOnly)
            {
                queryCollection.Set("scene", "2");
            }

            var searchQuery = query.ImdbID.IsNotNullOrWhiteSpace() ? query.ImdbID : query.GetQueryString();

            if (searchQuery.IsNotNullOrWhiteSpace())
            {
                queryCollection.Set("searchstr", searchQuery);
            }

            var queryCats = MapTorznabCapsToTrackers(query).Select(int.Parse).Distinct().ToList();

            if (searchQuery.IsNullOrWhiteSpace() && queryCats.Any())
            {
                queryCats.ForEach(cat => queryCollection.Set($"filter_cat[{cat}]", "1"));
            }

            if (query.Limit > 0 && query.Offset > 0)
            {
                var page = query.Offset / query.Limit + 1;
                queryCollection.Set("page", page.ToString());
            }

            movieListSearchUrl += "?" + queryCollection.GetQueryString();

            var authHeaders = new Dictionary<string, string>
            {
                { "ApiUser", configData.User.Value },
                { "ApiKey", configData.Key.Value }
            };

            var results = await RequestWithCookiesAndRetryAsync(movieListSearchUrl, headers: authHeaders);
            if (results.IsRedirect) // untested
            {
                results = await RequestWithCookiesAndRetryAsync(movieListSearchUrl, headers: authHeaders);
            }

            var seasonRegex = new Regex(@"\bS\d{2,3}(E\d{2,3})?\b", RegexOptions.Compiled);

            try
            {
                //Iterate over the releases for each movie
                var jsResults = JObject.Parse(results.ContentString);

                AuthKey = (string)jsResults["AuthKey"];
                PassKey = (string)jsResults["PassKey"];

                foreach (var movie in jsResults["Movies"])
                {
                    var movieTitle = (string)movie["Title"];
                    var year = (string)movie["Year"];
                    var movieImdbIdStr = (string)movie["ImdbId"];
                    var posterStr = (string)movie["Cover"];
                    var poster = !string.IsNullOrEmpty(posterStr) ? new Uri(posterStr) : null;
                    var movieImdbId = !string.IsNullOrEmpty(movieImdbIdStr) ? (long?)long.Parse(movieImdbIdStr) : null;
                    var movieGroupId = (string)movie["GroupId"];
                    foreach (var torrent in movie["Torrents"])
                    {
                        var releaseName = (string)torrent["ReleaseName"];
                        var torrentId = (string)torrent["Id"];

                        var releaseLinkQuery = new NameValueCollection
                        {
                            { "action", "download" },
                            { "id", torrentId },
                            { "authkey", AuthKey },
                            { "torrent_pass", PassKey }
                        };

                        var downloadVolumeFactor = torrent.Value<string>("FreeleechType")?.ToUpperInvariant() switch
                        {
                            "FREELEECH" => 0,
                            "HALF LEECH" => 0.5,
                            _ => 1
                        };

                        bool.TryParse((string)torrent["GoldenPopcorn"], out var golden);
                        bool.TryParse((string)torrent["Scene"], out var scene);
                        bool.TryParse((string)torrent["Checked"], out var check);

                        if (configGoldenPopcornOnly && !golden)
                        {
                            continue; //Skip release if user only wants GoldenPopcorn
                        }

                        if (configSceneOnly && !scene)
                        {
                            continue; //Skip release if user only wants Scene
                        }

                        if (configCheckedOnly && !check)
                        {
                            continue; //Skip release if user only wants Checked
                        }

                        if (configFreeOnly && downloadVolumeFactor != 0.0)
                        {
                            continue;
                        }

                        var link = new Uri($"{SearchUrl}?{releaseLinkQuery.GetQueryString()}");
                        var seeders = int.Parse((string)torrent["Seeders"]);
                        var details = new Uri($"{SearchUrl}?id={WebUtility.UrlEncode(movieGroupId)}&torrentid={WebUtility.UrlEncode(torrentId)}");
                        var size = long.Parse((string)torrent["Size"]);
                        var grabs = long.Parse((string)torrent["Snatched"]);
                        var publishDate = DateTime.ParseExact((string)torrent["UploadTime"], "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToLocalTime();
                        var leechers = int.Parse((string)torrent["Leechers"]);

                        var categories = new List<int> { TorznabCatType.Movies.ID };

                        if (releaseName != null && seasonRegex.Match(releaseName).Success)
                        {
                            categories.Add(TorznabCatType.TV.ID);
                        }

                        var release = new ReleaseInfo
                        {
                            Guid = link,
                            Link = link,
                            Details = details,
                            Title = releaseName,
                            Description = $"Title: {movieTitle}",
                            Year = int.Parse(year),
                            Category = categories,
                            Poster = poster,
                            Imdb = movieImdbId,
                            Size = size,
                            Grabs = grabs,
                            Seeders = seeders,
                            Peers = seeders + leechers,
                            PublishDate = publishDate,
                            DownloadVolumeFactor = downloadVolumeFactor,
                            UploadVolumeFactor = 1,
                            MinimumRatio = 1,
                            MinimumSeedTime = 345600
                        };

                        var titleTags = new List<string>();
                        var quality = (string)torrent["Quality"];
                        var container = (string)torrent["Container"];
                        var codec = (string)torrent["Codec"];
                        var resolution = (string)torrent["Resolution"];
                        var source = (string)torrent["Source"];
                        var otherTags = (string)torrent["RemasterTitle"];

                        if (year != null)
                        {
                            release.Description += $"<br>\nYear: {year}";
                        }

                        if (quality != null)
                        {
                            release.Description += $"<br>\nQuality: {quality}";
                        }

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

                        if (otherTags != null)
                        {
                            titleTags.Add(otherTags);
                        }

                        if (configData.AddAttributesToTitle.Value && titleTags.Any())
                        {
                            release.Title += " [" + string.Join(" / ", titleTags) + "]";
                        }

                        releases.Add(release);
                    }
                }
            }
            catch (Exception ex)
            {
                OnParseError(results.ContentString, ex);
            }

            return releases;
        }
    }
}
