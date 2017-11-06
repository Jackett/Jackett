using System;
using System.Reflection;

namespace Jacket.Common
{
    public class JackettStartup
    {
        public static bool TracingEnabled
        {
            get;
            set;
        }

        public static bool LogRequests
        {
            get;
            set;
        }

        public static string ClientOverride
        {
            get;
            set;
        }

        public static string ProxyConnection
        {
            get;
            set;
        }

        public static bool? DoSSLFix
        {
            get;
            set;
        }

        public static bool? IgnoreSslErrors
        {
            get;
            set;
        }

        public static string CustomDataFolder
        {
            get;
            set;
        }

        public static string BasePath
        {
            get;
            set;
        }

        public static bool NoRestart
        {
            get;
            set;
        }

        public static string JackettVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString() ?? "Unknown Version";
            }
        }

        public static bool IsWindows
        {
            get
            {
                return Environment.OSVersion.Platform == PlatformID.Win32NT;
            }
        }

    }
}
