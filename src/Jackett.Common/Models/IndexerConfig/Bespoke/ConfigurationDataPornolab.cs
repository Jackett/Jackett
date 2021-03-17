using System.Diagnostics.CodeAnalysis;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataPornolab : ConfigurationDataCaptchaLogin
    {
        public BoolConfigurationItem StripRussianLetters { get; private set; }

        public ConfigurationDataPornolab()
            => StripRussianLetters = new BoolConfigurationItem("Strip Russian Letters") { Value = false };
    }
}
