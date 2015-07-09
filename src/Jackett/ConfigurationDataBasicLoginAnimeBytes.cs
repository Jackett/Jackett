using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett
{
    public class ConfigurationDataBasicLoginAnimeBytes : ConfigurationDataBasicLogin 
    {
        public BoolItem IncludeRaw { get; private set; }
        public DisplayItem RageIdWarning { get; private set; }
        public DisplayItem DateWarning { get; private set; }

        public ConfigurationDataBasicLoginAnimeBytes(): base()
        {
            IncludeRaw = new BoolItem() { Name = "IncludeRaw",  Value = false };
            RageIdWarning = new DisplayItem("Ensure rageid lookup is disabled in Sonarr for this tracker.") { Name = "RageWarning" };
            DateWarning = new DisplayItem("This tracker does not supply upload dates so they are based off year of release.") { Name = "DateWarning" };
        }

        public override Item[] GetItems()
        {
            return new Item[] { Username, Password, IncludeRaw, RageIdWarning, DateWarning };
        }
    }
}
