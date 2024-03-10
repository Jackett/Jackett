using System.Diagnostics.CodeAnalysis;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataSpeedCD : ConfigurationDataBasicLogin
    {
        public BoolConfigurationItem Freeleech { get; set; }
        public BoolConfigurationItem ExcludeArchives { get; set; }
        public DisplayInfoConfigurationItem AccountActivity { get; private set; }

        public ConfigurationDataSpeedCD(string instructionMessageOptional = null)
            : base(instructionMessageOptional)
        {
            Freeleech = new BoolConfigurationItem("Search freeleech only") { Value = false };
            ExcludeArchives = new BoolConfigurationItem("Exclude torrents with RAR files") { Value = false };
            AccountActivity = new DisplayInfoConfigurationItem("Account Inactivity", "Accounts not being used for 3 months will be removed to make room for active members.");
        }
    }
}
