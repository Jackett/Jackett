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

        public ConfigurationDataHDBitsApi()
        {
            Codecs = new MultiSelectConfigurationItem("Codec", new Dictionary<string, string>()
                {
                    {"0", "Undefined"},
                    {"1", "H.264"},
                    {"5", "HEVC"},
                    {"2", "MPEG-2"},
                    {"3", "VC-1"},
                    {"6", "VP9"},
                    {"4", "XviD"}
                })
            { Values = new[] { "0", "1", "5", "2", "3", "6", "4" } };

            Mediums = new MultiSelectConfigurationItem("Medium", new Dictionary<string, string>()
                {
                    {"0", "Undefined"},
                    {"1", "Blu-ray/HD DVD"},
                    {"4", "Capture"},
                    {"3", "Encode"},
                    {"5", "Remux"},
                    {"6", "WEB-DL"}
                })
            { Values = new[] { "0", "1", "4", "3", "5", "6" } };

            Origins = new MultiSelectConfigurationItem("Origin", new Dictionary<string, string>()
                {
                    {"0", "Undefined"},
                    {"1", "Internal"}
                })
            { Values = new[] { "0", "1" } };
        }
    }
}
