using System;
using Jackett.Common.Indexers;

namespace Jackett.Common.Utils.FilterFuncBuilders
{
    public class LambdaFilterFuncBuilder : FilterFuncBuilderComponent
    {
        private readonly Func<string, Func<IIndexer, bool>> builderFunc;

        public LambdaFilterFuncBuilder(string id, Func<string, Func<IIndexer, bool>> builderFunc) : base(id)
        {
            this.builderFunc = builderFunc;
        }

        protected internal override Func<IIndexer, bool> BuildFilterFunc(string args)
        {
            return indexer => indexer != null
                ? indexer.IsConfigured && builderFunc(args)(indexer)
                : throw new ArgumentNullException(nameof(indexer));
        }
    }
}
