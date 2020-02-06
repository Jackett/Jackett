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
            _resolver = resolver;
            _query = query;
        }

        public async Task<IEnumerable<ReleaseInfo>> FilterResults(IEnumerable<ReleaseInfo> results)
        {
            long? imdbId = null;
            try
            {
                var normalizedImdbId = string.Concat(_query.ImdbID.Where(c => char.IsDigit(c)));
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
                wrongResults = new ReleaseInfo[]
                {
                };
                perfectResults = new ReleaseInfo[]
                {
                };
            }

            var remainingResults = results.Except(wrongResults).Except(perfectResults);
            var titles = (await _resolver.MovieForId(_query.ImdbID.ToNonNull())).Title?.ToEnumerable() ??
                         Enumerable.Empty<string>();
            var strippedTitles = titles.Select(t => RemoveSpecialChars(t));
            var normalizedTitles = strippedTitles.SelectMany(t => GenerateTitleVariants(t));
            var titleFilteredResults = remainingResults.Where(
                r =>
                {
                    // TODO Make it possible to configure case insensitivity
                    var containsAnyTitle =
                        normalizedTitles.Select(t => r.Title.ToLowerInvariant().Contains(t.ToLowerInvariant()));
                    var isProbablyValidResult = containsAnyTitle.Any(b => b);
                    return isProbablyValidResult;
                });
            var filteredResults = perfectResults.Concat(titleFilteredResults).Distinct();
            return filteredResults;
        }

        // TODO improve character replacement with invalid chars
        private static string RemoveSpecialChars(string title) => title.Replace(":", "");

        private static IEnumerable<string> GenerateTitleVariants(string title)
        {
            var delimiterVariants = new[]
            {
                '.',
                '_'
            };
            var result = new List<string>();
            var replacedTitles = delimiterVariants.Select(c => title.Replace(' ', c));
            result.Add(title);
            result.AddRange(replacedTitles);
            return result;
        }

        private readonly IImdbResolver _resolver;
        private readonly TorznabQuery _query;
    }

    public class NoFilter : IResultFilter
    {
        public Task<IEnumerable<ReleaseInfo>> FilterResults(IEnumerable<ReleaseInfo> results) => Task.FromResult(results);
    }

    public class NoResultFilterProvider : IResultFilterProvider
    {
        public IEnumerable<IResultFilter> FiltersForQuery(TorznabQuery query) => (new NoFilter()).ToEnumerable();
    }

    public class ImdbTitleResultFilterProvider : IResultFilterProvider
    {
        public ImdbTitleResultFilterProvider(IImdbResolver resolver) => _resolver = resolver;

        public IEnumerable<IResultFilter> FiltersForQuery(TorznabQuery query)
        {
            var filter = !query.IsImdbQuery ? new NoFilter() : (IResultFilter)new ImdbTitleResultFilter(_resolver, query);
            return filter.ToEnumerable();
        }

        private readonly IImdbResolver _resolver;
    }
}
