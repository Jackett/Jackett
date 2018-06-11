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
                    var currentAssembly = Assembly.GetExecutingAssembly();

                    bool aspNetCorePresent = new StackTrace().GetFrames()
                                                .Select(x => x.GetMethod().ReflectedType.Assembly).Distinct()
                                                .Where(x => x.GetReferencedAssemblies().Any(y => y.FullName == currentAssembly.FullName))
                                                .Where(x => x.ManifestModule.Name == "JackettConsole.exe").Select(x => x.CustomAttributes)
                                                .FirstOrDefault()
                                                .Where(x => x.AttributeType.Assembly.FullName.StartsWith("Microsoft.AspNetCore", StringComparison.OrdinalIgnoreCase))
                                                .Any();

                    runningOwin = !aspNetCorePresent;
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
