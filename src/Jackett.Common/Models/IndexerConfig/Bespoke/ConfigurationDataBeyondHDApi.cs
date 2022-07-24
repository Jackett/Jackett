using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataBeyondHDApi : ConfigurationData
    {
        public StringConfigurationItem ApiKey { get; private set; }
        public StringConfigurationItem RSSKey { get; private set; }
        public DisplayInfoConfigurationItem Instructions { get; private set; }
        public BoolConfigurationItem AddHybridFeaturesToTitle { get; private set; }

        public ConfigurationDataBeyondHDApi(string instructionMessageOptional)
        {
            ApiKey = new StringConfigurationItem("API Key");
            RSSKey = new StringConfigurationItem("RSS Key");
            Instructions = new DisplayInfoConfigurationItem("", instructionMessageOptional);
            AddHybridFeaturesToTitle = new BoolConfigurationItem("Include DV/HDR10 in title when release has multiple HDR formats.");
        }
    }
}
