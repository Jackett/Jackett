using System;
using Jackett.Common.Indexers;

namespace Jackett.Common.Utils.FilterFuncs
{
    public class StatusFilterFunc : FilterFuncComponent
    {
        public static readonly StatusFilterFunc Default = new StatusFilterFunc();
        public const string Healthy = "healthy";
        public const string Failing = "failing";
        public const string Unknown = "unknown";

        private StatusFilterFunc() : base("status")
        {
        }

        public override Func<IIndexer, bool> ToFunc(string args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));
            if (string.Equals(Healthy, args, StringComparison.InvariantCultureIgnoreCase))
                return i => IsValid(i) && i.IsHealthy && !i.IsFailing;
            if (string.Equals(Failing, args, StringComparison.InvariantCultureIgnoreCase))
                return i => IsValid(i) && !i.IsHealthy && i.IsFailing;
            if (string.Equals(Unknown, args, StringComparison.InvariantCultureIgnoreCase))
                return i => IsValid(i) && ((!i.IsHealthy && !i.IsFailing) || (i.IsHealthy && i.IsFailing));
            throw new ArgumentException($"Invalid filter. Status should be '{Healthy}', {Failing} or '{Unknown}'", nameof(args));
        }
    }
}
