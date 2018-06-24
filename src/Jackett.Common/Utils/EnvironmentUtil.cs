using System;
using System.Diagnostics;
using System.Linq;
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

        public static bool IsRunningLegacyOwin
        {
            get
            {
                bool runningOwin;

                try
                {
                    runningOwin = AppDomain.CurrentDomain.GetAssemblies().Where(x => x.FullName.StartsWith("Jackett, ")).Any();
                }
                catch
                {
                    runningOwin = true;
                }

                return runningOwin;
            }
        }
    }
}
