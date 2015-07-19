using Jackett.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models
{
    public class TorznabQuery
    {
        public string QueryType { get; private set; }
        public string[] Categories { get; private set; }
        public int Extended { get; private set; }
        public string ApiKey { get; private set; }
        public int Limit { get; private set; }
        public int Offset { get; private set; }
        public int RageID { get; private set; }

        public int Season { get; private set; }
        public string Episode { get; private set; }
        public string SearchTerm { get; private set; }
        public string SanitizedSearchTerm { get; private set; }

        public string GetEpisodeSearchString()
        {
            if (Season == 0)
                return string.Empty;

            string episodeString;
            DateTime showDate;
            if (DateTime.TryParseExact(string.Format("{0} {1}", Season, Episode), "yyyy MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out showDate))
                episodeString = showDate.ToString("yyyy.MM.dd");
            else if (string.IsNullOrEmpty(Episode))
                episodeString = string.Format("S{0:00}", Season);
            else
                episodeString = string.Format("S{0:00}E{1:00}", Season, ParseUtil.CoerceInt(Episode));

            return episodeString;
        }

        static string SanitizeSearchTerm(string title)
        {
            char[] arr = title.ToCharArray();

            arr = Array.FindAll<char>(arr, c => (char.IsLetterOrDigit(c)
                                              || char.IsWhiteSpace(c)
                                              || c == '-'
                                              || c == '.'
                                              ));
            title = new string(arr);
            return title;
        }

        public static TorznabQuery FromHttpQuery(NameValueCollection query)
        {

            //{t=tvsearch&cat=5030%2c5040&extended=1&apikey=test&offset=0&limit=100&rid=24493&season=5&ep=1}
            var q = new TorznabQuery();
            q.QueryType = query["t"];

            if (query["q"] == null)
            {
                q.SearchTerm = string.Empty;
                q.SanitizedSearchTerm = string.Empty;
            }
            else
            {
                q.SearchTerm = query["q"];
                q.SanitizedSearchTerm = SanitizeSearchTerm(q.SearchTerm);
            }

            if (query["cat"] != null)
            {
                q.Categories = query["cat"].Split(',');
            }

            if (query["extended"] != null)
            {
                q.Extended = ParseUtil.CoerceInt(query["extended"]);
            }
            q.ApiKey = query["apikey"];
            if (query["limit"] != null)
            {
                q.Limit = ParseUtil.CoerceInt(query["limit"]);
            }
            if (query["offset"] != null)
            {
                q.Offset = ParseUtil.CoerceInt(query["offset"]);
            }

            int rageId;
            if (int.TryParse(query["rid"], out rageId))
            {
                q.RageID = rageId;
            }

            int season;
            if (int.TryParse(query["season"], out season))
            {
                q.Season = season;
            }

            q.Episode = query["ep"];

            return q;
        }
    }
}
