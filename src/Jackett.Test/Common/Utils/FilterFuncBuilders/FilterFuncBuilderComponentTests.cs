using System;
using System.Collections;
using Jackett.Common.Indexers;
using Jackett.Common.Utils.FilterFuncBuilders;
using Jackett.Test.TestHelpers;
using NUnit.Framework;

namespace Jackett.Test.Common.Utils.FilterFuncBuilders
{
    [TestFixture]
    public class FilterFuncBuilderComponentTests
    {
        private readonly FilterFuncBuilderComponent target = new FilterFuncBuilderComponentStub("filter");
        private static readonly Func<IIndexer, bool> FilterFunc = _ => true;

        private class FilterFuncBuilderComponentStub : FilterFuncBuilderComponent
        {
            public FilterFuncBuilderComponentStub(string id) : base(id)
            {
            }

            protected override Func<IIndexer, bool> BuildFilterFunc(string args) => FilterFunc;
        }

        [Test]
        public void Ctor_NullID_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() => new FilterFuncBuilderComponentStub(null));
        }

        [Test]
        public void Ctor_EmptyID_ThrowsException()
        {
            Assert.Throws<ArgumentException>(() => new FilterFuncBuilderComponentStub(string.Empty));
        }

        [Test]
        public void Ctor_WhitespaceID_ThrowsException()
        {
            Assert.Throws<ArgumentException>(() => new FilterFuncBuilderComponentStub(" "));
        }

        [Test]
        public void TryParse_WrongPrefixSource_Fail()
        {
            Assert.IsFalse(target.TryParse("wrong:args", out var filterFunc));
            Assert.IsNull(filterFunc);
        }

        [Test]
        public void TryParse_NoSeparatorSource_Fail()
        {
            Assert.IsFalse(target.TryParse(target.ID, out var filterFunc));
            Assert.IsNull(filterFunc);
        }

        [Test]
        public void TryParse_NoArgsSource_Fail()
        {
            Assert.IsFalse(target.TryParse($"{target.ID}:", out var filterFunc));
            Assert.IsNull(filterFunc);
        }

        [Test]
        public void TryParse_CaseInsensitivePrefixSource()
        {
            Assert.IsTrue(target.TryParse($"{target.ID.ToUpper()}:args", out var filterFunc));
            Assert.AreSame(FilterFunc, filterFunc);
        }

    }
}
