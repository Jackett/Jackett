using System.Collections;
using Jackett.Common.Indexers.Definitions;
using NUnit.Framework;

namespace Jackett.Test.Common.Indexers
{
    [TestFixture]
    public class EraiRawsTests
    {
        [TestCaseSource(typeof(TitleParserTestData), nameof(TitleParserTestData.TestCases))]
        public string TestTitleParsing(string title)
        {
            var titleParser = new EraiRaws.TitleParser();
            return titleParser.Parse(title);
        }
    }

    public class TitleParserTestData
    {
        public static IEnumerable TestCases
        {
            get
            {
                yield return new TestCaseData("Tokyo Revengers").Returns("Tokyo Revengers");
                yield return new TestCaseData("Tokyo Revengers - 02").Returns("Tokyo Revengers - E02");
                yield return new TestCaseData("Mairimashita! Iruma-kun 2nd Season - 01").Returns("Mairimashita! Iruma-kun - S2E01");
                yield return new TestCaseData("Seijo no Maryoku wa Bannou Desu - 02 v2 (Multi)").Returns("Seijo no Maryoku wa Bannou Desu - E02 v2 (Multi)");
                yield return new TestCaseData("Yuukoku no Moriarty Part 2 - 01 (Multi)").Returns("Yuukoku no Moriarty - S2E01 (Multi)");
            }
        }
    }
}
