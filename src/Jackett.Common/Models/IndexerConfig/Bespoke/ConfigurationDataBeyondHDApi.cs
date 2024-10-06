using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    public class ConfigurationDataBeyondHDApi : ConfigurationData
    {
        public StringConfigurationItem ApiKey { get; private set; }
        public StringConfigurationItem RSSKey { get; private set; }
        public DisplayInfoConfigurationItem Instructions { get; private set; }
        public BoolConfigurationItem AddHybridFeaturesToTitle { get; private set; }
        public BoolConfigurationItem FilterFreeleech { get; private set; }
        public BoolConfigurationItem FilterLimited { get; private set; }
        public BoolConfigurationItem FilterRefund { get; private set; }
        public BoolConfigurationItem FilterRewind { get; private set; }
        public MultiSelectConfigurationItem SearchTypes { get; private set; }

        public ConfigurationDataBeyondHDApi(string instructionMessageOptional)
        {
            ApiKey = new StringConfigurationItem("API Key");
            RSSKey = new StringConfigurationItem("RSS Key");
            Instructions = new DisplayInfoConfigurationItem("", instructionMessageOptional);
            AddHybridFeaturesToTitle = new BoolConfigurationItem("Include DV/HDR10 in title when release has multiple HDR formats.");
            FilterFreeleech = new BoolConfigurationItem("Filter freeleech");
            FilterLimited = new BoolConfigurationItem("Filter freeleech (limited UL)");
            FilterRefund = new BoolConfigurationItem("Filter refund");
            FilterRewind = new BoolConfigurationItem("Filter rewind");
            SearchTypes = new MultiSelectConfigurationItem("Select the types of releases that you are interested in. Leave empty for all.", new Dictionary<string, string>
                {
                    {"UHD 100", "UHD 100"},
                    {"UHD 66", "UHD 66"},
                    {"UHD 50", "UHD 50"},
                    {"UHD Remux", "UHD Remux"},
                    {"BD 50", "BD 50"},
                    {"BD 25", "BD 25"},
                    {"BD Remux", "BD Remux"},
                    {"2160p", "2160p"},
                    {"1080p", "1080p"},
                    {"1080i", "1080i"},
                    {"720p", "720p"},
                    {"576p", "576p"},
                    {"540p", "540p"},
                    {"DVD 9", "DVD 9"},
                    {"DVD 5", "DVD 5"},
                    {"DVD Remux", "DVD Remux"},
                    {"480p", "480p"},
                    {"Other", "Other"},
                })
            { Values = Array.Empty<string>() };
        }
    }
}
