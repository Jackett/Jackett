using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;

namespace Jackett.Common.Indexers.Meta
{
    public interface IResultFilter
    {
        Task<IEnumerable<ReleaseInfo>> FilterResults(IEnumerable<ReleaseInfo> results);
    }

    public interface IResultFilterProvider
    {
        IEnumerable<IResultFilter> FiltersForQuery(TorznabQuery query);
    }

    public class ImdbTitleResultFilter : IResultFilter
    {
        public ImdbTitleResultFilter(IImdbResolver resolver, TorznabQuery query)
        {
            this.resolver = resolver;
            this.query = query;
        }

        public async Task<IEnumerable<ReleaseInfo>> FilterResults(IEnumerable<ReleaseInfo> results)
        {
            long? imdbId = null;
            try
            {
                // Convert from try/catch to long.TryParse since we're not handling the failure
                var normalizedImdbId = string.Concat(query.ImdbID.Where(char.IsDigit));
                imdbId = long.Parse(normalizedImdbId);
            }
            catch
            {
            }

            IEnumerable<ReleaseInfo> perfectResults;
            IEnumerable<ReleaseInfo> wrongResults;

            if (imdbId != null)
            {
                var resultsWithImdbId = results.Where(r => r.Imdb != null);
                wrongResults = resultsWithImdbId.Where(r => r.Imdb != imdbId);
                perfectResults = resultsWithImdbId.Where(r => r.Imdb == imdbId);
            }
            else
            {
                wrongResults = new ReleaseInfo[] { };
                perfectResults = new ReleaseInfo[] { };
            }

            var remainingResults = results.Except(wrongResults).Except(perfectResults);

            if (string.IsNullOrEmpty(query.ImdbID))
                return perfectResults;
            var title = (await resolver.MovieForId(query.ImdbID)).Title;
            if (title == null)
                return perfectResults;

            var normalizedTitles = GenerateTitleVariants(RemoveSpecialChars(title));

            // TODO Make it possible to configure case insensitivity
            var titleFilteredResults = remainingResults.Where(
                r => normalizedTitles.Any(t => r.Title.IndexOf(t, StringComparison.InvariantCultureIgnoreCase) >= 0));
            return perfectResults.Union(titleFilteredResults);
        }

        // TODO improve character replacement with invalid chars
        private static string RemoveSpecialChars(string title) => title.Replace(":", "");

        private static IEnumerable<string> GenerateTitleVariants(string title)
        {
            var delimiterVariants = new[] { '.', '_' };
            var result = new List<string>();
            var replacedTitles = delimiterVariants.Select(c => title.Replace(' ', c));

            result.Add(title);
            result.AddRange(replacedTitles);

            return result;
        }

        private readonly IImdbResolver resolver;
        private readonly TorznabQuery query;
    }

    public class NoFilter : IResultFilter
    {
        public Task<IEnumerable<ReleaseInfo>> FilterResults(IEnumerable<ReleaseInfo> results) => Task.FromResult(results);
    }

    public class NoResultFilterProvider : IResultFilterProvider
    {
        public IEnumerable<IResultFilter> FiltersForQuery(TorznabQuery query) { yield return new NoFilter(); }
    }

    public class ImdbTitleResultFilterProvider : IResultFilterProvider
    {
        public ImdbTitleResultFilterProvider(IImdbResolver resolver) => this.resolver = resolver;

        public IEnumerable<IResultFilter> FiltersForQuery(TorznabQuery query)
        {
            yield return !query.IsImdbQuery ? (IResultFilter)new NoFilter() : new ImdbTitleResultFilter(resolver, query);
        }

        private readonly IImdbResolver resolver;
    }
}
