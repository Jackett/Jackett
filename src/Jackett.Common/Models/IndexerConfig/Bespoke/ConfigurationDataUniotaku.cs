using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataUniotaku : ConfigurationDataBasicLogin
    {
        public BoolConfigurationItem Freeleech { get; private set; }
        public SingleSelectConfigurationItem SortBy { get; private set; }

        public ConfigurationDataUniotaku()
        {
            Freeleech = new BoolConfigurationItem("Search freeleech only") { Value = false };

            SortBy = new SingleSelectConfigurationItem("Sort By", new Dictionary<string, string>
            {
                {"0", "created"},
                {"3", "seeders"},
                {"9", "size"},
                {"1", "title"}
            })
            { Value = "0" };
        }

    }
}
