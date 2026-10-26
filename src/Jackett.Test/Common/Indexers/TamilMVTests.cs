using System.Collections.Generic;
using FluentAssertions;
using Jackett.Common.Indexers.Definitions;
using Jackett.Common.Models;
using NUnit.Framework;

namespace Jackett.Test.Common.Indexers
{
    [TestFixture]
    public class TamilMVTests
    {
        [TestCase("www.1TamilMV.solar - Baththa (2026) Tamil HDRip - 1080p - x264 - AAC - 2.9GB - HC-ESub.mkv.torrent",
                  "Baththa (2026) Tamil HDRip - 1080p - x264 - AAC - 2.9GB - HC-ESub.mkv")]
        [TestCase("1TamilMV.pm - The Marked Woman (2026) HD - 720p - x264 - 1.4GB.torrent",
                  "The Marked Woman (2026) HD - 720p - x264 - 1.4GB")]
        [TestCase("Teach You a Lesson (2026) S01 EP (01-08) TRUE WEB-DL - 1080p.mkv.torrent",
                  "Teach You a Lesson (2026) S01 EP (01-08) TRUE WEB-DL - 1080p.mkv")]
        public void TestCleanTitle(string raw, string expected)
        {
            var clean = TamilMV.CleanTitle(raw, stripPrefix: true);
            clean.Should().Be(expected);
        }

        [Test]
        public void TestParseSize()
        {
            var (gbBytes, gbHuman) = TamilMV.ParseSize("Baththa (2026) Tamil HDRip - 2.9GB - HC-ESub.mkv");
            gbBytes.Should().Be((long)(2.9 * 1024L * 1024L * 1024L));
            gbHuman.Should().Be("2.9 GB");

            var (mbBytes, mbHuman) = TamilMV.ParseSize("Release - 700MB - x264.mkv");
            mbBytes.Should().Be(700L * 1024L * 1024L);
            mbHuman.Should().Be("700 MB");

            var (kbBytes, kbHuman) = TamilMV.ParseSize("Sample - 500KB.mkv");
            kbBytes.Should().Be(500L * 1024L);
            kbHuman.Should().Be("500 KB");
        }

        [Test]
        public void TestDetermineCategoriesMovieHD()
        {
            var cats = TamilMV.DetermineCategories("Baththa (2026) Tamil HDRip - 1080p - x264 - 2.9GB.mkv");
            cats.Should().Contain(TorznabCatType.Movies.ID);
            cats.Should().Contain(TorznabCatType.MoviesHD.ID);
            cats.Should().Contain(TorznabCatType.MoviesForeign.ID);
        }

        [Test]
        public void TestDetermineCategoriesMovieUHD()
        {
            var cats = TamilMV.DetermineCategories("Baththa (2026) Tamil WEB-DL - 4K - 2160p - 20GB.mkv");
            cats.Should().Contain(TorznabCatType.Movies.ID);
            cats.Should().Contain(TorznabCatType.MoviesUHD.ID);
        }

        [Test]
        public void TestDetermineCategoriesTV()
        {
            var cats = TamilMV.DetermineCategories("Teach You a Lesson (2026) S01 EP (01-08) TRUE WEB-DL - 720p - 1.4GB");
            cats.Should().Contain(TorznabCatType.TV.ID);
            cats.Should().Contain(TorznabCatType.TVHD.ID);
            cats.Should().Contain(TorznabCatType.TVForeign.ID);
        }

        [Test]
        public void TestDetermineCategoriesAudio()
        {
            var cats = TamilMV.DetermineCategories("Baththa (2026) Original Motion Picture Soundtrack Hi-Fi FLAC - 190 MB");
            cats.Should().Contain(TorznabCatType.Audio.ID);
            cats.Should().Contain(TorznabCatType.AudioLossless.ID);
        }
    }
}
