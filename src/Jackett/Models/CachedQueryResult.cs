using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models
{
    public class CachedQueryResult
    {
        private List<ReleaseInfo> results;
        private DateTime created;
        private string query;

        public CachedQueryResult(string query, List<ReleaseInfo> results){
            this.results = results;
            created = DateTime.Now;
            this.query = query;
        }

        public IReadOnlyList<ReleaseInfo> Results
        {
            get { return results.AsReadOnly(); }
        }

        public DateTime Created
        {
            get { return created; }
        }

        public string Query
        {
            get { return query; }
        }
    }
}
