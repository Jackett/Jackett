using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Jackett.Common.Models.Config;
using MongoDB.Driver.Core.Configuration;

namespace Jackett.Common.Models.DTO
{
    [DataContract]
    public class ServerConfig
    {
        [DataMember]
        public IEnumerable<string> notices { get; set; }
        [DataMember]
        public int port { get; set; }
        [DataMember]
        public bool external { get; set; }
        [DataMember]
        public string local_bind_address { get; set; }
        [DataMember]
        public bool cors { get; set; }
        [DataMember]
        public string api_key { get; set; }
        [DataMember]
        public string blackholedir { get; set; }
        [DataMember]
        public bool updatedisabled { get; set; }
        [DataMember]
        public bool prerelease { get; set; }
        [DataMember]
        public string password { get; set; }
        [DataMember]
        public bool logging { get; set; }
        [DataMember]
        public string basepathoverride { get; set; }
        [DataMember]
        public string baseurloverride { get; set; }
        [DataMember]
        public CacheType cache_type { get; set; }
        [DataMember]
        public string cache_connection_string { get; set; }
        [DataMember]
        public long cache_ttl { get; set; }
        [DataMember]
        public long cache_max_results_per_indexer { get; set; }
        [DataMember]
        public string flaresolverrurl { get; set; }
        [DataMember]
        public int flaresolverr_maxtimeout { get; set; }
        [DataMember]
        public string omdbkey { get; set; }
        [DataMember]
        public string omdburl { get; set; }
        [DataMember]
        public string app_version { get; set; }
        [DataMember]
        public bool can_run_netcore { get; set; }

        [DataMember]
        public ProxyType proxy_type { get; set; }
        [DataMember]
        public string proxy_url { get; set; }
        [DataMember]
        public int? proxy_port { get; set; }
        [DataMember]
        public string proxy_username { get; set; }
        [DataMember]
        public string proxy_password { get; set; }

        public ServerConfig()
        {
            notices = Array.Empty<string>();
        }

        public ServerConfig(IEnumerable<string> notices, Models.Config.ServerConfig config, string version, bool canRunNetCore)
        {
            this.notices = notices;
            port = config.Port;
            external = config.AllowExternal;
            local_bind_address = config.LocalBindAddress;
            cors = config.AllowCORS;
            api_key = config.APIKey;
            blackholedir = config.BlackholeDir;
            updatedisabled = config.UpdateDisabled;
            prerelease = config.UpdatePrerelease;
            password = string.IsNullOrEmpty(config.AdminPassword) ? string.Empty : config.AdminPassword.Substring(0, 10);
            logging = config.RuntimeSettings.TracingEnabled;
            basepathoverride = config.BasePathOverride;
            baseurloverride = config.BaseUrlOverride;
            cache_type = config.CacheType;
            cache_connection_string = config.CacheConnectionString;
            cache_ttl = config.CacheTtl;
            cache_max_results_per_indexer = config.CacheMaxResultsPerIndexer;
            flaresolverrurl = config.FlareSolverrUrl;
            flaresolverr_maxtimeout = config.FlareSolverrMaxTimeout;
            omdbkey = config.OmdbApiKey;
            omdburl = config.OmdbApiUrl;
            app_version = version;
            can_run_netcore = canRunNetCore;

            proxy_type = config.ProxyType;
            proxy_url = config.ProxyUrl;
            proxy_port = config.ProxyPort;
            proxy_username = config.ProxyUsername;
            proxy_password = config.ProxyPassword;
        }
    }
}
