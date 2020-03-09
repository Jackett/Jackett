using System;
using System.Collections.Generic;

namespace Jackett.Common.Models
{
    public class CachedQueryResult
    {
        private readonly List<ReleaseInfo> results;

        public CachedQueryResult(string query, List<ReleaseInfo> results)
        {
            this.results = results;
            Created = DateTime.Now;
            Query = query;
        }

        public IReadOnlyList<ReleaseInfo> Results => results.AsReadOnly();

        public DateTime Created { get; }

        public string Query { get; }
    }
}
