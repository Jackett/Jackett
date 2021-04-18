using System;
using System.Collections;
using Jackett.Common.Indexers;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

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
                yield return new TestCaseData("[1080p] Tokyo Revengers").Returns("[1080p] Tokyo Revengers");
                yield return new TestCaseData("[1080p] Tokyo Revengers – 02").Returns("[1080p] Tokyo Revengers – E02");
                yield return new TestCaseData("[1080p] Mairimashita! Iruma-kun 2nd Season – 01").Returns("[1080p] Mairimashita! Iruma-kun – S2E01");
                yield return new TestCaseData("[540p] Seijo no Maryoku wa Bannou Desu – 02 v2 (Multi)").Returns("[540p] Seijo no Maryoku wa Bannou Desu – E02 v2 (Multi)");
                yield return new TestCaseData("[1080p] Yuukoku no Moriarty Part 2 – 01 (Multi)").Returns("[1080p] Yuukoku no Moriarty – S2E01 (Multi)");                
            }
        }
    }
}
