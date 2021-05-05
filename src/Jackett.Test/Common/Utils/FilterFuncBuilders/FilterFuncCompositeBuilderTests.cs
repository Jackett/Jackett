using System;

using Jackett.Common.Indexers;
using Jackett.Common.Utils.FilterFuncBuilders;
using Jackett.Test.TestHelpers;

using NUnit.Framework;

namespace Jackett.Test.Common.Utils.FilterFuncBuilders
{
    [TestFixture]
    public class FilterFuncCompositeBuilderTests
    {
        private class FilterFuncBuilderComponentStub : FilterFuncBuilderComponent
        {
            private readonly Func<string, Func<IIndexer, bool>> builderFunc;

            public FilterFuncBuilderComponentStub(string id, Func<string, Func<IIndexer, bool>> builderFunc) : base(id)
            {
                this.builderFunc = builderFunc;
            }

            protected override Func<IIndexer, bool> BuildFilterFunc(string args) => builderFunc(args);
        }

        [Test]
        public void Ctor_NoFilters_ThrowsException()
        {
            Assert.Throws<ArgumentException>(() => new FilterFuncCompositeBuilder());
        }

        [Test]
        public void Ctor_NullFilters_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() => new FilterFuncCompositeBuilder(null));
        }

        [Test]
        public void Ctor_EmptyFilters_ThrowsException()
        {
            Assert.Throws<ArgumentException>(() =>
                new FilterFuncCompositeBuilder(Array.Empty<FilterFuncBuilderComponent>())
            );
        }

        [Test]
        public void Ctor_WithNullFilter_ThrowsException()
        {
            Assert.Throws<ArgumentException>(() =>
                new FilterFuncCompositeBuilder(default(FilterFuncBuilderComponent))
            );
        }

        [Test]
        public void Ctor_WithDuplicatedPrefixFilter_ThrowsException()
        {
            const string id = "f1";
            Func<string, Func<IIndexer, bool>> func = _ => throw TestExceptions.UnexpectedInvocation;

            Assert.Throws<ArgumentException>(() =>
            {
                new FilterFuncCompositeBuilder(
                    new FilterFuncBuilderComponentStub(id, func),
                    new FilterFuncBuilderComponentStub(id, func));
            });
        }

        [Test]
        public void SingleSource()
        {
            Func<IIndexer, bool> expectedFunc1 = _ => throw TestExceptions.UnexpectedInvocation;
            Func<IIndexer, bool> expectedFunc2 = _ => throw TestExceptions.UnexpectedInvocation;

            var target = new FilterFuncCompositeBuilder(
                new FilterFuncBuilderComponentStub("f1", _ => expectedFunc1),
                new FilterFuncBuilderComponentStub("f2", _ => expectedFunc2)
                );

            Assert.IsTrue(target.TryParse("f1:args", out var actualFunc1));
            Assert.AreSame(expectedFunc1, actualFunc1);
            Assert.IsTrue(target.TryParse("f2:args", out var actualFunc2));
            Assert.AreSame(expectedFunc2, actualFunc2);
            Assert.IsFalse(target.TryParse("f3:args", out var actualFunc3));
            Assert.IsNull(actualFunc3);
        }

        [Test]
        public void SingleSource_NotOperator()
        {
            var target = new FilterFuncCompositeBuilder(
                new FilterFuncBuilderComponentStub("bool", args => bool.Parse(args) ? _ => true : _ => false)
                );

            Assert.IsTrue(target.TryParse("!bool:true", out var filterFunc));
            Assert.IsFalse(filterFunc(null));
        }

        [Test]
        public void SingleSource_AndOperator()
        {
            var target = new FilterFuncCompositeBuilder(
                new FilterFuncBuilderComponentStub("bool", args => bool.Parse(args) ? _ => true : _ => false)
                );

            Assert.IsTrue(target.TryParse("bool:true+bool:false", out var filterFunc));
            Assert.IsFalse(filterFunc(null));
        }

        [Test]
        public void SingleSource_OrOperator()
        {
            var target = new FilterFuncCompositeBuilder(
                new FilterFuncBuilderComponentStub("bool", args => bool.Parse(args) ? _ => true : _ => false)
                );

            Assert.IsTrue(target.TryParse("bool:false,bool:true", out var filterFunc));
            Assert.IsTrue(filterFunc(null));
        }

        [Test]
        public void SingleSource_OperatorPrecedence()
        {
            var target = new FilterFuncCompositeBuilder(
                new FilterFuncBuilderComponentStub("bool", args => bool.Parse(args) ? _ => true : _ => false)
                );

            Assert.IsTrue(target.TryParse("bool:false+bool:true,bool:true", out var filterFunc1));
            Assert.IsTrue(filterFunc1(null));
            Assert.IsTrue(target.TryParse("bool:true,bool:true+bool:false", out var filterFunc2));
            Assert.IsTrue(filterFunc2(null));
        }
    }
}
