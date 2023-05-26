using System.Diagnostics.CodeAnalysis;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataBakaBT : ConfigurationDataBasicLogin
    {
        public BoolConfigurationItem FreeleechOnly { get; private set; }
        public BoolConfigurationItem AddRomajiTitle { get; private set; }
        public BoolConfigurationItem AppendSeason { get; private set; }

        public ConfigurationDataBakaBT(string instructionMessageOptional = null)
            : base(instructionMessageOptional)
        {
            FreeleechOnly = new BoolConfigurationItem("Show freeleech only") { Value = false };
            AddRomajiTitle = new BoolConfigurationItem("Add releases for Romaji Title") { Value = true };
            AppendSeason = new BoolConfigurationItem("Append Season for Sonarr Compatibility") { Value = false };
        }
    }
}
