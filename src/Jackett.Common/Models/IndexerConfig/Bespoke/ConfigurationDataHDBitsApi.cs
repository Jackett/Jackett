using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataHDBitsApi : ConfigurationDataUserPasskey
    {
        public MultiSelectConfigurationItem Codecs { get; private set; }
        public MultiSelectConfigurationItem Mediums { get; private set; }
        public MultiSelectConfigurationItem Origins { get; private set; }
        public BoolConfigurationItem FilterFreeleech { get; private set; }
        public BoolConfigurationItem UseFilenames { get; private set; }

        public ConfigurationDataHDBitsApi()
        {
            FilterFreeleech = new BoolConfigurationItem("Filter FreeLeech only") { Value = false };
            UseFilenames = new BoolConfigurationItem("Use Filenames as release titles") { Value = true };

            Codecs = new MultiSelectConfigurationItem(
                "Codec",
                new Dictionary<string, string>
                {
                    { "0", "Undefined" },
                    { "1", "H.264" },
                    { "5", "HEVC" },
                    { "2", "MPEG-2" },
                    { "3", "VC-1" },
                    { "6", "VP9" },
                    { "4", "XviD" }
                });

            Mediums = new MultiSelectConfigurationItem(
                "Medium",
                new Dictionary<string, string>
                {
                    { "0", "Undefined" },
                    { "1", "Blu-ray/HD DVD" },
                    { "4", "Capture" },
                    { "3", "Encode" },
                    { "5", "Remux" },
                    { "6", "WEB-DL" }
                });

            Origins = new MultiSelectConfigurationItem(
                "Origin",
                new Dictionary<string, string>
                {
                    { "0", "Undefined" },
                    { "1", "Internal" }
                });
        }
    }
}
