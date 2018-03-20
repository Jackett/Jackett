using System.Collections.Generic;
using Jackett.Common.Models.Config;

namespace Jackett.Common.Models.DTO
{
    public class ServerConfig
    {
        public IEnumerable<string> notices { get; set; }
        public int port { get; set; }
        public bool external { get; set; }
        public string api_key { get; set; }
        public string blackholedir { get; set; }
        public bool updatedisabled { get; set; }
        public bool prerelease { get; set; }
        public string password { get; set; }
        public bool logging { get; set; }
        public string basepathoverride { get; set; }
        public string omdbkey { get; set; }
        public string app_version { get; set; }

        public ProxyType proxy_type { get; set; }
        public string proxy_url { get; set; }
        public int? proxy_port { get; set; }
        public string proxy_username { get; set; }
        public string proxy_password { get; set; }

        public ServerConfig()
        {
            notices = new string[0];
        }

        public ServerConfig(IEnumerable<string> notices, Models.Config.ServerConfig config, string version)
        {
            this.notices = notices;
            port = config.Port;
            external = config.AllowExternal;
            api_key = config.APIKey;
            blackholedir = config.BlackholeDir;
            updatedisabled = config.UpdateDisabled;
            prerelease = config.UpdatePrerelease;
            password = string.IsNullOrEmpty(config.AdminPassword) ? string.Empty : config.AdminPassword.Substring(0, 10);
            logging = config.RuntimeSettings.TracingEnabled;
            basepathoverride = config.BasePathOverride;
            omdbkey = config.OmdbApiKey;
            app_version = version;

            proxy_type = config.ProxyType;
            proxy_url = config.ProxyUrl;
            proxy_port = config.ProxyPort;
            proxy_username = config.ProxyUsername;
            proxy_password = config.ProxyPassword;
        }
    }
}
