using System;
using System.IO;

namespace Jackett.Common.Models.Config
{
    public class RuntimeSettings
    {
        public bool TracingEnabled { get; set; }

        public bool LogRequests { get; set; }

        public string ClientOverride { get; set; }

        public string ProxyConnection { get; set; }

        public bool? DoSSLFix { get; set; }

        public bool? IgnoreSslErrors { get; set; }

        public string CustomDataFolder { get; set; }

        public string BasePath { get; set; }

        public bool NoRestart { get; set; }

        public string CustomLogFileName { get; set; }

        public string PIDFile { get; set; }

        public bool NoUpdates { get; set; }


        public string DataFolder
        {
            get
            {                
                if (!string.IsNullOrWhiteSpace(this.CustomDataFolder))
                {
                    return this.CustomDataFolder;
                }

                if (System.Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Jackett");
                }
                else
                {
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Jackett");
                }
            }
        }
    }
}
