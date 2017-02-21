using CsQuery;
using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Text;
using Jackett.Models.IndexerConfig;

namespace Jackett.Indexers
{
    public class PassThePopcorn : BaseIndexer, IIndexer
    {
        private string LoginUrl { get { return "https://passthepopcorn.me/ajax.php?action=login"; } }
        private string indexUrl { get { return "https://passthepopcorn.me/ajax.php?action=login"; } }
        private string SearchUrl { get { return "https://passthepopcorn.me/torrents.php"; } }
        private string DetailURL { get { return "https://passthepopcorn.me/torrents.php?torrentid="; } }
        private string AuthKey { get; set; } 
        new ConfigurationDataBasicLoginWithFilterAndPasskey configData
        {
            get { return (ConfigurationDataBasicLoginWithFilterAndPasskey)base.configData; }
            set { base.configData = value; }
        }

        public PassThePopcorn(IIndexerManagerService i, Logger l, IWebClient c, IProtectionService ps)
            : base(name: "PassThePopcorn",
                description: "PassThePopcorn",
                link: "https://passthepopcorn.me/",
                caps: new TorznabCapabilities(),
                manager: i,
                client: c,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLoginWithFilterAndPasskey(@"Enter filter options below to restrict search results. 
                                                                        Separate options with a space if using more than one option.<br>Filter options available:
                                                                        <br><code>GoldenPopcorn</code><br><code>Scene</code><br><code>Checked</code>"))
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";

            TorznabCaps.SupportsImdbSearch = true;

            AddCategoryMapping(1, TorznabCatType.Movies);
            AddCategoryMapping(1, TorznabCatType.MoviesForeign);
            AddCategoryMapping(1, TorznabCatType.MoviesOther);
            AddCategoryMapping(1, TorznabCatType.MoviesSD);
            AddCategoryMapping(1, TorznabCatType.MoviesHD);
            AddCategoryMapping(1, TorznabCatType.Movies3D);
            AddCategoryMapping(1, TorznabCatType.MoviesBluRay);
            AddCategoryMapping(1, TorznabCatType.MoviesDVD);
            AddCategoryMapping(1, TorznabCatType.MoviesWEBDL);
        }

        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            await DoLogin();

            return IndexerConfigurationStatus.RequiresTesting;
        }

        private async Task DoLogin()
        {
            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "passkey", configData.Passkey.Value },
                { "keeplogged", "1" },
                { "login", "Log In!" }
            };

            var response = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, indexUrl, SiteLink);
            JObject js_response = JObject.Parse(response.Content);
            await ConfigureIfOK(response.Cookies, response.Content != null && (string)js_response["Result"] != "Error", () =>
            {
                // Landing page wil have "Result":"Error" if log in fails
                string errorMessage = (string)js_response["Message"];
                throw new ExceptionWithConfigData(errorMessage, configData);
            });   
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            await DoLogin();

            var releases = new List<ReleaseInfo>();
            bool configGoldenPopcornOnly = configData.FilterString.Value.ToLowerInvariant().Contains("goldenpopcorn");
            bool configSceneOnly = configData.FilterString.Value.ToLowerInvariant().Contains("scene");
            bool configCheckedOnly = configData.FilterString.Value.ToLowerInvariant().Contains("checked");
            string movieListSearchUrl;

            if (!string.IsNullOrEmpty(query.ImdbID))
            {
                movieListSearchUrl = string.Format("{0}?json=noredirect&searchstr={1}", SearchUrl, HttpUtility.UrlEncode(query.ImdbID));
            }
            else if(!string.IsNullOrEmpty(query.GetQueryString()))
            {
                movieListSearchUrl = string.Format("{0}?json=noredirect&searchstr={1}", SearchUrl, HttpUtility.UrlEncode(query.GetQueryString()));
            }
            else
            {
                movieListSearchUrl = string.Format("{0}?json=noredirect", SearchUrl);
            }

            var results = await RequestStringWithCookiesAndRetry(movieListSearchUrl);
            try
            {
                //Iterate over the releases for each movie
                JObject js_results = JObject.Parse(results.Content);
                foreach (var movie in js_results["Movies"])
                {
                    string movie_title = (string) movie["Title"];
                    long movie_imdbid = long.Parse((string)movie["ImdbId"]);
                    string movie_groupid = (string)movie["GroupId"];
                    foreach (var torrent in movie["Torrents"])
                    {
                        var release = new ReleaseInfo();
                        release.Title = movie_title;
                        release.Description = release.Title;
                        release.Imdb = movie_imdbid;
                        release.Comments = new Uri(string.Format("{0}?id={1}", SearchUrl, HttpUtility.UrlEncode(movie_groupid)));
                        release.Guid = release.Comments;
                        release.Size = long.Parse((string)torrent["Size"]);
                        release.Seeders = int.Parse((string)torrent["Seeders"]);
                        release.Peers = int.Parse((string)torrent["Leechers"]);
                        release.PublishDate = DateTime.ParseExact((string)torrent["UploadTime"], "yyyy-MM-dd HH:mm:ss", 
                                                CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToLocalTime();
                        release.Link = new Uri(string.Format("{0}?action=download&id={1}&authkey={2}&torrent_pass={3}", 
                                                SearchUrl, HttpUtility.UrlEncode((string)torrent["Id"]), HttpUtility.UrlEncode(AuthKey), HttpUtility.UrlEncode(configData.Passkey.Value)));
                        release.MinimumRatio = 1;
                        release.MinimumSeedTime = 345600;
                        release.Category = new List<int> { 2000 };

                        bool golden, scene, check;
                        bool.TryParse((string)torrent["GoldenPopcorn"], out golden);
                        bool.TryParse((string)torrent["Scene"], out scene);
                        bool.TryParse((string)torrent["Checked"], out check);


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
