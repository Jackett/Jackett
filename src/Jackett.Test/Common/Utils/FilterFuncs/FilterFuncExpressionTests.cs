using System;
using Jackett.Common.Indexers;
using Jackett.Common.Utils.FilterFuncs;
using Jackett.Test.TestHelpers;
using NUnit.Framework;

namespace Jackett.Test.Common.Utils.FilterFuncs
{
    [TestFixture]
    public class FilterFuncExpressionTests
    {
        private class FilterFuncComponentStub : FilterFuncComponent
        {
            private readonly Func<string, Func<IIndexer, bool>> builderFunc;

            public FilterFuncComponentStub(string id, Func<string, Func<IIndexer, bool>> builderFunc) : base(id)
            {
                this.builderFunc = builderFunc;
            }

            public override Func<IIndexer, bool> ToFunc(string args) => builderFunc(args);
        }

        private static readonly FilterFuncComponentStub _BoolFilterFunc =
                new FilterFuncComponentStub("bool",
                    args => bool.Parse(args) ? (Func<IIndexer, bool>)(_ => true) : _ => false
                );

        [Test]
        public void Ctor_NoFilters_ThrowsException()
        {
            Assert.Throws<ArgumentException>(() => new FilterFuncExpression());
        }

        [Test]
        public void Ctor_NullFilters_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() => new FilterFuncExpression(null));
        }

        [Test]
        public void Ctor_EmptyFilters_ThrowsException()
        {
            Assert.Throws<ArgumentException>(() =>
                new FilterFuncExpression(Array.Empty<FilterFuncComponent>())
            );
        }

        [Test]
        public void Ctor_WithNullFilter_ThrowsException()
        {
            Assert.Throws<ArgumentException>(() =>
                new FilterFuncExpression(default(FilterFuncComponent))
            );
        }

        [Test]
        public void Ctor_WithDuplicatedPrefixFilter_ThrowsException()
        {
            const string id = "f1";
            Func<string, Func<IIndexer, bool>> func = _ => throw TestExceptions.UnexpectedInvocation;

            Assert.Throws<ArgumentException>(() =>
            {
                new FilterFuncExpression(
                    new FilterFuncComponentStub(id, func),
                    new FilterFuncComponentStub(id, func));
            });
        }

        [Test]
        public void SingleSource()
        {
            Func<IIndexer, bool> expectedFunc1 = _ => throw TestExceptions.UnexpectedInvocation;
            Func<IIndexer, bool> expectedFunc2 = _ => throw TestExceptions.UnexpectedInvocation;

            var target = new FilterFuncExpression(
                new FilterFuncComponentStub("f1", _ => expectedFunc1),
                new FilterFuncComponentStub("f2", _ => expectedFunc2)
                );

            var actualFunc1 = target.FromFilter("f1:args");
            Assert.AreSame(expectedFunc1, actualFunc1);
            var actualFunc2 = target.FromFilter("f2:args");
            Assert.AreSame(expectedFunc2, actualFunc2);
            var actualFunc3 = target.FromFilter("f3:args");
            Assert.IsNull(actualFunc3);
        }

        [Test]
        public void SingleSource_NotOperator()
        {
            var target = new FilterFuncExpression(_BoolFilterFunc);

            var filterFunc = target.FromFilter("!bool:true");
            Assert.IsFalse(filterFunc(null));
        }

        [Test]
        public void SingleSource_AndOperator()
        {
            var target = new FilterFuncExpression(_BoolFilterFunc);

            var filterFunc = target.FromFilter("bool:true+bool:false");
            Assert.IsFalse(filterFunc(null));
        }

        [Test]
        public void SingleSource_OrOperator()
        {
            var target = new FilterFuncExpression(_BoolFilterFunc);

            var filterFunc = target.FromFilter("bool:false,bool:true");
            Assert.IsTrue(filterFunc(null));
        }

        [Test]
        public void SingleSource_OperatorPrecedence()
        {
            var target = new FilterFuncExpression(_BoolFilterFunc);

            var filterFunc1 = target.FromFilter("bool:false+bool:true,bool:true");
            Assert.IsTrue(filterFunc1(null));
            var filterFunc2 = target.FromFilter("bool:true,bool:true+bool:false");
            Assert.IsTrue(filterFunc2(null));
        }
    }
}
