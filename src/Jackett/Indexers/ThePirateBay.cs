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
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Jackett.Models.IndexerConfig;

namespace Jackett.Indexers
{
    public class ThePirateBay : BaseIndexer, IIndexer
    {
        readonly static string defaultSiteLink = "https://thepiratebay.mom/";

        private Uri BaseUri {
            get { return new Uri (configData.Url.Value); }
            set { configData.Url.Value = value.ToString (); }
        }

        private string SearchUrl { get { return BaseUri + "search/{0}/0/99/208,205"; } }
        private string RecentUrl { get { return BaseUri + "recent"; } }

        new ConfigurationDataUrl configData {
            get { return (ConfigurationDataUrl)base.configData; }
            set { base.configData = value; }
        }

        public ThePirateBay (IIndexerManagerService i, Logger l, IWebClient wc, IProtectionService ps)
            : base (name: "The Pirate Bay",
                description: "The worlds largest bittorrent indexer",
                link: defaultSiteLink,
                caps: TorznabUtil.CreateDefaultTorznabTVCaps (),
                manager: i,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationDataUrl (defaultSiteLink))
        {
        }

        public async Task<IndexerConfigurationStatus> ApplyConfiguration (JToken configJson)
        {
            configData.LoadValuesFromJson (configJson);
            var releases = await PerformQuery (new TorznabQuery ());

            await ConfigureIfOK (string.Empty, releases.Count () > 0, () => {
                throw new Exception ("Could not find releases from this URL");
            });

            return IndexerConfigurationStatus.Completed;
        }

        // Override to load legacy config format
        public override void LoadFromSavedConfiguration (JToken jsonConfig)
        {
            if (jsonConfig is JObject) {
                BaseUri = new Uri (jsonConfig.Value<string> ("base_url"));
                SaveConfig ();
                IsConfigured = true;
                return;
            }

            base.LoadFromSavedConfiguration (jsonConfig);
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery (TorznabQuery query)
        {
            var releases = new List<ReleaseInfo> ();
            var queryStr = HttpUtility.UrlEncode (query.GetQueryString ());
            var episodeSearchUrl = string.IsNullOrWhiteSpace (queryStr) ? RecentUrl : string.Format (SearchUrl, queryStr);
            var response = await RequestStringWithCookiesAndRetry (episodeSearchUrl, string.Empty);

            try {
                CQ dom = response.Content;

                var rows = dom ["#searchResult > tbody > tr"];
                foreach (var row in rows) {
                    if (row.ChildElements.Count () < 1)
                        continue;

                    var release = new ReleaseInfo ();
                    CQ qRow = row.Cq ();
                    CQ qLink = qRow.Find (".detName > .detLink").First ();



                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;
                    release.Title = qLink.Text ().Trim ();
                    release.Description = release.Title;
                    release.Comments = new Uri (BaseUri + qLink.Attr ("href").TrimStart ('/'));
                    release.Guid = release.Comments;

                    var downloadCol = row.ChildElements.ElementAt (1).Cq ().Children ("a");
                    logger.Warn (downloadCol);
                    release.MagnetUri = new Uri (downloadCol.Attr ("href"));
                    release.InfoHash = release.MagnetUri.ToString ().Split (':') [3].Split ('&') [0];

                    var descString = qRow.Find (".detDesc").Text ().Trim ();
                    var descParts = descString.Split (',');
                    var timeString = descParts [0].Split (' ') [1];

                    //time shit
                    if (timeString.Contains (":")) {
                        string s = timeString.Replace ("\u00a0", "_");
                        DateTime dt = DateTime.Now;
                        bool success = DateTime.TryParseExact (s, "MM-dd_HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt);
                        release.PublishDate = dt;
                    } else {
                        string s = timeString.Replace ("\u00a0", "_");
                        DateTime dt = DateTime.Now;
                        bool success = DateTime.TryParseExact (s, "MM-dd_yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt);
                        release.PublishDate = dt;
                    }
                    release.Size = ReleaseInfo.GetBytes (descParts [1]);
                    release.Seeders = ParseUtil.CoerceInt (row.ChildElements.ElementAt (2).Cq ().Text ());
                    release.Peers = ParseUtil.CoerceInt (row.ChildElements.ElementAt (3).Cq ().Text ()) + release.Seeders;
                    releases.Add (release);
                }
            } catch (Exception ex) {
                OnParseError (response.Content, ex);
            }
            return releases.ToArray ();
        }

        public override Task<byte []> Download (Uri link)
        {
            throw new NotImplementedException ();
        }
    }
}