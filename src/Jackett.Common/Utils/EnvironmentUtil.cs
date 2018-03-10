using System;
using System.Reflection;

namespace Jackett.Common.Utils
{
    public static class EnvironmentUtil
    {
    
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
