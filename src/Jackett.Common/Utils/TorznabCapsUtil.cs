using Jackett.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Jackett.Utils
{
    public class TorznabUtil
    {
        static Regex reduceSpacesRegex = new Regex("\\s{2,}", RegexOptions.Compiled);

        static Regex findYearRegex = new Regex(@"(?<=\[|\(|\s)(\d{4})(?=\]|\)|\s)", RegexOptions.Compiled);

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

        private static int GetYearFromTitle(string title)
        {
            var match = findYearRegex.Match(title);
            if (match.Success)
            {
                var year = ParseUtil.CoerceInt(match.Value);
                if (year > 1850 && year < 2100)
                {
                    return year;
                }
            }

            return 0;
        }
    }
}
