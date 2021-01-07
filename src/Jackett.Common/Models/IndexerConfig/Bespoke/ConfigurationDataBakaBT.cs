using System.Diagnostics.CodeAnalysis;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataBakaBT : ConfigurationDataBasicLogin
    {
        public BoolItem AddRomajiTitle { get; private set; }
        public BoolItem AppendSeason { get; private set; }

        public ConfigurationDataBakaBT(string instructionMessageOptional = null)
            : base(instructionMessageOptional)
        {
            AddRomajiTitle = new BoolItem() { Name = "Add releases for Romaji Title", Value = true };
            AppendSeason = new BoolItem() { Name = "Append Season for Sonarr Compatibility", Value = false };
        }
    }
}
