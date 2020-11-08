using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
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
    [ExcludeFromCodeCoverage]
    public class PassThePopcorn : BaseWebIndexer
    {
        private static string SearchUrl => "https://passthepopcorn.me/torrents.php";
        private string AuthKey { get; set; }
        private string PassKey { get; set; }

        // TODO: merge ConfigurationDataAPILoginWithUserAndPasskeyAndFilter class with with ConfigurationDataUserPasskey
        private new ConfigurationDataAPILoginWithUserAndPasskeyAndFilter configData
        {
            get => (ConfigurationDataAPILoginWithUserAndPasskeyAndFilter)base.configData;
            set => base.configData = value;
        }

        public PassThePopcorn(IIndexerConfigurationService configService, Utils.Clients.WebClient c, Logger l, IProtectionService ps)
            : base(id: "passthepopcorn",
                   name: "PassThePopcorn",
                   description: "PassThePopcorn is a Private site for MOVIES / TV",
                   link: "https://passthepopcorn.me/",
                   caps: new TorznabCapabilities
                   {
                       TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                       },
                       MovieSearchParams = new List<MovieSearchParam>
                       {
                           MovieSearchParam.Q, MovieSearchParam.ImdbId
                       }
                   },
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
            var queryCollection = new NameValueCollection { { "json", "noredirect" } };

            if (!string.IsNullOrEmpty(query.ImdbID))
                queryCollection.Add("searchstr", query.ImdbID);
            else if (!string.IsNullOrEmpty(query.GetQueryString()))
                queryCollection.Add("searchstr", query.GetQueryString());
            if (configFreeOnly)
                queryCollection.Add("freetorrent", "1");

            movieListSearchUrl += "?" + queryCollection.GetQueryString();

            var authHeaders = new Dictionary<string, string>
            {
                { "ApiUser", configData.User.Value },
                { "ApiKey", configData.Key.Value }
            };

            var results = await RequestWithCookiesAndRetryAsync(movieListSearchUrl, headers: authHeaders);
            if (results.IsRedirect) // untested
                results = await RequestWithCookiesAndRetryAsync(movieListSearchUrl, headers: authHeaders);
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
                            {"action", "download"},
                            {"id", torrentId},
                            {"authkey", AuthKey},
                            {"torrent_pass", PassKey}
                        };
                        var free = !(torrent["FreeleechType"] is null);

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
                        var link = new Uri($"{SearchUrl}?{releaseLinkQuery.GetQueryString()}");
                        var seeders = int.Parse((string)torrent["Seeders"]);
                        var details = new Uri($"{SearchUrl}?id={WebUtility.UrlEncode(movieGroupId)}&torrentid={WebUtility.UrlEncode(torrentId)}");
                        var size = long.Parse((string)torrent["Size"]);
                        var grabs = long.Parse((string)torrent["Snatched"]);
                        var publishDate = DateTime.ParseExact((string)torrent["UploadTime"],
                                                              "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal)
                                                  .ToLocalTime();
                        var leechers = int.Parse((string)torrent["Leechers"]);
                        var release = new ReleaseInfo
                        {
                            Title = releaseName,
                            Description = $"Title: {movieTitle}",
                            Poster = poster,
                            Imdb = movieImdbId,
                            Details = details,
                            Size = size,
                            Grabs = grabs,
                            Seeders = seeders,
                            Peers = seeders + leechers,
                            PublishDate = publishDate,
                            Link = link,
                            Guid = link,
                            MinimumRatio = 1,
                            MinimumSeedTime = 345600,
                            DownloadVolumeFactor = free ? 0 : 1,
                            UploadVolumeFactor = 1,
                            Category = new List<int> { TorznabCatType.Movies.ID }
                        };


                        var titleTags = new List<string>();
                        var quality = (string)torrent["Quality"];
                        var container = (string)torrent["Container"];
                        var codec = (string)torrent["Codec"];
                        var resolution = (string)torrent["Resolution"];
                        var source = (string)torrent["Source"];
                        var otherTags = (string)torrent["RemasterTitle"];

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

                        if (otherTags != null)
                            titleTags.Add(otherTags);

                        if (titleTags.Any())
                            release.Title += " [" + string.Join(" / ", titleTags) + "]";

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
