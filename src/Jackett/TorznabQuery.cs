using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett
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
        public int Episode { get; private set; }

        public static TorznabQuery FromHttpQuery(NameValueCollection query)
        {

            //{t=tvsearch&cat=5030%2c5040&extended=1&apikey=test&offset=0&limit=100&rid=24493&season=5&ep=1}
            var q = new TorznabQuery();
            q.QueryType = query["t"];
            q.Categories = query["cat"].Split(',');
            q.Extended = int.Parse(query["extended"]);
            q.ApiKey = query["apikey"];
            q.Limit = int.Parse(query["limit"]);
            q.Offset = int.Parse(query["offset"]);

            int temp;
            if (int.TryParse(query["rid"], out temp))
                q.RageID = temp;
            if (int.TryParse(query["season"], out temp))
                q.Season = temp;
            if (int.TryParse(query["ep"], out temp))
                q.Episode = int.Parse(query["ep"]);

            return q;
        }
    }
}
