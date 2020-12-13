using Jackett.Common.Utils;
using Jackett.Test.TestHelpers;
using NUnit.Framework;

namespace Jackett.Test.Common.Utils
{
    [TestFixture]
    internal class ParseTvShowQualityTest : TestBase
    {
        [TestCase("Chuck.S04E05.HDTV.XviD-LOL", 5030)]
        [TestCase("Gold.Rush.S04E05.Garnets.or.Gold.REAL.REAL.PROPER.HDTV.x264-W4F", 5030)]
        [TestCase("Chuck.S03E17.REAL.PROPER.720p.HDTV.x264-ORENJI-RP", 5040)]
        [TestCase("Covert.Affairs.S05E09.REAL.PROPER.HDTV.x264-KILLERS", 5030)]
        [TestCase("Mythbusters.S14E01.REAL.PROPER.720p.HDTV.x264-KILLERS", 5040)]
        [TestCase("Orange.Is.the.New.Black.s02e06.real.proper.720p.webrip.x264-2hd", 5040)]
        [TestCase("Top.Gear.S21E07.Super.Duper.Real.Proper.HDTV.x264-FTP", 5030)]
        [TestCase("Top.Gear.S21E07.PROPER.HDTV.x264-RiVER-RP", 5030)]
        [TestCase("House.S07E11.PROPER.REAL.RERIP.1080p.BluRay.x264-TENEIGHTY", 5040)]
        [TestCase("The.Blacklist.S02E05", 5000)]
        [TestCase("The.IT.Crowd.S01.DVD.REMUX.DD2.0.MPEG2-DTG", 5030)]

        public void should_parse_quality_from_title(string title, int quality) => Assert.That(TvCategoryParser.ParseTvShowQuality(title), Is.EqualTo(quality));
    }
}

