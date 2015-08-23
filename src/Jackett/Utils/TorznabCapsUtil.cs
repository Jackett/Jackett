using Jackett.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Jackett.Utils
{
    public class TorznabUtil
    {
        static Regex reduceSpacesRegex = new Regex("\\s{2,}", RegexOptions.Compiled);

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

        public static IEnumerable<ReleaseInfo> FilterResultsToTitle(IEnumerable<ReleaseInfo> results, string name, int year)
        {
            if (string.IsNullOrWhiteSpace(name))
                return results;

            name = CleanTitle(name);
            var filteredResults = new List<ReleaseInfo>();
            foreach (var result in results)
            {
                if (result.Title == null)
                    continue;
                if (CleanTitle(result.Title).Contains(name) &&
                    (year ==0 || result.Title.Contains(year.ToString())))
                {
                    filteredResults.Add(result);
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
                    result => !result.Imdb.HasValue || result.Imdb.Value == 0 || ("tt" + result.Imdb.Value).Equals(imdb));
        } 

        private static string CleanTitle(string title)
        {
            title = title.Replace(':', ' ').Replace('.', ' ').Replace('-', ' ').Replace('_', ' ').Replace('+', ' ');
            return reduceSpacesRegex.Replace(title, " ").ToLowerInvariant();
        }
    }
}
