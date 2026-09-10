using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataBigBbs : ConfigurationDataBasicLogin
    {
        public BoolConfigurationItem Freeleech { get; private set; }

        public SingleSelectConfigurationItem Sort { get; private set; }

        public SingleSelectConfigurationItem Type { get; private set; }

        public DisplayInfoConfigurationItem ProfileInfo { get; private set; }

        public ConfigurationDataBigBbs()
        {
            Freeleech = new BoolConfigurationItem("Filter freeleech only") { Value = false };

            Sort = new SingleSelectConfigurationItem(
                "Sort requested from site",
                new Dictionary<string, string> { { "added", "created" }, { "seeders", "seeders" }, { "size", "size" } })
            {
                Value = "added"
            };

            Type = new SingleSelectConfigurationItem(
                "Order requested from site", new Dictionary<string, string> { { "desc", "desc" }, { "asc", "asc" } })
            {
                Value = "desc"
            };

            ProfileInfo = new DisplayInfoConfigurationItem(
                "Layout",
                "<ul><li>Only the English Classic profile is supported.</li>" +
                "<li>Make sure to set the <b>Torrent Listing (Lista torrentów)</b> option in your profile to <b>Classic (Klasyczny)</b></li>" +
                "<li>And set the <b>Language (Dil)</b> to <b>English</b></li>" +
                "<li>Using the <i>Modern</i> theme will prevent results, and using <i>Polski</i> will prevent upload dates.</li></ul>");
        }
    }
}
