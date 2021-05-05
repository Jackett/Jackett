using Jackett.Common.Utils.FilterFuncBuilders;
using NUnit.Framework;

namespace Jackett.Test.Common.Utils.FilterFuncBuilders
{
    [TestFixture]
    public class TypeFilterFuncBuilderTests
    {
        private class TypeIndexerStub : IndexerStub
        {
            public TypeIndexerStub(string type)
            {
                Type = type;
            }

            public override bool IsConfigured => true;

            public override string Type { get; }
        }

        private readonly FilterFuncBuilderComponent target = FilterFuncBuilder.Type;

        [Test]
        public void TryParse_CaseInsensitiveSource_CaseInsensitiveFilter()
        {
            var typeId = "type-id";

            var lowerType = new TypeIndexerStub(typeId.ToLower());
            Assert.IsTrue(target.TryParse($"{target.ID}:{typeId.ToUpper()}", out var upperFilterFunc));
            Assert.IsTrue(upperFilterFunc(lowerType));

            var upperType = new TypeIndexerStub(typeId.ToUpper());
            Assert.IsTrue(target.TryParse($"{target.ID}:{typeId.ToLower()}", out var lowerFilterFunc));
            Assert.IsTrue(lowerFilterFunc(upperType));
        }

        [Test]
        public void TryParse_PartialType()
        {
            var typeId = "type-id";
            Assert.IsTrue(target.TryParse($"{target.ID}:{typeId}", out var funcFilter));

            Assert.IsFalse(funcFilter(new TypeIndexerStub($"{typeId}suffix")));
            Assert.IsFalse(funcFilter(new TypeIndexerStub($"prefix{typeId}")));
            Assert.IsFalse(funcFilter(new TypeIndexerStub($"prefix{typeId}suffix")));
        }
    }


}
