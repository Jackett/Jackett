using System.Collections.Generic;
using Jackett.Common.Indexers.Definitions;
using Jackett.Common.Models;
using NUnit.Framework;

namespace Jackett.Test.Common.Indexers.ArabP2P
{
    [TestFixture]
    public class ArabP2PTests
    {
        [TestCaseSource(typeof(TitleParserTestData), nameof(TitleParserTestData.TestCases))]
        public string TestTitleParser(string input)
        {
            var titleParser = new Jackett.Common.Indexers.Definitions.ArabP2P.TitleParser();
            return titleParser.Parse(input);
        }
    }

    public class TitleParserTestData
    {
        public static IEnumerable<TestCaseData> TestCases
        {
            get
            {
                // Turkish series with Arabic names
                yield return new TestCaseData("هذا البحر سوف يفيض [09][م1][HP][1080p][H264] This Sea Will Overflow AKA Tasacak Bu Deniz")
                    .Returns("هذا البحر سوف يفيض S01E09 [HP][1080p][H264] This Sea Will Overflow AKA Tasacak Bu Deniz");

                yield return new TestCaseData("المدينة البعيدة الموسم الثاني [209-210][2025][م2][SHAHID][1080p][H264] Uzak Sehir AKA Far Away")
                    .Returns("المدينة البعيدة الموسم الثاني S02E209-E210 [2025][SHAHID][1080p][H264] Uzak Sehir AKA Far Away");

                yield return new TestCaseData("حلم أشرف [35-36-37][م2][AMZN][1080p][H264] Esref Ruya")
                    .Returns("حلم أشرف S02E35-E36-E37 [AMZN][1080p][H264] Esref Ruya");

                // Episode packs separated by spaces
                yield return new TestCaseData("هذا البحر سوف يفيض [05 06][م1][HP][1080p][H264] This Sea Will Overflow AKA Tasacak Bu Deniz")
                    .Returns("هذا البحر سوف يفيض S01E05-E06 [HP][1080p][H264] This Sea Will Overflow AKA Tasacak Bu Deniz");

                yield return new TestCaseData("عيناك كالبحر الأسود [63 64 65][م1][SHAHID][1080p][H264] Black Sea Eyes AKA Gozleri Karadeniz")
                    .Returns("عيناك كالبحر الأسود S01E63-E64-E65 [SHAHID][1080p][H264] Black Sea Eyes AKA Gozleri Karadeniz");

                yield return new TestCaseData("عيناك كالبحر الأسود [56 57 58 59][م1][SHAHID][1080p][H264] Black Sea Eyes AKA Gozleri Karadeniz")
                    .Returns("عيناك كالبحر الأسود S01E56-E57-E58-E59 [SHAHID][1080p][H264] Black Sea Eyes AKA Gozleri Karadeniz");

                // Arabic series
                yield return new TestCaseData("لا ترد ولا تستبدل [04-05-06][2025][م1][SHAHID][2160p][HEVC]")
                    .Returns("لا ترد ولا تستبدل S01E04-E05-E06 [2025][SHAHID][2160p][HEVC]");

                yield return new TestCaseData("2 قهوي [15][م1][WATCH IT][1080p][H264]")
                    .Returns("2 قهوي S01E15 [WATCH IT][1080p][H264]");

                yield return new TestCaseData("أنا حرة [30][م1][SHAHID][4K]")
                    .Returns("أنا حرة S01E30 [SHAHID][4K]");

                yield return new TestCaseData("مش مهم الاسم [09][م1][SHAHID][1080p][H264]")
                    .Returns("مش مهم الاسم S01E09 [SHAHID][1080p][H264]");

                // Arabic with English name
                yield return new TestCaseData("ذا فويس [08] [2025][م6][SHAHID][1080p][H264] The voice S06")
                    .Returns("ذا فويس S06E08 [2025][SHAHID][1080p][H264] The voice S06");

                yield return new TestCaseData("توب شيف [03] [2025][م9][SHAHID][1080p][H264] Top Chef S09")
                    .Returns("توب شيف S09E03 [2025][SHAHID][1080p][H264] Top Chef S09");

                // English (non-Arabic) - should not be modified
                yield return new TestCaseData("It: Welcome to Derry [2025][S01][OSN][1080p][H264]")
                    .Returns("It: Welcome to Derry [2025][S01][OSN][1080p][H264]");

                // Arabic Movies (no episodes) - should not be modified
                yield return new TestCaseData("[حليمو أسطورة الشواطئ [STARZ]][2017][1080p][Web-DL]")
                    .Returns("[حليمو أسطورة الشواطئ [STARZ]][2017][1080p][Web-DL]");

                // Non-Arabic movies - should not be modified
                yield return new TestCaseData("[مترجم]Sisu: Road to Revenge [2025][WEBRip][1080p][H265]")
                    .Returns("[مترجم]Sisu: Road to Revenge [2025][WEBRip][1080p][H265]");
            }
        }
    }
}
