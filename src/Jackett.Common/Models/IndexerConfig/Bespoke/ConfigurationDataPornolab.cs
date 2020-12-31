using System.Diagnostics.CodeAnalysis;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataPornolab : ConfigurationDataCaptchaLogin
    {
        public BoolItem StripRussianLetters { get; private set; }

        public ConfigurationDataPornolab()
            => StripRussianLetters = new BoolItem() { Name = "Strip Russian Letters", Value = false };
    }
}
