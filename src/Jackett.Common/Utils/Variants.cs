using System;
using System.Runtime.InteropServices;

namespace Jackett.Common.Utils
{
    public class Variants
    {
        public enum JackettVariant
        {
            NotFound,
            FullFrameworkWindows,
            Mono,
            CoreWindows,
            CoreMacOs,
            CoreMacOsArm64,
            CoreLinuxAmdx64,
            CoreLinuxArm32,
            CoreLinuxArm64,
            CoreLinuxMuslAmdx64,
            CoreLinuxMuslArm32,
            CoreLinuxMuslArm64
        }

        public JackettVariant GetVariant()
        {
            if (DotNetCoreUtil.IsRunningOnDotNetCore)
            {
                //Dot Net Core
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    return JackettVariant.CoreWindows;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && RuntimeInformation.ProcessArchitecture == Architecture.X64)
                    return JackettVariant.CoreMacOs;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                    return JackettVariant.CoreMacOsArm64;
#if ISLINUXMUSL
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && RuntimeInformation.ProcessArchitecture == Architecture.X64)
                    return JackettVariant.CoreLinuxMuslAmdx64;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && RuntimeInformation.ProcessArchitecture == Architecture.Arm)
                    return JackettVariant.CoreLinuxMuslArm32;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                    return JackettVariant.CoreLinuxMuslArm64;
#else
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && RuntimeInformation.ProcessArchitecture == Architecture.X64)
                    return JackettVariant.CoreLinuxAmdx64;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && RuntimeInformation.ProcessArchitecture == Architecture.Arm)
                    return JackettVariant.CoreLinuxArm32;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                    return JackettVariant.CoreLinuxArm64;
#endif
            }
            else
            {
                //Full framework
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                    return JackettVariant.FullFrameworkWindows;
                return JackettVariant.Mono;
            }

            return JackettVariant.NotFound;
        }

        public string GetArtifactFileName(JackettVariant variant)
        {
            switch (variant)
            {
                case JackettVariant.FullFrameworkWindows:
                    return "Jackett.Binaries.Windows.zip";
                case JackettVariant.Mono:
                    return "Jackett.Binaries.Mono.tar.gz";
                case JackettVariant.CoreWindows:
                    return "Jackett.Binaries.Windows.zip";
                case JackettVariant.CoreMacOs:
                    return "Jackett.Binaries.macOS.tar.gz";
                case JackettVariant.CoreMacOsArm64:
                    return "Jackett.Binaries.macOSARM64.tar.gz";
                case JackettVariant.CoreLinuxAmdx64:
                    return "Jackett.Binaries.LinuxAMDx64.tar.gz";
                case JackettVariant.CoreLinuxArm32:
                    return "Jackett.Binaries.LinuxARM32.tar.gz";
                case JackettVariant.CoreLinuxArm64:
                    return "Jackett.Binaries.LinuxARM64.tar.gz";
                case JackettVariant.CoreLinuxMuslAmdx64:
                    return "Jackett.Binaries.LinuxMuslAMDx64.tar.gz";
                case JackettVariant.CoreLinuxMuslArm32:
                    return "Jackett.Binaries.LinuxMuslARM32.tar.gz";
                case JackettVariant.CoreLinuxMuslArm64:
                    return "Jackett.Binaries.LinuxMuslARM64.tar.gz";
                default:
                    return "";
            }
        }

        public bool IsNonWindowsDotNetCoreVariant(JackettVariant variant)
        {
            return (variant == JackettVariant.CoreMacOs || variant == JackettVariant.CoreMacOsArm64
                || variant == JackettVariant.CoreLinuxAmdx64 || variant == JackettVariant.CoreLinuxArm32
                || variant == JackettVariant.CoreLinuxArm64
                || variant == JackettVariant.CoreLinuxMuslAmdx64 || variant == JackettVariant.CoreLinuxMuslArm32
                || variant == JackettVariant.CoreLinuxMuslArm64);
        }
    }
}
