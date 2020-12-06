using System;
using System.Diagnostics;
using System.Reflection;

namespace Jackett.Common.Utils
{
    public static class EnvironmentUtil
    {

        public static string JackettVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            return $"v{fvi.ProductVersion}";
        }

        public static bool IsWindows => Environment.OSVersion.Platform == PlatformID.Win32NT;

    }
}
