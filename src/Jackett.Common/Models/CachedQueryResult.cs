using System;
using System.Collections.Generic;

namespace Jackett.Common.Models
{
    public class CachedQueryResult
    {
        private readonly List<ReleaseInfo> _results;

        public CachedQueryResult(string query, List<ReleaseInfo> results)
        {
            _results = results;
            Created = DateTime.Now;
            Query = query;
        }

        public IReadOnlyList<ReleaseInfo> Results => _results.AsReadOnly();

        public DateTime Created { get; }

        public string Query { get; }
    }
}
