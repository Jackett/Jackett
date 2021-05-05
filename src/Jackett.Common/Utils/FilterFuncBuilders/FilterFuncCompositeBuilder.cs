using System;
using System.Collections.Generic;
using System.Linq;

using Jackett.Common.Indexers;

namespace Jackett.Common.Utils.FilterFuncBuilders
{
    public class FilterFuncCompositeBuilder : FilterFuncBuilder
    {
        private static readonly char Separator = ':';
        private static readonly char NotOperator = '!';
        private static readonly char OrOperator = ',';
        private static readonly char AndOperator = '+';

        private readonly IReadOnlyDictionary<string, Func<string, Func<IIndexer, bool>>> filters;

        public FilterFuncCompositeBuilder(params FilterFuncBuilderComponent[] filters)
        {
            if (filters == null)
                throw new ArgumentNullException(nameof(filters));
            if (filters.Length == 0)
                throw new ArgumentException("Filters cannot be an empty collection.", nameof(filters));
            if (filters.Any(x => x == null))
                throw new ArgumentException("Filters cannot contains null values.", nameof(filters));
            this.filters = filters.ToDictionary<FilterFuncBuilderComponent, string, Func<string, Func<IIndexer, bool>>>(x => x.ID, x => x.BuildFilterFunc, StringComparer.InvariantCultureIgnoreCase);
        }

        protected override Func<IIndexer, bool> Build(string source)
        {
            if (source.Contains(OrOperator))
                return source.Split(OrOperator).Select(Build).Aggregate(Or);
            if (source.Contains(AndOperator))
                return source.Split(AndOperator).Select(Build).Aggregate(And);
            if (source[0] == NotOperator)
                return Not(Build(source.Substring(1)));
            if (source.Contains(Separator))
            {
                var parts = source.Split(new[] {Separator}, 2);
                if (parts.Length == 2 && filters.TryGetValue(parts[0], out var builder))
                    return builder(parts[1]);
            }
            return null;
        }

        private static Func<IIndexer, bool> Not(Func<IIndexer, bool> u) => i => !u(i);
        private static Func<IIndexer, bool> And(Func<IIndexer, bool> l, Func<IIndexer, bool> r) => i => l(i) && r(i);
        private static Func<IIndexer, bool> Or(Func<IIndexer, bool> l, Func<IIndexer, bool> r) => i => l(i) || r(i);
    }
}
