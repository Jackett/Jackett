using System;
using System.Runtime.InteropServices;

namespace Jackett.Common.Utils
{
    public static class DotNetCoreUtil
    {
        public static bool IsRunningOnDotNetCore
        {
            get
            {
                bool runningOnDotNetCore;
                try
                {
                    runningOnDotNetCore =
                        RuntimeInformation.FrameworkDescription.IndexOf("core", StringComparison.OrdinalIgnoreCase) >= 0;
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
