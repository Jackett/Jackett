using Jackett.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Utils
{
    public static class TorznabCapsUtil
    {
        public static TorznabCapabilities CreateDefaultTorznabTVCaps()
        {
            var caps = new TorznabCapabilities();
            caps.SearchAvailable = true;
            caps.TVSearchAvailable = true;
            caps.SupportsTVRageSearch = false;
            caps.Categories.AddRange(new[] {
                TorznabCategory.TV,
                TorznabCategory.TVSD,
                TorznabCategory.TVHD
            });
            return caps;
        }

    }
}
