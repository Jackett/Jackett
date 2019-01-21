using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

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
            CoreLinuxAmd64,
            CoreLinuxArm32,
            CoreLinuxArm64
        }

        public JackettVariant GetVariant()
        {
            if (DotNetCoreUtil.IsRunningOnDotNetCore)
            {
                //Dot Net Core

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return JackettVariant.CoreWindows;
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return JackettVariant.CoreMacOs;
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && RuntimeInformation.ProcessArchitecture == Architecture.X64)
                {
                    return JackettVariant.CoreLinuxAmd64;
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && RuntimeInformation.ProcessArchitecture == Architecture.Arm)
                {
                    return JackettVariant.CoreLinuxArm32;
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                {
                    return JackettVariant.CoreLinuxArm64;
                }
            }
            else
            {
                //Full framework

                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    return JackettVariant.FullFrameworkWindows;
                }
                else
                {
                    return JackettVariant.Mono;
                }
            }

            return JackettVariant.NotFound;
        }


        public string GetArtifactFileName(JackettVariant variant)
        {
            switch (variant)
            {
                case JackettVariant.FullFrameworkWindows:
                {
                    return "Jackett.Binaries.Windows.zip";
                }
                case JackettVariant.Mono:
                {
                    return "Jackett.Binaries.Mono.tar.gz";
                }
                case JackettVariant.CoreWindows:
                {
                    return ""; //Not implemented yet
                }
                case JackettVariant.CoreMacOs:
                {
                    return "Jackett.Binaries.macOS.tar.gz";
                }
                case JackettVariant.CoreLinuxAmd64:
                {
                    return "Jackett.Binaries.LinuxAMD64.tar.gz";
                }
                case JackettVariant.CoreLinuxArm32:
                {
                    return "Jackett.Binaries.LinuxARM32.tar.gz";
                }
                case JackettVariant.CoreLinuxArm64:
                {
                    return "Jackett.Binaries.LinuxARM64.tar.gz";
                }
                default:
                {
                    return "";
                }
            }
        }
    }
}
