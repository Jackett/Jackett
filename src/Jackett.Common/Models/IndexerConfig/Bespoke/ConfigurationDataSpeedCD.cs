using System.Diagnostics.CodeAnalysis;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataSpeedCD : ConfigurationDataBasicLogin
    {
        public BoolConfigurationItem Freeleech { get; set; }
        public BoolConfigurationItem ExcludeArchives { get; set; }

        public ConfigurationDataSpeedCD(string instructionMessageOptional = null)
            : base(instructionMessageOptional)
        {
            Freeleech = new BoolConfigurationItem("Search freeleech only") { Value = false };
            ExcludeArchives = new BoolConfigurationItem("Exclude torrents with RAR files") { Value = false };
        }
    }
}
