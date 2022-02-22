using System;
using System.Linq;
using Jackett.Common.Indexers;

namespace Jackett.Common.Utils.FilterFuncs
{
    public abstract class FilterFuncComponent : FilterFunc
    {
        private static readonly char Separator = ':';

        protected FilterFuncComponent(string id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID cannot be an empty string or whitespaces", nameof(id));
            ID = id;
        }

        public string ID { get; }

        public override Func<IIndexer, bool> FromFilter(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return null;

            var parts = source.Split(new[] { Separator }, 2);
            if (parts.Length != 2)
                return null;
            if (!string.Equals(parts[0], ID, StringComparison.InvariantCultureIgnoreCase))
                return null;
            var args = parts[1];
            if (string.IsNullOrWhiteSpace(args))
                return null;

            return ToFunc(args);
        }

        public abstract Func<IIndexer, bool> ToFunc(string args);

        public string ToFilter(string args)
        {
            return $"{ID}{Separator}{args}";
        }
    }
}
