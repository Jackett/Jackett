using System.Text;
using System.Threading.Tasks;

using Jackett.Common.Indexers;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Utils.FilterFuncBuilders;
using Jackett.Test.TestHelpers;

using Newtonsoft.Json.Linq;

using NUnit.Framework;

namespace Jackett.Test.Common.Utils.FilterFuncBuilders
{
    [TestFixture]
    public class GroupFilterFuncBuilderTests
    {
        private class GroupsIndexerStub : IndexerStub
        {
            public GroupsIndexerStub(params string[] groups)
            {
                Groups = groups;
            }

            public override bool IsConfigured => true;

            public override string[] Groups { get; }
        }

        private readonly FilterFuncBuilderComponent target = FilterFuncBuilder.Group;

        [Test]
        public void TryParse_CaseInsensitiveSource_CaseInsensitiveFilter()
        {
            var groupId = "g1";

            var lowerGroup = new GroupsIndexerStub(groupId.ToLower());
            Assert.IsTrue(target.TryParse($"{target.ID}:{groupId.ToUpper()}", out var upperFilterFunc));
            Assert.IsTrue(upperFilterFunc(lowerGroup));

            var upperGroup = new GroupsIndexerStub(groupId.ToUpper());
            Assert.IsTrue(target.TryParse($"{target.ID}:{groupId.ToLower()}", out var lowerFilterFunc));
            Assert.IsTrue(lowerFilterFunc(upperGroup));
        }

        [Test]
        public void TryParse_ContainsGroupId()
        {
            var groupId = "g1";
            Assert.IsTrue(target.TryParse($"{target.ID}:{groupId}", out var funcFilter));

            Assert.IsTrue(funcFilter(new GroupsIndexerStub(groupId)));
            Assert.IsTrue(funcFilter(new GroupsIndexerStub(groupId, "g2")));
            Assert.IsTrue(funcFilter(new GroupsIndexerStub("g2", groupId)));
            Assert.IsFalse(funcFilter(new GroupsIndexerStub("g2")));
        }
    }
}
