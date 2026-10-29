using System.Collections.Generic;
using FluentAssertions;
using Jackett.Common.Helpers;
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
            var gbBytes = TamilMV.ParseSize("Baththa (2026) Tamil HDRip - 2.9GB - HC-ESub.mkv");
            gbBytes.Should().Be(2.9.Gigabytes());

            var mbBytes = TamilMV.ParseSize("Release - 700MB - x264.mkv");
            mbBytes.Should().Be(700.Megabytes());

            var kbBytes = TamilMV.ParseSize("Sample - 500KB.mkv");
            kbBytes.Should().Be(500L * 1024L);
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

        [TestCase("https://www.1tamilmv.style/index.php?/forums/forum/92-post-share-here/", true)]
        [TestCase("https://www.1tamilmv.style/forums/forum/92-post-share-here/", true)]
        [TestCase("/forums/forum/92-post-share-here/", true)]
        [TestCase("/forum/92-post-share-here/", true)]
        [TestCase("/16-video-songs/", true)]
        [TestCase("/15-mp3-songs/", true)]
        [TestCase("/18-cinema-news-reviews-general-videos/", true)]
        [TestCase("/102-watch-tamil-movies-online/", true)]
        [TestCase("104-watch-malayalam-movies-online/", true)]
        [TestCase("https://www.1tamilmv.style/index.php?/forums/forum/95-watch-telugu-movies-online/", true)]
        [TestCase("https://www.1tamilmv.style/index.php?/forums/forum/75-watch-kannada-movies-online/", true)]
        [TestCase("https://www.1tamilmv.style/index.php?/forums/forum/52-video-songs/", true)]
        [TestCase("https://www.1tamilmv.style/index.php?/forums/forum/62-mp3-songs/", true)]
        [TestCase("https://www.1tamilmv.style/index.php?/forums/forum/89-request-section/", true)]
        [TestCase("https://www.1tamilmv.style/index.php?/forums/forum/92/", true)]
        [TestCase("https://www.1tamilmv.style/index.php?/forums/topic/12345-some-topic/", false)]
        [TestCase("https://www.1tamilmv.style/index.php?/forums/forum/11-web-hd-itunes-hd-bluray/", false)]
        [TestCase("https://www.1tamilmv.style/index.php?/forums/forum/12-tamil-movies/", false)]
        public void TestIsExcludedForumUrl(string url, bool expected)
        {
            var result = TamilMV.IsExcludedForum(null, url);
            result.Should().Be(expected);
        }

        [Test]
        public void TestIsExcludedForumBreadcrumbs()
        {
            var parser = new AngleSharp.Html.Parser.HtmlParser();
            var docExcluded = parser.ParseDocument(@"
                <html>
                    <nav class='ipsBreadcrumb ipsBreadcrumb_top'>
                        <ul data-role='breadcrumbList'>
                            <li><a href='https://www.1tamilmv.style/'>Home</a></li>
                            <li><a href='https://www.1tamilmv.style/index.php?/forums/forum/92-post-share-here/'>Post & Share Here</a></li>
                            <li>Some User Torrent Title</li>
                        </ul>
                    </nav>
                </html>");
            TamilMV.IsExcludedForum(docExcluded, "https://www.1tamilmv.style/index.php?/forums/topic/99999-shared-release/").Should().BeTrue();

            var docAllowed = parser.ParseDocument(@"
                <html>
                    <nav class='ipsBreadcrumb ipsBreadcrumb_top'>
                        <ul data-role='breadcrumbList'>
                            <li><a href='https://www.1tamilmv.style/'>Home</a></li>
                            <li><a href='https://www.1tamilmv.style/index.php?/forums/forum/12-tamil-hd-movies/'>Tamil HD Movies</a></li>
                            <li>Official HD Release</li>
                        </ul>
                    </nav>
                </html>");
            TamilMV.IsExcludedForum(docAllowed, "https://www.1tamilmv.style/index.php?/forums/topic/99999-official-release/").Should().BeFalse();
        }
    }
}
