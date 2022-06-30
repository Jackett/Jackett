using System;

using Jackett.Common.Indexers;

namespace Jackett.Common.Utils.FilterFuncs
{
    public class TestFilterFunc : FilterFuncComponent
    {
        public static readonly TestFilterFunc Default = new TestFilterFunc();
        public const string Passed = "passed";
        public const string Failed = "failed";

        private TestFilterFunc() : base("test")
        {
        }

        public override Func<IIndexer, bool> ToFunc(string args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));
            if (string.Equals(Passed, args, StringComparison.InvariantCultureIgnoreCase))
                return i => IsValid(i) && i.LastError == null;
            if (string.Equals(Failed, args, StringComparison.InvariantCultureIgnoreCase))
                return i => IsValid(i) && i.LastError != null;
            throw new ArgumentException($"Invalid filter. Status should be '{Passed}' or '{Failed}'", nameof(args));
        }
    }
}
