using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;

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
        public IEnumerable<IFallbackStrategy> FallbackStrategiesForQuery(TorznabQuery query) => (new NoFallbackStrategy()).ToEnumerable();
    }

    public class ImdbFallbackStrategy : IFallbackStrategy
    {
        public ImdbFallbackStrategy(IImdbResolver resolver, TorznabQuery query)
        {
            this.resolver = resolver;
            titles = null;
            this.query = query;
        }

        public async Task<IEnumerable<TorznabQuery>> FallbackQueries()
        {
            titles ??= (await resolver.MovieForId(query.ImdbID.ToNonNull())).Title?.ToEnumerable() ?? Enumerable.Empty<string>();
            return titles.Select(query.CreateFallback);
        }

        private readonly IImdbResolver resolver;
        private IEnumerable<string> titles;
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
