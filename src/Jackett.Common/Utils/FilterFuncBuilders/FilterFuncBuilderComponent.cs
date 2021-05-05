using System;

using Jackett.Common.Indexers;

namespace Jackett.Common.Utils.FilterFuncBuilders
{
    public abstract class FilterFuncBuilderComponent : FilterFuncBuilder
    {
        private static readonly char[] Separator = { ':' };

        protected FilterFuncBuilderComponent()
        {
            ID = null;
        }

        protected FilterFuncBuilderComponent(string id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID cannot be an empty string or whitespaces", nameof(id));
            ID = id;
        }

        public string ID { get; }

        protected override Func<IIndexer, bool> Build(string source)
        {
            var parts = source.Split(Separator, 2);
            if (parts.Length != 2)
                return null;
            if (!string.Equals(parts[0], ID, StringComparison.InvariantCultureIgnoreCase))
                return null;
            var args = parts[1];
            if (string.IsNullOrWhiteSpace(args))
                return null;
            return BuildFilterFunc(args);
        }

        protected internal abstract Func<IIndexer, bool> BuildFilterFunc(string args);
    }
}
