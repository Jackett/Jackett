using System;
using System.Collections.Generic;
using System.Linq;
using Jackett.Common.Indexers;

namespace Jackett.Common.Utils.FilterFuncs
{
    public class FilterFuncExpression : FilterFunc
    {
        private static readonly char Separator = ':';
        private static readonly char NotOperator = '!';
        private static readonly char OrOperator = ',';
        private static readonly char AndOperator = '+';

        private readonly IReadOnlyDictionary<string, Func<string, Func<IIndexer, bool>>> components;

        public FilterFuncExpression(params FilterFuncComponent[] components)
        {
            if (components == null)
                throw new ArgumentNullException(nameof(components));
            if (components.Length == 0)
                throw new ArgumentException("Filters cannot be an empty collection.", nameof(components));
            if (components.Any(x => x == null))
                throw new ArgumentException("Filters cannot contains null values.", nameof(components));
            this.components = components.ToDictionary<FilterFuncComponent, string, Func<string, Func<IIndexer, bool>>>(x => x.ID, x => x.ToFunc, StringComparer.InvariantCultureIgnoreCase);
        }

        public override Func<IIndexer, bool> FromFilter(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return null;
            if (source.Contains(OrOperator))
                return source.Split(OrOperator).Select(FromFilter).Aggregate(Or);
            if (source.Contains(AndOperator))
                return source.Split(AndOperator).Select(FromFilter).Aggregate(And);
            if (source[0] == NotOperator)
                return Not(FromFilter(source.Substring(1)));
            if (source.Contains(Separator))
            {
                var parts = source.Split(new[] { Separator }, 2);
                if (parts.Length == 2 && components.TryGetValue(parts[0], out var toFunc))
                    return toFunc(parts[1]);
            }
            return null;
        }

        private static Func<IIndexer, bool> Not(Func<IIndexer, bool> u) => i => !u(i);
        private static Func<IIndexer, bool> And(Func<IIndexer, bool> l, Func<IIndexer, bool> r) => i => l(i) && r(i);
        private static Func<IIndexer, bool> Or(Func<IIndexer, bool> l, Func<IIndexer, bool> r) => i => l(i) || r(i);
    }
}
