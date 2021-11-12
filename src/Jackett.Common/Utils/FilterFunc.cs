using System;
using System.Linq;

using Jackett.Common.Indexers;
using Jackett.Common.Utils.FilterFuncs;

namespace Jackett.Common.Utils
{
    public abstract class FilterFunc
    {
        public static readonly FilterFuncExpression Expression;
        public static readonly FilterFuncComponent Tag = Component("tag", args =>
        {
            var tag = args.ToLowerInvariant();
            return indexer => Array.IndexOf(indexer.Tags, tag) > -1;
        });
        public static readonly FilterFuncComponent Language = Component("lang", args => indexer => indexer.Language.StartsWith(args, StringComparison.InvariantCultureIgnoreCase));
        public static readonly FilterFuncComponent Type = Component("type", args => indexer => string.Equals(indexer.Type, args, StringComparison.InvariantCultureIgnoreCase));
        public static readonly FilterFuncComponent Test = TestFilterFunc.Default;
        public static readonly FilterFuncComponent Status = StatusFilterFunc.Default;

        static FilterFunc()
        {
            Expression = new FilterFuncExpression(Tag, Language, Type, Test, Status);
        }

        public static bool TryParse(string source, out Func<IIndexer, bool> func)
        {
            func = Expression.FromFilter(source);
            return func != null;
        }

        public abstract Func<IIndexer, bool> FromFilter(string source);

        public static FilterFuncComponent Component(string id, Func<string, Func<IIndexer, bool>> builder)
        {
            return new LambdaFilterFuncComponent(id, builder);
        }

        private class LambdaFilterFuncComponent : FilterFuncComponent
        {
            private readonly Func<string, Func<IIndexer, bool>> builder;

            internal LambdaFilterFuncComponent(string id, Func<string, Func<IIndexer, bool>> builder) : base(id)
            {
                if (builder == null)
                    throw new ArgumentNullException(nameof(builder));
                this.builder = builder;
            }

            public override Func<IIndexer, bool> ToFunc(string args)
            {
                var func = builder(args);
                return indexer => IsValid(indexer) && func(indexer);
            }
        }

        protected static bool IsValid(IIndexer indexer) => (indexer?.IsConfigured ?? throw new ArgumentNullException(nameof(indexer)));
    }
}
