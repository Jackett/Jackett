using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;

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
            try {
                var normalizedImdbId = String.Concat(query.ImdbID.Where(c => char.IsDigit(c)));
                imdbId = Int64.Parse(normalizedImdbId);
            } catch {
            }

            IEnumerable<ReleaseInfo> perfectResults;
            IEnumerable<ReleaseInfo> wrongResults;

            if (imdbId != null) {
                var resultsWithImdbId = results.Where(r => r.Imdb != null);
                wrongResults = resultsWithImdbId.Where(r => r.Imdb != imdbId);
                perfectResults = resultsWithImdbId.Where(r => r.Imdb == imdbId);
            } else {
                wrongResults = new ReleaseInfo[] { };
                perfectResults = new ReleaseInfo[] { };
            }

            var remainingResults = results.Except(wrongResults).Except(perfectResults);

            var titles = (await resolver.MovieForId(query.ImdbID.ToNonNull())).Title.ToEnumerable();
            var strippedTitles = titles.Select(t => RemoveSpecialChars(t));
            var normalizedTitles = strippedTitles.SelectMany(t => GenerateTitleVariants(t));

            var titleFilteredResults = remainingResults.Where(r => {
                // TODO Make it possible to configure case insensitivity
                var containsAnyTitle = normalizedTitles.Select(t => r.Title.ToLowerInvariant().Contains(t.ToLowerInvariant()));
                var isProbablyValidResult = containsAnyTitle.Any(b => b);
                return isProbablyValidResult;
            });

            var filteredResults = perfectResults.Concat(titleFilteredResults).Distinct();
            return filteredResults;
        }

        private static string RemoveSpecialChars(string title)
        {
            // TODO improve character replacement with invalid chars
            return title.Replace(":", "");
        }

        private static IEnumerable<string> GenerateTitleVariants(string title)
        {
            var delimiterVariants = new char[] { '.', '_' };
            var result = new List<string>();
            var replacedTitles = delimiterVariants.Select(c => title.Replace(' ', c));

            result.Add(title);
            result.AddRange(replacedTitles);

            return result;
        }

        private IImdbResolver resolver;
        private TorznabQuery query;
    }

    public class NoFilter : IResultFilter
    {
        public Task<IEnumerable<ReleaseInfo>> FilterResults(IEnumerable<ReleaseInfo> results)
        {
            return Task.FromResult(results);
        }
    }

    public class NoResultFilterProvider : IResultFilterProvider
    {
        public IEnumerable<IResultFilter> FiltersForQuery(TorznabQuery query)
        {
            return (new NoFilter()).ToEnumerable();
        }
    }

    public class ImdbTitleResultFilterProvider : IResultFilterProvider
    {
        public ImdbTitleResultFilterProvider(IImdbResolver resolver)
        {
            this.resolver = resolver;
        }

        public IEnumerable<IResultFilter> FiltersForQuery(TorznabQuery query)
        {
            IResultFilter filter = null;
            if (!query.IsImdbQuery)
                filter = new NoFilter();
            else
                filter = new ImdbTitleResultFilter(resolver, query);
            return filter.ToEnumerable();
        }

        private IImdbResolver resolver;
    }
}
