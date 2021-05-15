using System;
using Jackett.Common.Utils;
using Jackett.Common.Utils.FilterFuncs;
using Jackett.Test.TestHelpers;
using NUnit.Framework;

namespace Jackett.Test.Common.Utils.FilterFuncs
{
    [TestFixture]
    public class TestFuncTests
    {
        private static readonly IndexerStub HealthyIndexer = new IndexerStub(lastError: null);
        private static readonly IndexerStub ErrorIndexer = new IndexerStub(lastError: "error");

        private class IndexerStub : IndexerBaseStub
        {
            private readonly string lastError;

            public IndexerStub(string lastError)
            {
                this.lastError = lastError;
            }

            public override bool IsConfigured => true;

            public override string LastError
            {
                get => lastError;
                set => throw TestExceptions.UnexpectedInvocation;
            }
        }

        [Test]
        public void NullStatus_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() => FilterFunc.Test.ToFunc(null));
        }

        [Test]
        public void EmptyStatus_ThrowsException()
        {
            Assert.Throws<ArgumentException>(() => FilterFunc.Test.ToFunc(string.Empty));
        }

        [Test]
        public void InvalidStatus_ThrowsException()
        {
            Assert.Throws<ArgumentException>(() => FilterFunc.Test.ToFunc(TestFilterFunc.Passed + TestFilterFunc.Failed));
        }

        [Test]
        public void PassedFilter()
        {
            var passedFilterFunc = FilterFunc.Test.ToFunc(TestFilterFunc.Passed);
            Assert.IsTrue(passedFilterFunc(HealthyIndexer));
            Assert.IsFalse(passedFilterFunc(ErrorIndexer));
        }

        [Test]
        public void FailedFilter()
        {
            var failedFilterFunc = FilterFunc.Test.ToFunc(TestFilterFunc.Failed);
            Assert.IsFalse(failedFilterFunc(HealthyIndexer));
            Assert.IsTrue(failedFilterFunc(ErrorIndexer));
        }

        [Test]
        public void PassedFilter_CaseInsensitiveSource()
        {
            var upperFilterFunc = FilterFunc.Test.ToFunc(TestFilterFunc.Passed.ToUpper());
            Assert.IsTrue(upperFilterFunc(HealthyIndexer));
            Assert.IsFalse(upperFilterFunc(ErrorIndexer));

            var lowerFilterFunc = FilterFunc.Test.ToFunc(TestFilterFunc.Passed.ToLower());
            Assert.IsTrue(lowerFilterFunc(HealthyIndexer));
            Assert.IsFalse(lowerFilterFunc(ErrorIndexer));
        }

        [Test]
        public void FailedFilter_CaseInsensitiveSource()
        {
            var upperFilterFunc = FilterFunc.Test.ToFunc(TestFilterFunc.Failed.ToUpper());
            Assert.IsFalse(upperFilterFunc(HealthyIndexer));
            Assert.IsTrue(upperFilterFunc(ErrorIndexer));

            var lowerFilterFunc = FilterFunc.Test.ToFunc(TestFilterFunc.Failed.ToLower());
            Assert.IsFalse(lowerFilterFunc(HealthyIndexer));
            Assert.IsTrue(lowerFilterFunc(ErrorIndexer));
        }
    }
}
