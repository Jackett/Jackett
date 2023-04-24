using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataShazbat : ConfigurationDataBasicLogin
    {
        public SingleSelectConfigurationItem ShowPagesFetchLimit { get; private set; }

        public DisplayInfoConfigurationItem ShowPagesFetchLimitInstructions { get; private set; }

        public ConfigurationDataShazbat()
        {
            ShowPagesFetchLimit = new SingleSelectConfigurationItem(
                "Show Pages Fetch Limit (sub-requests when searching)",
                new Dictionary<string, string>
                {
                    {"1", "1"},
                    {"2", "2"},
                    {"3", "3"},
                    {"4", "4"},
                    {"5", "5"}
                })
            { Value = "2" };

            ShowPagesFetchLimitInstructions = new DisplayInfoConfigurationItem("Show Pages Fetch Limit Warning", "Higher values may risk your account being flagged for bot activity when used with automation software such as Sonarr.");
        }
    }
}
