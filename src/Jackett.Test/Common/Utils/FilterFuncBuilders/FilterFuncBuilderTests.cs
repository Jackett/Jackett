using System;
using Jackett.Common.Indexers;
using Jackett.Common.Utils.FilterFuncBuilders;
using Jackett.Test.TestHelpers;
using NUnit.Framework;

namespace Jackett.Test.Common.Utils.FilterFuncBuilders
{
    [TestFixture]
    public class FilterFuncBuilderTests
    {
        private readonly FilterFuncBuilder target = new FilterFuncBuilderBaseStub();
        private static readonly Func<IIndexer, bool> FilterFunc = _ => true;

        private class FilterFuncBuilderBaseStub : FilterFuncBuilder
        {
            protected override Func<IIndexer, bool> Build(string source) => FilterFunc;
        }

        [Test]
        public void TryParse_NullSource_Fail()
        {
            Assert.IsFalse(target.TryParse(null, out var filterFunc));
            Assert.IsNull(filterFunc);
        }

        [Test]
        public void TryParse_EmptySource_Fail()
        {
            Assert.IsFalse(target.TryParse(string.Empty, out var filterFunc));
            Assert.IsNull(filterFunc);
        }

        [Test]
        public void TryParse_WhitespaceSource_Fail()
        {
            Assert.IsFalse(target.TryParse(" ", out var filterFunc));
            Assert.IsNull(filterFunc);
        }

        [Test]
        public void TryParse_NoWhitespaceSource()
        {
            Assert.IsTrue(target.TryParse("string", out var filterFunc));
            Assert.AreSame(FilterFunc, filterFunc);
        }
    }
}
