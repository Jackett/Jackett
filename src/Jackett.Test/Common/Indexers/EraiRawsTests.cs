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

        [TestCaseSource(typeof(UrlSlugTestData), nameof(UrlSlugTestData.TestCases))]
        public string TestTitleParsing_GetUrlSlug(string title)
        {
            var titleParser = new EraiRaws.TitleParser();
            return titleParser.GetUrlSlug(title);
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

    public class UrlSlugTestData
    {
        public static IEnumerable TestCases
        {
            get
            {
                yield return new TestCaseData("Tokyo Revengers – 02").Returns("tokyo-revengers");
                yield return new TestCaseData("Mairimashita! Iruma-kun 2nd Season – 01").Returns("mairimashita-iruma-kun-2nd-season");
                yield return new TestCaseData("Seijo no Maryoku wa Bannou Desu – 02 v2 (Multi)").Returns("seijo-no-maryoku-wa-bannou-desu");
                yield return new TestCaseData("Yuukoku no Moriarty Part 2 – 01 (Multi)").Returns("yuukoku-no-moriarty-part-2");
                yield return new TestCaseData("Maou-sama Retry! – 12 END ").Returns("maou-sama-retry");
                yield return new TestCaseData("Baki (2018) – 01 ~ 26 ").Returns("baki-2018");
                yield return new TestCaseData("Free!: Dive to the Future – 01 ~ 26 ").Returns("free-dive-to-the-future");
            }
        }
    }
}
