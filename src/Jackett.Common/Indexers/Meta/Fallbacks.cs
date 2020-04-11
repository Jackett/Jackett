using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;

namespace Jackett.Common.Indexers.Meta
{
    public interface IFallbackStrategy
    {
        Task<IEnumerable<TorznabQuery>> FallbackQueries();
    }

    public interface IFallbackStrategyProvider
    {
        IEnumerable<IFallbackStrategy> FallbackStrategiesForQuery(TorznabQuery query);
    }

    public class NoFallbackStrategy : IFallbackStrategy
    {
        public Task<IEnumerable<TorznabQuery>> FallbackQueries() => Task.FromResult<IEnumerable<TorznabQuery>>(new List<TorznabQuery>());
    }

    public class NoFallbackStrategyProvider : IFallbackStrategyProvider
    {
        public IEnumerable<IFallbackStrategy> FallbackStrategiesForQuery(TorznabQuery query) { yield return new NoFallbackStrategy(); }
    }

    public class ImdbFallbackStrategy : IFallbackStrategy
    {
        public ImdbFallbackStrategy(IImdbResolver resolver, TorznabQuery query)
        {
            this.resolver = resolver;
            this.query = query;
        }

        public async Task<IEnumerable<TorznabQuery>> FallbackQueries()
        {
            if (string.IsNullOrEmpty(query.ImdbID))
                return Enumerable.Empty<TorznabQuery>();
            var title = (await resolver.MovieForId(query.ImdbID)).Title;
            return title != null ? new[] { query.CreateFallback(title) } : Enumerable.Empty<TorznabQuery>();
        }

        private readonly IImdbResolver resolver;
        private readonly TorznabQuery query;
    }

    public class ImdbFallbackStrategyProvider : IFallbackStrategyProvider
    {
        public ImdbFallbackStrategyProvider(IImdbResolver resolver) => this.resolver = resolver;

        public IEnumerable<IFallbackStrategy> FallbackStrategiesForQuery(TorznabQuery query)
        {
            var result = new List<IFallbackStrategy>();
            if (!query.IsImdbQuery)
                result.Add(new NoFallbackStrategy());
            else
                result.Add(new ImdbFallbackStrategy(resolver, query));
            return result;
        }

        private readonly IImdbResolver resolver;
    }
}
