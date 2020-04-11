using System;
using System.Reflection;

namespace Jackett.Common.Utils
{
    public static class EnvironmentUtil
    {

        public static string JackettVersion => Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString() ?? "Unknown Version";

        public static bool IsWindows => Environment.OSVersion.Platform == PlatformID.Win32NT;

    }
}
