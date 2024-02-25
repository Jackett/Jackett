using System;
using System.Runtime.InteropServices;
using Jackett.Common.Models.Config;
using Jackett.Test.TestHelpers;
using NUnit.Framework;

namespace Jackett.Test.Server.Services
{
    [TestFixture]
    internal class RuntimeSettingsTests : TestBase
    {
        [Test]
        public void Default_data_folder_is_correct()
        {
            var runtimeSettings = new RuntimeSettings();
            var dataFolder = runtimeSettings.DataFolder;

            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                var expectedUnixPath = Environment.GetEnvironmentVariable("HOME") +
                                       (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                                           ? "/Library/Application Support"
                                           : "/.config") + "/Jackett";
                Assert.AreEqual(expectedUnixPath, dataFolder);
            }
            else
            {
                var expectedWindowsPath = Environment.ExpandEnvironmentVariables("%ProgramData%") + "\\Jackett";
                Assert.AreEqual(expectedWindowsPath, dataFolder);
            }
        }
    }
}
