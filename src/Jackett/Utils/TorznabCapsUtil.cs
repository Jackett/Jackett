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
            caps.Categories.AddRange(new[] {
                TorznabCatType.TV,
                TorznabCatType.TVSD,
                TorznabCatType.TVHD
            });
            return caps;
        }
    }
}
