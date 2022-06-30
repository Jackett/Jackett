using System.Diagnostics.CodeAnalysis;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataBakaBT : ConfigurationDataBasicLogin
    {
        public BoolConfigurationItem AddRomajiTitle { get; private set; }
        public BoolConfigurationItem AppendSeason { get; private set; }

        public ConfigurationDataBakaBT(string instructionMessageOptional = null)
            : base(instructionMessageOptional)
        {
            AddRomajiTitle = new BoolConfigurationItem("Add releases for Romaji Title") { Value = true };
            AppendSeason = new BoolConfigurationItem("Append Season for Sonarr Compatibility") { Value = false };
        }
    }
}
