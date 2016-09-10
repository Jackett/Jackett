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
    public class Kickass : BaseIndexer, IIndexer
    {
        readonly static string defaultSiteLink = "https://kickass.cd/";

        private Uri BaseUri {
            get { return new Uri (configData.Url.Value); }
            set { configData.Url.Value = value.ToString (); }
        }

        private string SearchUrl { get { return BaseUri + "search.php?q={0}"; } }

        new ConfigurationDataUrl configData {
            get { return (ConfigurationDataUrl)base.configData; }
            set { base.configData = value; }
        }

        public Kickass (IIndexerManagerService i, Logger l, IWebClient wc, IProtectionService ps)
            : base (name: "Kickass Torrents",
                description: "Kickass Torrents",
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
            var episodeSearchUrl = string.IsNullOrWhiteSpace (queryStr) ? SearchUrl : string.Format (SearchUrl, queryStr);
            var response = await RequestStringWithCookiesAndRetry (episodeSearchUrl, string.Empty);

            try {
                CQ dom = response.Content;

                var rows = dom [".data > tbody > tr.odd"];
                foreach (var row in rows) {
                    if (row.ChildElements.Count () <= 0)
                        continue;

                    var release = new ReleaseInfo ();
                    CQ qRow = row.Cq ();
                    CQ qLink = qRow.Find (".cellMainLink");

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;
                    release.Title = qLink.Text ().Trim ();
                    release.Description = release.Title;
                    CQ qMag = qRow.Children ().ElementAt (0).ChildElements.ElementAt (0).ChildElements.ElementAt (2).Cq ().ToString ();
                    release.MagnetUri = new Uri (qMag.Attr ("href"));
                    release.Comments = new Uri (BaseUri + release.MagnetUri.ToString ());
                    release.Guid = release.Comments;
                    release.InfoHash = release.MagnetUri.ToString ().Split (':') [3].Split ('&') [0];

                    var sizeString = qRow.Children ().ElementAt (1).InnerText.Trim ();
                    var timeString = qRow.Children ().ElementAt (2).InnerText.Trim ();
                    var seedString = qRow.Children ().ElementAt (3).InnerText.Trim ();
                    var peerString = qRow.Children ().ElementAt (4).InnerText.Trim ();

                    //time shit
                    if (timeString.Contains ("Today")) {
                        string s = timeString.Replace ("Today&nbsp;", "_");
                        DateTime dt;
                        bool success = DateTime.TryParseExact (s, "_HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt);
                        //logger.Warn ("Is Parsing Successful? {0}", success);
                        release.PublishDate = dt;
                        //logger.Error ("1");
                        //working

                    } else if (timeString.Contains ("Y-day")) {
                        string s = timeString.Replace ("Y-day&nbsp;", "_");
                        DateTime dt;
                        bool success = DateTime.TryParseExact (s, "_HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt);
                        //logger.Warn ("Is Parsing Successful? {0} " + dt, success);
                        release.PublishDate = dt.AddDays (-1);
                        //logger.Error ("2");
                        //working

                    } else if (timeString.Contains (':')) {
                        string s = timeString.Replace ("&nbsp;", "_");
                        //logger.Warn (s);
                        DateTime dt;
                        bool success = DateTime.TryParseExact (s, "MM-dd_HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt);
                        //logger.Warn ("Is Parsing Successful? {0}", success);
                        release.PublishDate = dt;
                        //logger.Error ("3");
                        //working

                    } else {
                        string s = timeString.Replace ("&nbsp;", "_");
                        //logger.Warn (s);
                        DateTime dt;
                        bool success = DateTime.TryParseExact (s, "MM-dd_yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt);
                        //logger.Warn ("Is Parsing Successful? {0}", success);
                        release.PublishDate = dt;
                        //logger.Error ("4");
                        //working

                    }
                    release.Size = ReleaseInfo.GetBytes (sizeString.Replace ("&nbsp;", " "));
                    release.Seeders = seedString.Count ();
                    release.Peers = peerString.Count ();
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