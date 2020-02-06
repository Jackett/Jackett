using System.Text.RegularExpressions;
using Jackett.Common.Models;

namespace Jackett.Common.Utils
{
    public class TorznabUtil
    {
        private static readonly Regex s_ReduceSpacesRegex = new Regex("\\s{2,}", RegexOptions.Compiled);
        private static readonly Regex s_FindYearRegex = new Regex(@"(?<=\[|\(|\s)(\d{4})(?=\]|\)|\s)", RegexOptions.Compiled);

        public static TorznabCapabilities CreateDefaultTorznabTVCaps()
        {
            var caps = new TorznabCapabilities();
            caps.Categories.AddRange(
                new[]
                {
                    TorznabCatType.TV,
                    TorznabCatType.TVSD,
                    TorznabCatType.TVHD
                });
            return caps;
        }

        private static int GetYearFromTitle(string title)
        {
            var match = s_FindYearRegex.Match(title);
            if (match.Success)
            {
                var year = ParseUtil.CoerceInt(match.Value);
                if (year > 1850 && year < 2100)
                    return year;
            }

            return 0;
        }
    }
}
