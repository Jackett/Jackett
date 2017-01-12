using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models.IndexerConfig.Bespoke
{
    class ConfigurationDataHDCity : ConfigurationDataBasicLogin
    {
        public DisplayItem FormatExplication { get; private set; }
        public DisplayItem ResultFilters { get; private set; }
        //public DisplayItem TranslateExplication { get; private set; }

        public BoolItem TVShowEnglishMode { get; private set; }
        public StringItem TVShowsFilter { get; private set; }
        public StringItem MoviesFilter { get; private set; }
        //public BoolItem TranslateMediaNamesToEnglish { get; private set; }

        public ConfigurationDataHDCity()
            : base()
        {
            FormatExplication = new DisplayItem("<b>HDCity naming formats:</b><br/>HDCity tv shows usually have this format (<i>Tv Show Name Temporada 1</i> or <i> TV Show 2x03) <br/> "+
                "sonarr and other english applications requires (<i> TV Show Name S02E15)<br/>Check <b>TV Show English Format</b> if you want jackett automatically replace the format") { Name = "Format" };
            TVShowEnglishMode = new BoolItem() { Name = "TV Show English Format", Value = false };

            ResultFilters = new DisplayItem("<b>Filters:</b><br/>If the field <i>Filters</i> is not empty, Jackett will expect a regular expression.<br/>" +
                "This regular expression will be used for filter results, for exemple maybe you want jackett only return result where the name have \"dual\" or \"subs\"<br/>"+
                "All the titles will be lower case for easiest regular expression matching.")
            { Name = "Regular expresions Filters" };

            TVShowsFilter = new StringItem { Name = "TV Show Filters", Value = "(dual|triaudio|multi)|((es|spa|cast?)( *)[ /|-]( *)(eng?|v( *)(\\.?)( *)o)( *))|((eng?|v( *)(\\.?)( *)o)( *)[ /|-]( *)(es|spa|cast?))" };
            MoviesFilter = new StringItem { Name = "Movies Filters", Value = "" };

            /*TranslateExplication = new DisplayItem("<b>Translations:</b><br/>Sonarr and Couch potato will always check for Original/English media names<br/>"+
                "Check <b>Automated Translations</b> if you want Jackett try to transform the names for better matchings")
            { Name = "Translations" };
            TranslateMediaNamesToEnglish = new BoolItem() { Name = "Automatic Translation", Value = false };*/
        }
    }
}
