using NUnit.Framework;
using static Jackett.Common.Utils.FilterFunc;

namespace Jackett.Test.Common.Utils.FilterFuncs
{
    [TestFixture]
    public class TagFuncTests
    {
        private class TagsIndexerStub : IndexerStub
        {
            public TagsIndexerStub(params string[] tags)
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

            var tag = new TagsIndexerStub(tagId);

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

            Assert.IsTrue(target(new TagsIndexerStub(tagId)));
            Assert.IsTrue(target(new TagsIndexerStub(tagId, "g2")));
            Assert.IsTrue(target(new TagsIndexerStub("g2", tagId)));
            Assert.IsFalse(target(new TagsIndexerStub("g2")));
        }
    }
}
