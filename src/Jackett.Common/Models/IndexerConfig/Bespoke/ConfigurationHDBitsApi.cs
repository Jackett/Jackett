using System.Collections.Generic;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    internal class ConfigurationDataHDBitsApi : ConfigurationDataUserPasskey
    {
        public CheckboxItem Codecs { get; private set; }
        public CheckboxItem Mediums { get; private set; }

        public ConfigurationDataHDBitsApi() : base()
        {
            Codecs = new CheckboxItem(new Dictionary<string, string>()
                {
                    {"1", "H.264"},
                    {"5", "HEVC"},
                    {"2", "MPEG-2"},
                    {"3", "VC-1"},
                    {"6", "VP9"},
                    {"4", "XviD"}
                })
            { Name = "Codec", Values = new string[] { "1", "5", "2", "3", "6", "4" } };

            Mediums = new CheckboxItem(new Dictionary<string, string>()
                {
                    {"1", "Blu-ray/HD DVD"},
                    {"4", "Capture"},
                    {"3", "Encode"},
                    {"5", "Remux"},
                    {"6", "WEB-DL"}
                })
            { Name = "Medium", Values = new string[] { "1", "4", "3", "5", "6" } };
        }
    }
}
