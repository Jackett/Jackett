using System;
using Jackett.Common.Utils;
using Jackett.Common.Utils.FilterFuncs;
using NUnit.Framework;

namespace Jackett.Test.Common.Utils.FilterFuncs
{
    [TestFixture]
    public class StatusFuncTests
    {
        private static readonly IndexerStub HealthyIndexer = new IndexerStub(isHealthy: true, isFailing: false);
        private static readonly IndexerStub FailingIndexer = new IndexerStub(isHealthy: false, isFailing: true);
        private static readonly IndexerStub UnknownIndexer = new IndexerStub(isHealthy: false, isFailing: false);
        private static readonly IndexerStub InvalidIndexer = new IndexerStub(isHealthy: true, isFailing: true);

        private class IndexerStub : IndexerBaseStub
        {
            public IndexerStub(bool isHealthy, bool isFailing)
            {
                IsHealthy = isHealthy;
                IsFailing = isFailing;
            }

            public override bool IsConfigured => true;

            public override bool IsHealthy { get; }
            public override bool IsFailing { get; }
        }

        [Test]
        public void NullStatus_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() => FilterFunc.Status.ToFunc(null));
        }

        [Test]
        public void EmptyStatus_ThrowsException()
        {
            Assert.Throws<ArgumentException>(() => FilterFunc.Status.ToFunc(string.Empty));
        }

        [Test]
        public void InvalidStatus_ThrowsException()
        {
            Assert.Throws<ArgumentException>(() => FilterFunc.Status.ToFunc(StatusFilterFunc.Healthy + StatusFilterFunc.Failing));
        }

        [Test]
        public void HealthyFilter()
        {
            var passedFilterFunc = FilterFunc.Status.ToFunc(StatusFilterFunc.Healthy);
            Assert.IsTrue(passedFilterFunc(HealthyIndexer));
            Assert.IsFalse(passedFilterFunc(FailingIndexer));
            Assert.IsFalse(passedFilterFunc(UnknownIndexer));
            Assert.IsFalse(passedFilterFunc(InvalidIndexer));
        }

        [Test]
        public void FailingFilter()
        {
            var failingFilterFunc = FilterFunc.Status.ToFunc(StatusFilterFunc.Failing);
            Assert.IsFalse(failingFilterFunc(HealthyIndexer));
            Assert.IsTrue(failingFilterFunc(FailingIndexer));
            Assert.IsFalse(failingFilterFunc(UnknownIndexer));
            Assert.IsFalse(failingFilterFunc(InvalidIndexer));
        }

        [Test]
        public void UnknownFilter()
        {
            var unknownFilterFunc = FilterFunc.Status.ToFunc(StatusFilterFunc.Unknown);
            Assert.IsFalse(unknownFilterFunc(HealthyIndexer));
            Assert.IsFalse(unknownFilterFunc(FailingIndexer));
            Assert.IsTrue(unknownFilterFunc(UnknownIndexer));
            Assert.IsTrue(unknownFilterFunc(InvalidIndexer));
        }

        [Test]
        public void PassedFilter_CaseInsensitiveSource()
        {
            var upperFilterFunc = FilterFunc.Status.ToFunc(StatusFilterFunc.Healthy.ToUpper());
            Assert.IsTrue(upperFilterFunc(HealthyIndexer));

            var lowerFilterFunc = FilterFunc.Status.ToFunc(StatusFilterFunc.Healthy.ToLower());
            Assert.IsTrue(lowerFilterFunc(HealthyIndexer));
        }

        [Test]
        public void FailedFilter_CaseInsensitiveSource()
        {
            var upperFilterFunc = FilterFunc.Status.ToFunc(StatusFilterFunc.Failing.ToUpper());
            Assert.IsTrue(upperFilterFunc(FailingIndexer));

            var lowerFilterFunc = FilterFunc.Status.ToFunc(StatusFilterFunc.Failing.ToLower());
            Assert.IsTrue(lowerFilterFunc(FailingIndexer));
        }

        [Test]
        public void UnknownFilter_CaseInsensitiveSource()
        {
            var upperFilterFunc = FilterFunc.Status.ToFunc(StatusFilterFunc.Unknown.ToUpper());
            Assert.IsTrue(upperFilterFunc(UnknownIndexer));

            var lowerFilterFunc = FilterFunc.Status.ToFunc(StatusFilterFunc.Unknown.ToLower());
            Assert.IsTrue(lowerFilterFunc(UnknownIndexer));
        }
    }
}
