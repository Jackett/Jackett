using CsQuery;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Net.Http.Headers;
using Jackett.Models;
using Jackett.Utils;
using NLog;
using Jackett.Services;
using Jackett.Utils.Clients;
using Jackett.Models.IndexerConfig;
using System.Collections.Specialized;

namespace Jackett.Indexers
{
    /// <summary>
    /// Provider for WiHD Private Tracker
    /// Created by JigSaw
    /// </summary>
    public class WiHD : BaseIndexer, IIndexer
    {
        private string LoginUrl { get { return SiteLink + "login"; } }
        private string LoginCheckUrl { get { return SiteLink + "login_check"; } }
        private string SearchUrl { get { return SiteLink + "ajaxmetasearch/"; } }
        private string DownloadUrl { get { return SiteLink + "torrents/download/"; } }
        private string GuidUrl { get { return SiteLink + "torrents/view/"; } }

        public WiHD(IIndexerManagerService i, IWebClient w, Logger l, IProtectionService ps)
            : base(
                name: "WiHD",
                description: "Your World in High Definition -- Provided by JigSaw",
                link: "http://world-in-hd.net/",
                caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                manager: i,
                client: w,
                logger: l,
                p: ps,
                downloadBase: "http://world-in-hd.net/torrents/download/",
                configData: new ConfigurationDataBasicLogin())
        {
            // No category mapping, assuming all is TV
        }

        /// <summary>
        /// Configure our WiHD Provider
        /// </summary>
        /// <param name="configJson">Our params in Json</param>
        /// <returns>Configuration state</returns>
        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson) {
            var incomingConfig = new ConfigurationDataBasicLogin();
            incomingConfig.LoadValuesFromJson(configJson);

            // Getting login form to retrieve CSRF token
            var loginPage = await webclient.GetString(new Utils.Clients.WebRequest() {
                Url = LoginUrl
            });

            // Retrieving our CSRF token
            CQ loginPageDom = loginPage.Content;
            var csrfToken = loginPageDom["input[name=\"_csrf_token\"]"].Last();

            // Building login form data
            var pairs = new Dictionary<string, string> {
                { "_csrf_token", csrfToken.Attr("value") },
                { "_username", incomingConfig.Username.Value },
                { "_password", incomingConfig.Password.Value },
                { "_remember_me", "on" },
                { "_submit", "" }
            };

            // Do the login
            var request = new Utils.Clients.WebRequest(){
                Cookies = loginPage.Cookies,
                PostData = pairs,
                Referer = LoginUrl,
                Type = RequestType.POST,
                Url = LoginUrl
            };

            // Perform loggin
            var response = await RequestLoginAndFollowRedirect(LoginCheckUrl, pairs, loginPage.Cookies, true, null, null);

            // Test if we are logged in
            await ConfigureIfOK(response.Cookies, response.Content != null && response.Content.Contains("/logout"), () => {
                // Oops, unable to login
                throw new ExceptionWithConfigData("Failed to login", configData);
            });

            return IndexerConfigurationStatus.RequiresTesting;
        }

        /// <summary>
        /// Execute our search query
        /// </summary>
        /// <param name="query">Query</param>
        /// <returns>Releases</returns>
        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query) {
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();

            var searchUrl = SearchUrl;

            if (!string.IsNullOrWhiteSpace(searchString)) {
                // Add search string
                searchUrl += searchString;
            }
            else {
                // If no search string provided, use default (for test)
                searchUrl += "fear%20the%20walking%20dead";
            }

            var response = await RequestStringWithCookiesAndRetry(searchUrl);

            try
            {
                // Parsing JSON data
                var json = JArray.Parse(response.Content);
                // Loop on all results
                foreach (var r in json)
                {
                    // We just want torrent
                    if(r["rawtype"].ToString() == "torrent")
                    {
                        var release = new ReleaseInfo();

                        // Populating release info
                        release.Title = (string)r["title"];
                        release.Guid = (Uri)new Uri(SiteLink.TrimEnd('/') + r["link"]);
                        release.Link = (Uri)new Uri(DownloadUrl + r["link"].ToString().Substring(14));
                        release.Size = null;
                        release.Description = null;
                        release.Seeders = null;
                        release.Peers = null;
                        release.MinimumRatio = 1;
                        release.MinimumSeedTime = 172800;
                        release.Category = TorznabCatType.TV.ID;
                        release.Comments = null;
                        
                        // Adding release to list
                        releases.Add(release);
                    }
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
