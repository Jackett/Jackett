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
            var queryCollection = new NameValueCollection();
            queryCollection.Add("json", "noredirect");

            if (!string.IsNullOrEmpty(query.ImdbID))
            {
                queryCollection.Add("searchstr", query.ImdbID);
            }
            else if (!string.IsNullOrEmpty(query.GetQueryString()))
            {
                queryCollection.Add("searchstr", query.GetQueryString());
            }

            if (configFreeOnly)
                queryCollection.Add("freetorrent", "1");

            if (queryCollection.Count > 0)
            {
                movieListSearchUrl += "?" + queryCollection.GetQueryString();
            }

            var authHeaders = new Dictionary<string, string>()
            {
                { "ApiUser", configData.User.Value },
                { "ApiKey", configData.Key.Value }
            };

            var results = await RequestStringWithCookiesAndRetry(movieListSearchUrl, null, null, authHeaders);
            if (results.IsRedirect) // untested
            {
                results = await RequestStringWithCookiesAndRetry(movieListSearchUrl, null, null, authHeaders);
            }
            try
            {
                //Iterate over the releases for each movie
                var js_results = JObject.Parse(results.Content);
                foreach (var movie in js_results["Movies"])
                {
                    var movie_title = (string)movie["Title"];
                    var Year = (string)movie["Year"];
                    var movie_imdbid_str = (string)movie["ImdbId"];
                    var coverStr = (string)movie["Cover"];
                    Uri coverUri = null;
                    if (!string.IsNullOrEmpty(coverStr))
                        coverUri = new Uri(coverStr);
                    long? movie_imdbid = null;
                    if (!string.IsNullOrEmpty(movie_imdbid_str))
                        movie_imdbid = long.Parse(movie_imdbid_str);
                    var movie_groupid = (string)movie["GroupId"];
                    foreach (var torrent in movie["Torrents"])
                    {
                        var release = new ReleaseInfo();
                        var release_name = (string)torrent["ReleaseName"];
                        release.Title = release_name;
                        release.Description = string.Format("Title: {0}", movie_title);
                        release.BannerUrl = coverUri;
                        release.Imdb = movie_imdbid;
                        release.Comments = new Uri(string.Format("{0}?id={1}", SearchUrl, WebUtility.UrlEncode(movie_groupid)));
                        release.Size = long.Parse((string)torrent["Size"]);
                        release.Grabs = long.Parse((string)torrent["Snatched"]);
                        release.Seeders = int.Parse((string)torrent["Seeders"]);
                        release.Peers = release.Seeders + int.Parse((string)torrent["Leechers"]);
                        release.PublishDate = DateTime.ParseExact((string)torrent["UploadTime"], "yyyy-MM-dd HH:mm:ss",
                                                CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToLocalTime();
                        release.Link = new Uri(string.Format("{0}?action=download&id={1}&authkey={2}&torrent_pass={3}",
                                                SearchUrl, WebUtility.UrlEncode((string)torrent["Id"]), WebUtility.UrlEncode(AuthKey), WebUtility.UrlEncode(configData.Passkey.Value)));
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

                        var titletags = new List<string>();
                        var Quality = (string)torrent["Quality"];
                        var Container = (string)torrent["Container"];
                        var Codec = (string)torrent["Codec"];
                        var Resolution = (string)torrent["Resolution"];
                        var Source = (string)torrent["Source"];

                        if (Year != null)
                        {
                            release.Description += string.Format("<br>\nYear: {0}", Year);
                        }
                        if (Quality != null)
                        {
                            release.Description += string.Format("<br>\nQuality: {0}", Quality);
                        }
                        if (Resolution != null)
                        {
                            titletags.Add(Resolution);
                            release.Description += string.Format("<br>\nResolution: {0}", Resolution);
                        }
                        if (Source != null)
                        {
                            titletags.Add(Source);
                            release.Description += string.Format("<br>\nSource: {0}", Source);
                        }
                        if (Codec != null)
                        {
                            titletags.Add(Codec);
                            release.Description += string.Format("<br>\nCodec: {0}", Codec);
                        }
                        if (Container != null)
                        {
                            titletags.Add(Container);
                            release.Description += string.Format("<br>\nContainer: {0}", Container);
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
                            release.Title += " [" + string.Join(" / ", titletags) + "]";

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
