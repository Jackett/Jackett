using System;
using Jackett.Common.Indexers.Definitions;
using Jackett.Common.Models;
using Jackett.Common.Utils;
using NUnit.Framework;

namespace Jackett.Test.Common.Indexers.PelisPanda
{
    [TestFixture]
    public class PelisPandaHelperTests
    {
        [TestCase("Hero of Liberty", 2024, "1080p", "Latino", null, null, ExpectedResult = "Hero of Liberty (2024) 1080p Latino")]
        [TestCase("Stranger Things", 2026, "720p", "Castellano", 1, 5, ExpectedResult = "Stranger Things S01E05 (2026) 720p Castellano")]
        [TestCase("Bare", null, null, null, null, null, ExpectedResult = "Bare")]
        [TestCase("With  Internal   Spaces", 2024, "1080p", null, null, null, ExpectedResult = "With Internal Spaces (2024) 1080p")]
        [TestCase("NoLang", 2024, "1080p", "", null, null, ExpectedResult = "NoLang (2024) 1080p")]
        public string FormatTitle_VariousInputs_FormatsCorrectly(
            string titleBase, object yearObj, string quality, string language, object seasonObj, object episodeObj)
        {
            int? year = yearObj == null ? (int?)null : Convert.ToInt32(yearObj);
            int? season = seasonObj == null ? (int?)null : Convert.ToInt32(seasonObj);
            int? episode = episodeObj == null ? (int?)null : Convert.ToInt32(episodeObj);
            return PelisPandaParser.FormatTitle(titleBase, year, quality, language, season, episode);
        }

        [TestCase("720p",  ExpectedResult = 1L * 1024 * 1024 * 1024)]
        [TestCase("1080p", ExpectedResult = (long)(2.5 * 1024 * 1024 * 1024))]
        [TestCase("2160p", ExpectedResult = 5L * 1024 * 1024 * 1024)]
        [TestCase("4K",    ExpectedResult = 512L * 1024 * 1024)]
        [TestCase("",      ExpectedResult = 512L * 1024 * 1024)]
        [TestCase(null,    ExpectedResult = 512L * 1024 * 1024)]
        public long EstimateSizeFromQuality_AllResolutions(string quality) =>
            PelisPandaParser.EstimateSizeFromQuality(quality);

        [Test]
        public void ResolveSize_WithExplicitSize_UsesParseUtil()
        {
            var bytes = PelisPandaParser.ResolveSize("3.80 GB", "1080p");
            Assert.That(bytes, Is.EqualTo(ParseUtil.GetBytes("3.80 GB")));
        }

        [Test]
        public void ResolveSize_WithEmptySize_FallsBackToResolutionEstimate()
        {
            var bytes = PelisPandaParser.ResolveSize(null, "1080p");
            Assert.That(bytes, Is.EqualTo((long)(2.5 * 1024 * 1024 * 1024)));
        }

        [Test]
        public void ResolveSize_WithExplicitSize_DoesNotFallBackToEstimate()
        {
            var bytes = PelisPandaParser.ResolveSize("3.80 GB", "720p");
            Assert.That(bytes, Is.EqualTo(ParseUtil.GetBytes("3.80 GB")));
            Assert.That(bytes, Is.Not.EqualTo(1L * 1024 * 1024 * 1024));
        }

        [Test]
        public void ResolvePublishDate_ValidYyyymmdd_ParsesIt()
        {
            var d = PelisPandaParser.ResolvePublishDate("20240115", 2024);
            Assert.That(d, Is.EqualTo(new DateTime(2024, 1, 15)));
        }

        [Test]
        public void ResolvePublishDate_InvalidDateString_FallsBackToYearJan1()
        {
            var d = PelisPandaParser.ResolvePublishDate("not-a-date", 2023);
            Assert.That(d, Is.EqualTo(new DateTime(2023, 1, 1)));
        }

        [Test]
        public void ResolvePublishDate_NullDateNullYear_FallsBackToToday()
        {
            var d = PelisPandaParser.ResolvePublishDate(null, null);
            Assert.That(d.Date, Is.EqualTo(DateTime.Today));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(10000)]
        public void ResolvePublishDate_OutOfRangeYear_FallsBackToToday(int year)
        {
            var d = PelisPandaParser.ResolvePublishDate(null, year);
            Assert.That(d.Date, Is.EqualTo(DateTime.Today));
        }

        [TestCase("pelicula", ExpectedResult = 2000)]
        [TestCase("anime",    ExpectedResult = 5070)]
        [TestCase("serie",    ExpectedResult = 5000)]
        [TestCase("documental", ExpectedResult = 0)]
        [TestCase(null,       ExpectedResult = 0)]
        public int MapCategory_KnownAndUnknownTypes(string type) =>
            PelisPandaParser.MapCategory(type);

        [Test]
        public void FirstNonEmpty_PicksFirstNonWhitespace()
        {
            Assert.That(PelisPandaParser.FirstNonEmpty(null, "", "  ", "winner", "loser"), Is.EqualTo("winner"));
            Assert.That(PelisPandaParser.FirstNonEmpty(null, null), Is.Null);
        }
    }
}
