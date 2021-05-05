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
    public class LanguageFilterFuncBuilderTests
    {
        private class LanguageIndexerStub : IndexerStub
        {
            public LanguageIndexerStub(string language)
            {
                Language = language;
            }

            public override bool IsConfigured => true;

            public override string Language { get; }
        }

        private readonly FilterFuncBuilderComponent target = FilterFuncBuilder.Language;

        [Test]
        public void TryParse_CaseInsensitiveSource_CaseInsensitiveFilter()
        {
            var language = "en";
            var region = "US";

            var lrLanguage = new LanguageIndexerStub($"{language.ToLower()}-{region.ToLower()}");
            Assert.IsTrue(target.TryParse($"{target.ID}:{language.ToUpper()}-{region.ToUpper()}", out var LRFilterFunc));
            Assert.IsTrue(LRFilterFunc(lrLanguage));

            var lRLanguage = new LanguageIndexerStub($"{language.ToLower()}-{region.ToUpper()}");
            Assert.IsTrue(target.TryParse($"{target.ID}:{language.ToUpper()}-{region.ToLower()}", out var LrFilterFunc));
            Assert.IsTrue(LrFilterFunc(lRLanguage));

            var LrLanguage = new LanguageIndexerStub($"{language.ToUpper()}-{region.ToLower()}");
            Assert.IsTrue(target.TryParse($"{target.ID}:{language.ToLower()}-{region.ToUpper()}", out var lRFilterFunc));
            Assert.IsTrue(lRFilterFunc(LrLanguage));

            var LRLanguage = new LanguageIndexerStub($"{language.ToUpper()}-{region.ToUpper()}");
            Assert.IsTrue(target.TryParse($"{target.ID}:{language.ToLower()}-{region.ToLower()}", out var lrFilterFunc));
            Assert.IsTrue(lrFilterFunc(LRLanguage));
        }

        [Test]
        public void TryParse_LanguageWithoutRegion()
        {
            var language = "en";
            Assert.IsTrue(target.TryParse($"{target.ID}:{language}", out var funcFilter));

            Assert.IsTrue(funcFilter(new LanguageIndexerStub(language)));
            Assert.IsTrue(funcFilter(new LanguageIndexerStub($"{language}-region1")));
            Assert.IsFalse(funcFilter(new LanguageIndexerStub($"language2-{language}")));
        }
    }
}
