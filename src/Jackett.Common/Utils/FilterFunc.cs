using System;

using Jackett.Common.Indexers;
using Jackett.Common.Utils.FilterFuncBuilders;

namespace Jackett.Common.Utils
{
    public static class FilterFunc
    {
        public static readonly IFilterFuncBuilder Default;

        static FilterFunc()
        {
            Default = new FilterFuncCompositeBuilder(
                FilterFuncBuilder.Group,
                FilterFuncBuilder.Language,
                FilterFuncBuilder.Type
            );
        }

        public static bool TryParse(string source, out Func<IIndexer, bool> filterFunc) => Default.TryParse(source, out filterFunc);
    }
}
