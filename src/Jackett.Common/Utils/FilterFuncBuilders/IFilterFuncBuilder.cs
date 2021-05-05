using System;
using Jackett.Common.Indexers;

namespace Jackett.Common.Utils.FilterFuncBuilders
{
    public interface IFilterFuncBuilder
    {
        bool TryParse(string source, out Func<IIndexer, bool> filterFunc);
    }
}
