using System;
using System.Collections.Generic;

namespace Jackett.Common.Models
{
    public class CachedQueryResult
    {
        private readonly List<ReleaseInfo> results;
        private readonly DateTime created;
        private readonly string query;

        public CachedQueryResult(string query, List<ReleaseInfo> results)
        {
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
