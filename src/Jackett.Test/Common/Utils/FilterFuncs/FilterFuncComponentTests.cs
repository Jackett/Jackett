using System;
using Jackett.Common.Indexers;
using Jackett.Common.Utils.FilterFuncs;
using NUnit.Framework;

namespace Jackett.Test.Common.Utils.FilterFuncs
{
    [TestFixture]
    public class FilterFuncComponentTests
    {
        private readonly FilterFuncComponent target = new FilterFuncComponentStub("filter");
        private static readonly Func<IIndexer, bool> Func = _ => true;

        private class FilterFuncComponentStub : FilterFuncComponent
        {
            public FilterFuncComponentStub(string id) : base(id)
            {
            }

            public override Func<IIndexer, bool> ToFunc(string args) => Func;
        }

        [Test]
        public void Ctor_NullID_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() => new FilterFuncComponentStub(null));
        }

        [Test]
        public void Ctor_EmptyID_ThrowsException()
        {
            Assert.Throws<ArgumentException>(() => new FilterFuncComponentStub(string.Empty));
        }

        [Test]
        public void Ctor_WhitespaceID_ThrowsException()
        {
            Assert.Throws<ArgumentException>(() => new FilterFuncComponentStub(" "));
        }

        [Test]
        public void FromFilter_NullSource_Null()
        {
            var actual = target.FromFilter(null);
            Assert.IsNull(actual);
        }

        [Test]
        public void FromFilter_EmptySource_Null()
        {
            var actual = target.FromFilter(string.Empty);
            Assert.IsNull(actual);
        }

        [Test]
        public void FromFilter_WhitespaceSource_Null()
        {
            var actual = target.FromFilter(" ");
            Assert.IsNull(actual);
        }

        [Test]
        public void FromFilter_WrongSource_Null()
        {
            var actual = target.FromFilter("wrong:args");
            Assert.IsNull(actual);
        }

        [Test]
        public void FromFilter_NoArgsSource_Null()
        {
            var actual = target.FromFilter(target.ID);
            Assert.IsNull(actual);
        }

        [Test]
        public void FromFilter_EmptyArgsSource_Null()
        {
            var actual = target.FromFilter($"{target.ID}:");
            Assert.IsNull(actual);
        }

        [Test]
        public void FromFilter_SourceWithArgs()
        {
            var actual = target.FromFilter($"{target.ID.ToUpper()}:args");
            Assert.AreSame(Func, actual);
        }

        [Test]
        public void FromFilter_CaseInsensitivePrefixSource()
        {
            var actual = target.FromFilter($"{target.ID.ToUpper()}:args");
            Assert.AreSame(Func, actual);
        }

    }
}
