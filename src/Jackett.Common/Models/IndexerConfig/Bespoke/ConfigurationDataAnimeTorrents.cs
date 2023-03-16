using System.Diagnostics.CodeAnalysis;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataAnimeTorrents : ConfigurationDataBasicLogin
    {
        public BoolConfigurationItem FreeleechOnly { get; private set; }
        public BoolConfigurationItem DownloadableOnly { get; private set; }

        public ConfigurationDataAnimeTorrents()
        {
            FreeleechOnly = new BoolConfigurationItem("Show freeleech only") { Value = false };
            DownloadableOnly = new BoolConfigurationItem("Search downloadable torrents only (enable this only if your account class is Newbie)") { Value = false };
        }
    }
}
