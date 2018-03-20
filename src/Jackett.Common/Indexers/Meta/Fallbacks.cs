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
        public Task<IEnumerable<TorznabQuery>> FallbackQueries()
        {
            return Task.FromResult<IEnumerable<TorznabQuery>>(new List<TorznabQuery>());
        }
    }

    public class NoFallbackStrategyProvider : IFallbackStrategyProvider
    {
        public IEnumerable<IFallbackStrategy> FallbackStrategiesForQuery(TorznabQuery query)
        {
            return (new NoFallbackStrategy()).ToEnumerable();
        }
    }

    public class ImdbFallbackStrategy : IFallbackStrategy
    {
        public ImdbFallbackStrategy(IImdbResolver resolver, TorznabQuery query)
        {
            this.resolver = resolver;
            this.titles = null;
            this.query = query;
        }

        public async Task<IEnumerable<TorznabQuery>> FallbackQueries()
        {
            if (titles == null)
                titles = (await resolver.MovieForId(query.ImdbID.ToNonNull())).Title.ToEnumerable();
            return titles.Select(t => query.CreateFallback(t));
        }

        private IImdbResolver resolver;
        private IEnumerable<string> titles;
        private TorznabQuery query;
    }

    public class ImdbFallbackStrategyProvider : IFallbackStrategyProvider
    {
        public ImdbFallbackStrategyProvider(IImdbResolver resolver)
        {
            this.resolver = resolver;
        }

        public IEnumerable<IFallbackStrategy> FallbackStrategiesForQuery(TorznabQuery query)
        {
            var result = new List<IFallbackStrategy>();
            if (!query.IsImdbQuery)
                result.Add(new NoFallbackStrategy());
            else
                result.Add(new ImdbFallbackStrategy(resolver, query));
            return result;
        }

        private IImdbResolver resolver;
    }
}
