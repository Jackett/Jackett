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
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers
{
    public class PassThePopcorn : BaseWebIndexer
    {
        private string LoginUrl => "https://passthepopcorn.me/ajax.php?action=login";
        private string indexUrl => "https://passthepopcorn.me/ajax.php?action=login";
        private string SearchUrl => "https://passthepopcorn.me/torrents.php";
        private string DetailURL => "https://passthepopcorn.me/torrents.php?torrentid=";
        private string AuthKey { get; set; }

        private new ConfigurationDataAPILoginWithUserAndPasskeyAndFilter configData
        {
            get => (ConfigurationDataAPILoginWithUserAndPasskeyAndFilter)base.configData;
            set => base.configData = value;
        }

        public PassThePopcorn(IIndexerConfigurationService configService, WebClient c, Logger l, IProtectionService ps) :
            base(
                "PassThePopcorn", description: "PassThePopcorn is a Private site for MOVIES / TV",
                link: "https://passthepopcorn.me/", caps: new TorznabCapabilities(), configService: configService, client: c,
                logger: l, p: ps, configData: new ConfigurationDataAPILoginWithUserAndPasskeyAndFilter(
                    @"Enter filter options below to restrict search results.
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
                if (results.Count() == 0)
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
            if (queryCollection.Count > 0)
                movieListSearchUrl += $"?{queryCollection.GetQueryString()}";
            var authHeaders = new Dictionary<string, string>
            {
                {"ApiUser", configData.User.Value}, {"ApiKey", configData.Key.Value}
            };
            var results = await RequestStringWithCookiesAndRetryAsync(movieListSearchUrl, null, null, authHeaders);
            if (results.IsRedirect) // untested
                results = await RequestStringWithCookiesAndRetryAsync(movieListSearchUrl, null, null, authHeaders);
            try
            {
                //Iterate over the releases for each movie
                var jsResults = JObject.Parse(results.Content);
                foreach (var movie in jsResults["Movies"])
                {
                    var movieTitle = (string)movie["Title"];
                    var year = (string)movie["Year"];
                    var movieImdbidStr = (string)movie["ImdbId"];
                    var coverStr = (string)movie["Cover"];
                    Uri coverUri = null;
                    if (!string.IsNullOrEmpty(coverStr))
                        coverUri = new Uri(coverStr);
                    long? movieImdbid = null;
                    if (!string.IsNullOrEmpty(movieImdbidStr))
                        movieImdbid = long.Parse(movieImdbidStr);
                    var movieGroupid = (string)movie["GroupId"];
                    foreach (var torrent in movie["Torrents"])
                    {
                        var release = new ReleaseInfo();
                        var releaseName = (string)torrent["ReleaseName"];
                        release.Title = releaseName;
                        release.Description = string.Format("Title: {0}", movieTitle);
                        release.BannerUrl = coverUri;
                        release.Imdb = movieImdbid;
                        release.Comments = new Uri(
                            string.Format("{0}?id={1}", SearchUrl, WebUtility.UrlEncode(movieGroupid)));
                        release.Size = long.Parse((string)torrent["Size"]);
                        release.Grabs = long.Parse((string)torrent["Snatched"]);
                        release.Seeders = int.Parse((string)torrent["Seeders"]);
                        release.Peers = release.Seeders + int.Parse((string)torrent["Leechers"]);
                        release.PublishDate = DateTime.ParseExact(
                                                          (string)torrent["UploadTime"], "yyyy-MM-dd HH:mm:ss",
                                                          CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal)
                                                      .ToLocalTime();
                        release.Link = new Uri(
                            string.Format(
                                "{0}?action=download&id={1}&authkey={2}&torrent_pass={3}", SearchUrl,
                                WebUtility.UrlEncode((string)torrent["Id"]), WebUtility.UrlEncode(AuthKey),
                                WebUtility.UrlEncode(configData.Passkey.Value)));
                        release.Guid = release.Link;
                        release.MinimumRatio = 1;
                        release.MinimumSeedTime = 345600;
                        release.DownloadVolumeFactor = 1;
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
                        var titletags = new List<string>();
                        var quality = (string)torrent["Quality"];
                        var container = (string)torrent["Container"];
                        var codec = (string)torrent["Codec"];
                        var resolution = (string)torrent["Resolution"];
                        var source = (string)torrent["Source"];
                        if (year != null)
                            release.Description += string.Format("<br>\nYear: {0}", year);
                        if (quality != null)
                            release.Description += string.Format("<br>\nQuality: {0}", quality);
                        if (resolution != null)
                        {
                            titletags.Add(resolution);
                            release.Description += string.Format("<br>\nResolution: {0}", resolution);
                        }

                        if (source != null)
                        {
                            titletags.Add(source);
                            release.Description += string.Format("<br>\nSource: {0}", source);
                        }

                        if (codec != null)
                        {
                            titletags.Add(codec);
                            release.Description += string.Format("<br>\nCodec: {0}", codec);
                        }

                        if (container != null)
                        {
                            titletags.Add(container);
                            release.Description += string.Format("<br>\nContainer: {0}", container);
                        }

                        if (scene)
                        {
                            titletags.Add("Scene");
                            release.Description += "<br>\nScene";
                        }

                        if (check)
                        {
                            titletags.Add("Checked");
                            release.Description += "<br>\nChecked";
                        }

                        if (golden)
                        {
                            titletags.Add("Golden Popcorn");
                            release.Description += "<br>\nGolden Popcorn";
                        }

                        if (titletags.Count() > 0)
                            release.Title += $" [{string.Join(" / ", titletags)}]";
                        bool.TryParse((string)torrent["FreeleechType"], out var freeleech);
                        if (freeleech)
                            release.DownloadVolumeFactor = 0;
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
