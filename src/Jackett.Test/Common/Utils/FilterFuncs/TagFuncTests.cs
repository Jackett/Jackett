using NUnit.Framework;
using static Jackett.Common.Utils.FilterFunc;

namespace Jackett.Test.Common.Utils.FilterFuncs
{
    [TestFixture]
    public class TagFuncTests
    {
        private class IndexerStub : IndexerBaseStub
        {
            public IndexerStub(params string[] tags)
            {
                Tags = tags;
            }

            public override bool IsConfigured => true;

            public override string[] Tags { get; }
        }

        [Test]
        public void CaseInsensitiveFilter()
        {
            var tagId = "g1";

            var tag = new IndexerStub(tagId);

            var upperTarget = Tag.ToFunc(tagId.ToUpper());
            Assert.IsTrue(upperTarget(tag));

            var lowerTarget = Tag.ToFunc(tagId.ToLower());
            Assert.IsTrue(lowerTarget(tag));
        }

        [Test]
        public void ContainsTagId()
        {
            var tagId = "g1";
            var target = Tag.ToFunc(tagId);

            Assert.IsTrue(target(new IndexerStub(tagId)));
            Assert.IsTrue(target(new IndexerStub(tagId, "g2")));
            Assert.IsTrue(target(new IndexerStub("g2", tagId)));
            Assert.IsFalse(target(new IndexerStub("g2")));
        }
    }
}
