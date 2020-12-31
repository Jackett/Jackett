using System.Diagnostics.CodeAnalysis;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataAniDub : ConfigurationDataBasicLogin
    {
        public BoolItem StripRussianTitle { get; private set; }

        public ConfigurationDataAniDub() : base()
        {
            StripRussianTitle = new BoolItem
            {
                Name = "Strip Russian Title",
                Value = true
            };
        }
    }
}
