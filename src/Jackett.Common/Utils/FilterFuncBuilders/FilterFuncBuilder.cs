using System;
using System.Linq;
using System.Text.RegularExpressions;
using Jackett.Common.Indexers;

namespace Jackett.Common.Utils.FilterFuncBuilders
{
    public abstract class FilterFuncBuilder : IFilterFuncBuilder
    {

        public static readonly FilterFuncBuilderComponent Group = new LambdaFilterFuncBuilder("group", args => x => x.Groups.Contains(args, StringComparer.InvariantCultureIgnoreCase));
        public static readonly FilterFuncBuilderComponent Language = new LambdaFilterFuncBuilder("lang", args => x => x.Language.StartsWith(args, StringComparison.InvariantCultureIgnoreCase));
        public static readonly FilterFuncBuilderComponent Type = new LambdaFilterFuncBuilder("type", args => x => string.Equals(x.Type, args, StringComparison.InvariantCultureIgnoreCase));

        public bool TryParse(string source, out Func<IIndexer, bool> filterFunc)
        {
            filterFunc = null;
            if (string.IsNullOrWhiteSpace(source))
                return false;
            filterFunc = Build(source);
            return filterFunc != null;
        }

        protected abstract Func<IIndexer, bool> Build(string source);
    }
}
