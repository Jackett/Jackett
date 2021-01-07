using System;
using System.Diagnostics;
using System.IO;
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

        public static string JackettInstallationPath()
        {
            return Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
        }

        public static string JackettExecutablePath()
        {
            return Assembly.GetEntryAssembly()?.Location;
        }

        public static bool IsWindows => Environment.OSVersion.Platform == PlatformID.Win32NT;

    }
}
