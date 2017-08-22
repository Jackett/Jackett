using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Jackett.Models.IndexerConfig;
using System.Collections.Specialized;
using Newtonsoft.Json;
using System.Globalization;

namespace Jackett.Indexers
{
    public class Hardbay : BaseWebIndexer
    {
        private string SearchUrl { get { return SiteLink + "api/v1/torrents"; } }
        private string LoginUrl { get { return SiteLink + "api/v1/auth"; } }

        new ConfigurationDataBasicLogin configData
        {
            get { return (ConfigurationDataBasicLogin)base.configData; }
            set { base.configData = value; }
        }

        public Hardbay(IIndexerConfigurationService configService, IWebClient w, Logger l, IProtectionService ps)
            : base(name: "Hardbay",
                description: null,
                link: "https://hardbay.club/",
                caps: new TorznabCapabilities(),
                configService: configService,
                client: w,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLogin())
        {
            Encoding = Encoding.GetEncoding("UTF-8");
            Language = "en-us";
            Type = "private";

            AddCategoryMapping(1, TorznabCatType.Audio);
            AddCategoryMapping(2, TorznabCatType.AudioLossless);
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var queryCollection = new NameValueCollection();

            queryCollection.Add("username", configData.Username.Value);
            queryCollection.Add("password", configData.Password.Value);

            var loginUrl = LoginUrl + "?" + queryCollection.GetQueryString();
            var loginResult = await RequestStringWithCookies(loginUrl, null, SiteLink);

            await ConfigureIfOK(loginResult.Cookies, loginResult.Content.Contains("\"user\""), () =>
            {
                throw new ExceptionWithConfigData(loginResult.Content, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();
            var queryCollection = new NameValueCollection();
            var searchString = query.GetQueryString();
            var searchUrl = SearchUrl;

            queryCollection.Add("extendedSearch", "false");
            queryCollection.Add("hideOld", "false");
            queryCollection.Add("index", "0");
            queryCollection.Add("limit", "100");
            queryCollection.Add("order", "desc");
            queryCollection.Add("page", "search");
            queryCollection.Add("searchText", searchString);
            queryCollection.Add("sort", "d");

            /*foreach (var cat in MapTorznabCapsToTrackers(query))
                queryCollection.Add("categories[]", cat);
            */

            searchUrl += "?" + queryCollection.GetQueryString();
            var results = await RequestStringWithCookies(searchUrl, null, SiteLink);

            try
            {
                //var json = JArray.Parse(results.Content);
                dynamic json = JsonConvert.DeserializeObject<dynamic>(results.Content);
                if (json != null) // no results
                { 
                    foreach (var row in json)
                    {
                        var release = new ReleaseInfo();
                        var descriptions = new List<string>();
                        var tags = new List<string>();

                        release.MinimumRatio = 0.5;
                        release.MinimumSeedTime = 0;
                        release.Title = row.name;
                        release.Category = new List<int> { TorznabCatType.Audio.ID };
                        release.Size = row.size;
                        release.Seeders = row.seeders;
                        release.Peers = row.leechers + release.Seeders;
                        release.PublishDate = DateTime.ParseExact(row.added.ToString() + " +01:00", "yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);
                        release.Files = row.numfiles;
                        release.Grabs = row.times_completed;

                        release.Comments = new Uri(SiteLink + "torrent/" + row.id.ToString() + "/");
                        release.Link = new Uri(SiteLink + "api/v1/torrents/download/" + row.id.ToString());

                        if (row.frileech == 1)
                            release.DownloadVolumeFactor = 0;
                        else
                            release.DownloadVolumeFactor = 0.33;
                        release.UploadVolumeFactor = 1;

                        if ((int)row.p2p == 1)
                            tags.Add("P2P");
                        if ((int)row.pack == 1)
                            tags.Add("Pack");
                        if ((int)row.reqid != 0)
                            tags.Add("Archive");
                        if ((int)row.flac != 0)
                        {
                            tags.Add("FLAC");
                            release.Category = new List<int> { TorznabCatType.AudioLossless.ID };
                        }

                        if (tags.Count > 0)
                            descriptions.Add("Tags: " + string.Join(", ", tags));

                        release.Description = string.Join("<br>\n", descriptions);

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