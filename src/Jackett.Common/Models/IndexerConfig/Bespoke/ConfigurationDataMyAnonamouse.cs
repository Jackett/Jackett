using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataMyAnonamouse : ConfigurationData
    {
        public StringConfigurationItem MamId { get; private set; }
        public DisplayInfoConfigurationItem MamIdHint { get; private set; }
        public SingleSelectConfigurationItem SearchType { get; private set; }
        public BoolConfigurationItem SearchInDescription { get; private set; }
        public BoolConfigurationItem SearchInSeries { get; private set; }
        public BoolConfigurationItem SearchInFilenames { get; private set; }
        public MultiSelectConfigurationItem SearchLanguages { get; private set; }
        public DisplayInfoConfigurationItem AccountActivity { get; private set; }

        public ConfigurationDataMyAnonamouse()
        {
            MamId = new StringConfigurationItem("mam_id");
            MamIdHint = new DisplayInfoConfigurationItem("mam_id instructions", "Go to your <a href=\"https://www.myanonamouse.net/preferences/index.php?view=security\" target=\"_blank\">security preferences</a> and create a new session for the IP used by the Jackett server. Then paste the resulting mam_id value into the mam_id field here.");
            SearchType = new SingleSelectConfigurationItem(
                "Search Type",
                new Dictionary<string, string>
                {
                    { "all", "All torrents" },
                    { "active", "Only active" },
                    { "fl", "Freeleech" },
                    { "fl-VIP", "Freeleech or VIP" },
                    { "VIP", "VIP torrents" },
                    { "nVIP", "Torrents not VIP" },
                })
            { Value = "all" };
            SearchInDescription = new BoolConfigurationItem("Also search text in the description") { Value = false };
            SearchInSeries = new BoolConfigurationItem("Also search text in the series") { Value = false };
            SearchInFilenames = new BoolConfigurationItem("Also search text in the filenames") { Value = false };

            SearchLanguages = new MultiSelectConfigurationItem(
                "Search Languages",
                new Dictionary<string, string>
                {
                    { "1", "English" },
                    { "17", "Afrikaans" },
                    { "32", "Arabic" },
                    { "35", "Bengali" },
                    { "51", "Bosnian" },
                    { "18", "Bulgarian" },
                    { "6", "Burmese" },
                    { "44", "Cantonese" },
                    { "19", "Catalan" },
                    { "2", "Chinese" },
                    { "49", "Croatian" },
                    { "20", "Czech" },
                    { "21", "Danish" },
                    { "22", "Dutch" },
                    { "61", "Estonian" },
                    { "39", "Farsi" },
                    { "23", "Finnish" },
                    { "36", "French" },
                    { "37", "German" },
                    { "26", "Greek" },
                    { "59", "Greek, Ancient" },
                    { "3", "Gujarati" },
                    { "27", "Hebrew" },
                    { "8", "Hindi" },
                    { "28", "Hungarian" },
                    { "63", "Icelandic" },
                    { "53", "Indonesian" },
                    { "56", "Irish" },
                    { "43", "Italian" },
                    { "38", "Japanese" },
                    { "12", "Javanese" },
                    { "5", "Kannada" },
                    { "41", "Korean" },
                    { "50", "Lithuanian" },
                    { "46", "Latin" },
                    { "62", "Latvian" },
                    { "33", "Malay" },
                    { "58", "Malayalam" },
                    { "57", "Manx" },
                    { "9", "Marathi" },
                    { "48", "Norwegian" },
                    { "45", "Polish" },
                    { "34", "Portuguese" },
                    { "52", "Brazilian Portuguese (BP)" },
                    { "14", "Punjabi" },
                    { "30", "Romanian" },
                    { "16", "Russian" },
                    { "24", "Scottish Gaelic" },
                    { "60", "Sanskrit" },
                    { "31", "Serbian" },
                    { "54", "Slovenian" },
                    { "4", "Spanish" },
                    { "55", "Castilian Spanish" },
                    { "40", "Swedish" },
                    { "29", "Tagalog" },
                    { "11", "Tamil" },
                    { "10", "Telugu" },
                    { "7", "Thai" },
                    { "42", "Turkish" },
                    { "25", "Ukrainian" },
                    { "15", "Urdu" },
                    { "13", "Vietnamese" },
                    { "47", "Other" }
                });

            AccountActivity = new DisplayInfoConfigurationItem("Account Inactivity", "To prevent your account from being disabled for inactivity, you must log in on a regular basis. You must also use your account - if you do not, your account will be disabled. If you know that you will not be able to login for an extended period of time, you can park your account in your preferences and it will not be disabled.");
        }
    }
}
