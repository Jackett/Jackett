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
                if(year>1850 && year < 2100)
                {
                    return year;
                }
            }

            return 0;
        }

        public static IEnumerable<ReleaseInfo> FilterResultsToTitle(IEnumerable<ReleaseInfo> results, string name, int imdbYear)
        {
            if (string.IsNullOrWhiteSpace(name))
                return results;

            name = CleanTitle(name);
            var filteredResults = new List<ReleaseInfo>();
            foreach (var result in results)
            {
                // don't filter results with IMDBID (will be filtered seperately)
                if (result.Imdb != null)
                {
                    filteredResults.Add(result);
                    continue;
                }

                if (result.Title == null)
                    continue;

                // Match on title
                if (CultureInfo.InvariantCulture.CompareInfo.IndexOf(CleanTitle(result.Title), name, CompareOptions.IgnoreNonSpace) >= 0)
                {
                    // Match on year
                    var titleYear = GetYearFromTitle(result.Title);
                    if (imdbYear == 0 || titleYear == 0 || titleYear == imdbYear)
                    {
                        filteredResults.Add(result);
                    }
                }
            }

            return filteredResults;
        }

        public static IEnumerable<ReleaseInfo> FilterResultsToImdb(IEnumerable<ReleaseInfo> results, string imdb)
        {
            if (string.IsNullOrWhiteSpace(imdb))
                return results;
            // Filter out releases that do have a valid imdb ID, that is not equal to the one we're searching for.
            return
                results.Where(
                    result => !result.Imdb.HasValue || result.Imdb.Value == 0 || ("tt" + result.Imdb.Value.ToString("D7")).Equals(imdb));
        } 

        private static string CleanTitle(string title)
        {
            title = title.Replace(':', ' ').Replace('.', ' ').Replace('-', ' ').Replace('_', ' ').Replace('+', ' ').Replace("'", "").Replace("[", "").Replace("]", "").Replace("(", "").Replace(")", "");
            return reduceSpacesRegex.Replace(title, " ").ToLowerInvariant();
        }
    }
}
