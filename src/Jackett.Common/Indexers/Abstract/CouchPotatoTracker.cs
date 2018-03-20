using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers.Abstract
{
    public abstract class CouchPotatoTracker : BaseWebIndexer
    {
        protected string endpoint;
        protected string APIUrl { get { return SiteLink + endpoint; } }

        new ConfigurationDataUserPasskey configData
        {
            get { return (ConfigurationDataUserPasskey)base.configData; }
            set { base.configData = value; }
        }

        public CouchPotatoTracker(IIndexerConfigurationService configService, WebClient client, Logger logger, IProtectionService p, ConfigurationDataUserPasskey configData, string name, string description, string link, string endpoint)
            : base(name: name,
                description: description,
                link: link,
                caps: new TorznabCapabilities(),
                configService: configService,
                client: client,
                logger: logger,
                p: p,
                configData: configData
            )
        {
            this.endpoint = endpoint;
            TorznabCaps.SupportsImdbSearch = true;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            IsConfigured = true;
            SaveConfig();
            return await Task.FromResult(IndexerConfigurationStatus.RequiresTesting);
        }

        protected virtual string GetSearchString(TorznabQuery query)
        {
            // can be overriden to alter the search string
            return query.GetQueryString();
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = GetSearchString(query);

            var searchUrl = APIUrl;
            var queryCollection = new NameValueCollection();
            
            if (!string.IsNullOrEmpty(query.ImdbID))
            { 
                queryCollection.Add("imdbid", "browse");
            }
            if (searchString != null)
            {
                queryCollection.Add("search", searchString);
            }
            queryCollection.Add("passkey", configData.Passkey.Value);
            queryCollection.Add("user", configData.Username.Value);

            searchUrl += "?" + queryCollection.GetQueryString();

            var response = await RequestStringWithCookiesAndRetry(searchUrl);

            JObject json = null;
            try
            {
                json = JObject.Parse(response.Content);
            }
            catch (Exception ex)
            {
                throw new Exception("Error while parsing json: " + response.Content, ex);
            }
            var error = (string)json["error"];
            if (error != null)
                throw new Exception(error);

            if ((int)json["total_results"] == 0)
                return releases;

            try
            {
                foreach (JObject r in json["results"])
                {
                    var release = new ReleaseInfo();
                    release.Title = (string)r["release_name"];
                    release.Comments = new Uri((string)r["details_url"]);
                    release.Link = new Uri((string)r["download_url"]);
                    release.Guid = release.Link;
                    release.Imdb = ParseUtil.GetImdbID((string)r["imdb_id"]);
                    var freeleech = (bool)r["freeleech"];
                    if (freeleech)
                        release.DownloadVolumeFactor = 0;
                    else
                        release.DownloadVolumeFactor = 1;
                    release.UploadVolumeFactor = 1;
                    var type = (string)r["type"];
                    release.Category = MapTrackerCatToNewznab(type);
                    release.Size = (long?)r["size"] * 1024 * 1024;
                    release.Seeders = (int?)r["seeders"];
                    release.Peers = release.Seeders + (int?)r["leechers"];
                    release.PublishDate = DateTimeUtil.FromUnknown((string)r["publish_date"]);
                    releases.Add(release);
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
