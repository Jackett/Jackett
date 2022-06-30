using System;
using System.Reflection;
using System.Runtime.Versioning;

namespace Jackett.Common.Utils
{
    public static class DotNetCoreUtil
    {
        public static bool IsRunningOnDotNetCore
        {
            get
            {
                var runningOnDotNetCore = false;

                try
                {
                    runningOnDotNetCore = Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName.IndexOf("Core", StringComparison.OrdinalIgnoreCase) >= 0;
                }
                catch
                {
                    //Issue only appears to occur for small number of users on Mono
                    runningOnDotNetCore = false;
                }

                return runningOnDotNetCore;
            }
        }
    }
}
