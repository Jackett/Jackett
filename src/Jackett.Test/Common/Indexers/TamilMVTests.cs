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

        [TestCase("Leo (2023) Tamil Proper TRUE WEB-DL", "leo-2023-tamil-proper-true-web-dl")]
        [TestCase("Jackie Chan's The Tuxedo (2002)", "jackie-chans-the-tuxedo-2002")]
        [TestCase("Movie @ 2024 #Special!", "movie-2024-special")]
        [TestCase("Baththa (2026) Tamil TRUE WEB-DL - [4K, 1080p & 720p - AVC / HEVC - (DD+5.1 - 640Kbps) - 17GB - 4GB - 1.4GB & 950MB] - [x264 - 700MB - 400MB & 250MB] - ESub",
                  "baththa-2026-tamil-true-web-dl-4k-1080p-720p-avc-hevc-dd51-640kbps-17gb-4gb-14gb-950mb-x264-700mb-400mb-250mb-esub")]
        [TestCase("", "t")]
        public void TestSlugify(string rawTitle, string expectedSlug)
        {
            var slug = TamilMV.Slugify(rawTitle);
            slug.Should().Be(expectedSlug);
        }

        [Test]
        public void TestParseSearchApiResponse_OnlyScrapesNonNullStarterRole()
        {
            var json = @"{
                ""results"": [
                    {
                        ""tid"": 176409,
                        ""title"": ""Leo (2023) Official Release"",
                        ""starter_role"": ""Moderator""
                    },
                    {
                        ""tid"": 193696,
                        ""title"": ""Leo (2023) User Post"",
                        ""starter_role"": null
                    },
                    {
                        ""tid"": 176106,
                        ""title"": ""Leo (2023) Elite Release"",
                        ""starter_role"": ""Elite""
                    },
                    {
                        ""tid"": 189999,
                        ""title"": ""Leo (2023) Empty Role Post"",
                        ""starter_role"": """"
                    },
                    {
                        ""tid"": 188888,
                        ""title"": ""Leo (2023) Missing Role Post""
                    }
                ],
                ""total"": 5,
                ""page"": 1,
                ""per_page"": 50,
                ""page_count"": 1
            }";

            var urls = TamilMV.ParseSearchApiResponse(json, "https://www.1tamilmv.style/");
            urls.Should().HaveCount(2);
            urls[0].Should().Be("https://www.1tamilmv.style/index.php?/forums/topic/176409-leo-2023-official-release/");
            urls[1].Should().Be("https://www.1tamilmv.style/index.php?/forums/topic/176106-leo-2023-elite-release/");
        }

        [Test]
        public void TestParseSearchApiResponse_EmptyOrInvalid()
        {
            TamilMV.ParseSearchApiResponse("", "https://www.1tamilmv.style/").Should().BeEmpty();
            TamilMV.ParseSearchApiResponse("   ", "https://www.1tamilmv.style/").Should().BeEmpty();
            TamilMV.ParseSearchApiResponse(@"{""results"":[], ""total"":0}", "https://www.1tamilmv.style/").Should().BeEmpty();
        }


        [TestCase("The Truman Show (1998) 1080p BluRay", false)]
        [TestCase("The Daily Show S29E50 720p", false)]
        [TestCase("Regular Show S01 1080p", false)]
        [TestCase("The Great Indian Kapil Show S01", false)]
        [TestCase("The Interview (2014) 1080p BluRay", false)]
        [TestCase("Song of the Sea (2014) 1080p", false)]
        [TestCase("Swan Song (2021) 720p", false)]
        [TestCase("Trailer Park Boys (2006) 720p", false)]
        [TestCase("Show Dogs (2018) 1080p", false)]
        [TestCase("Leo (2023) Tamil Proper TRUE WEB-DL - [4K, 1080p & 720p - AVC / HEVC]", false)]
        [TestCase("Sundarakanda - Official Trailer | Nara Rohith", true)]
        [TestCase("#Single Trailer | #SingleOnMay9th", true)]
        [TestCase("Dragon Telugu Trailer", true)]
        [TestCase("Leo Success Meet (2023) Tamil Full Show - [1080p & 720p]", true)]
        [TestCase("Leo Success Meet (2023) Thalapathy Vijay Speech - [1080p]", true)]
        [TestCase("Leo (2023) Tamil Full Album - [FLAC - 24Bit & 16Bit]", true)]
        [TestCase("Dragon (2025) Original Motion Picture Soundtrack Master Quality 24bit - FLAC", true)]
        [TestCase("Leo (2023) - Video Songs - TRUE WEB-DL - 4K SDR - HEVC", true)]
        [TestCase("Leo - Villain Yaaru Video Song - 1080P - DD5.1", true)]
        [TestCase("Vazhithunaiye Video Song | Dragon | Pradeep Ranganathan", true)]
        [TestCase("Anbenum (From Leo) 3rd Single - (2023) Tamil MP3", true)]
        [TestCase("Badass (From Leo) 2nd Single - (2023) Tamil MP3", true)]
        [TestCase("Tu Mera Lover (From Mass Jathara) Telugu - Single Track - FLAC", true)]
        [TestCase("Movie Teaser", true)]
        [TestCase("Movie Sample.mkv", true)]
        [TestCase("Movie Promo", true)]
        public void TestExcludedTitles(string title, bool shouldExclude)
        {
            TamilMV.ExcludedTitles.IsMatch(title).Should().Be(shouldExclude);
        }
    }
}
