using System.Collections.Generic;
using AngleSharp.Dom;
using Jackett.Common.Indexers;
using Jackett.Common.Indexers.Definitions.Abstract;
using Jackett.Common.Models;
using NUnit.Framework;

namespace Jackett.Test.Common.Indexers.Abstract
{
    [TestFixture]
    public class PublicBrazilianParserCleanTitleTests
    {
        private class TestableParser : PublicBrazilianParser
        {
            public static string Invoke(string title) => CleanTitle(title);
            public override IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse) => throw new System.NotImplementedException();
            protected override INode GetTitleElementOrNull(IElement downloadButton) => throw new System.NotImplementedException();
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("\t\n")]
        public void Returns_Null_For_NullOrWhitespace(string input)
        {
            Assert.That(TestableParser.Invoke(input), Is.Null);
        }

        [TestCase("Inception", "Inception")]
        [TestCase("The Lord of the Rings", "The Lord of the Rings")]
        [TestCase("Cidade de Deus", "Cidade de Deus")]
        public void Preserves_Clean_Titles(string input, string expected)
        {
            Assert.That(TestableParser.Invoke(input), Is.EqualTo(expected));
        }

        [TestCase("Inception (2.5 GB)", "Inception")]
        [TestCase("Inception (700 MB)", "Inception")]
        [TestCase("Inception (450KB)", "Inception")]
        [TestCase("Inception (1.2 GB) extras", "Inception extras")]
        public void Removes_Size_Info_In_Parentheses(string input, string expected)
        {
            Assert.That(TestableParser.Invoke(input), Is.EqualTo(expected));
        }

        [TestCase("Inception HIDRATORRENTS.ORG", "Inception")]
        [TestCase("Inception COMANDO4K.COM", "Inception")]
        [TestCase("Inception www.comando.to", "Inception")]
        [TestCase("Inception https://example.com", "Inception")]
        [TestCase("Inception http://www.bludv.com", "Inception")]
        [TestCase("Inception [vacatorrent.com]", "Inception")]
        [TestCase("Inception VEMTORRENT.COM extras", "Inception extras")]
        public void Removes_URLs(string input, string expected)
        {
            Assert.That(TestableParser.Invoke(input), Is.EqualTo(expected));
        }

        [TestCase("Inception 480p", "Inception")]
        [TestCase("Inception 720p", "Inception")]
        [TestCase("Inception 1080p", "Inception")]
        [TestCase("Inception 2160p", "Inception")]
        [TestCase("Inception 4K", "Inception")]
        [TestCase("Inception UHD", "Inception")]
        [TestCase("Inception HDR10", "Inception")]
        public void Removes_Resolutions(string input, string expected)
        {
            Assert.That(TestableParser.Invoke(input), Is.EqualTo(expected));
        }

        [TestCase("Inception x264", "Inception")]
        [TestCase("Inception x265", "Inception")]
        [TestCase("Inception H.264", "Inception")]
        [TestCase("Inception H264", "Inception")]
        [TestCase("Inception HEVC", "Inception")]
        [TestCase("Inception XviD", "Inception")]
        public void Removes_Video_Codecs(string input, string expected)
        {
            Assert.That(TestableParser.Invoke(input), Is.EqualTo(expected));
        }

        [TestCase("Inception AAC", "Inception")]
        [TestCase("Inception AC3", "Inception")]
        [TestCase("Inception DTS", "Inception")]
        [TestCase("Inception FLAC", "Inception")]
        [TestCase("Inception Atmos", "Inception")]
        public void Removes_Audio_Codecs(string input, string expected)
        {
            Assert.That(TestableParser.Invoke(input), Is.EqualTo(expected));
        }

        [TestCase("Inception WEB-DL", "Inception")]
        [TestCase("Inception WEBDL", "Inception")]
        [TestCase("Inception BluRay", "Inception")]
        [TestCase("Inception Blu-Ray", "Inception")]
        [TestCase("Inception BDRip", "Inception")]
        [TestCase("Inception WEBRip", "Inception")]
        [TestCase("Inception HDTV", "Inception")]
        [TestCase("Inception REMUX", "Inception")]
        public void Removes_Sources(string input, string expected)
        {
            Assert.That(TestableParser.Invoke(input), Is.EqualTo(expected));
        }

        [TestCase("Inception Dublado", "Inception")]
        [TestCase("Inception Legendado", "Inception")]
        [TestCase("Inception Dual Audio", "Inception")]
        [TestCase("Inception Multi", "Inception")]
        [TestCase("Inception Nacional", "Inception")]
        [TestCase("Inception PT-BR", "Inception")]
        [TestCase("Inception PTBR", "Inception")]
        public void Removes_Language_Tags(string input, string expected)
        {
            Assert.That(TestableParser.Invoke(input), Is.EqualTo(expected));
        }

        [TestCase("Inception MKV", "Inception")]
        [TestCase("Inception MP4", "Inception")]
        [TestCase("Inception AVI", "Inception")]
        [TestCase("Inception WEBM", "Inception")]
        public void Removes_File_Extensions(string input, string expected)
        {
            Assert.That(TestableParser.Invoke(input), Is.EqualTo(expected));
        }

        [TestCase("[Erai-raws] Naruto", "Naruto")]
        [TestCase("Naruto [Anime Time]", "Naruto")]
        [TestCase("Naruto (Episode 1)", "Naruto")]
        [TestCase("[Group] Movie [Other Group]", "Movie")]
        public void Removes_Bracket_Content(string input, string expected)
        {
            Assert.That(TestableParser.Invoke(input), Is.EqualTo(expected));
        }

        [TestCase("Movie.Title.Here", "Movie Title Here")]
        [TestCase("Some.Random.Series", "Some Random Series")]
        public void Replaces_Dots_Between_Words_With_Space(string input, string expected)
        {
            Assert.That(TestableParser.Invoke(input), Is.EqualTo(expected));
        }

        [TestCase("App v1.2.3", "App v1.2.3")]
        public void Preserves_Dots_Between_Digits(string input, string expected)
        {
            Assert.That(TestableParser.Invoke(input), Is.EqualTo(expected));
        }

        [TestCase("Inception -", "Inception")]
        [TestCase("Inception |", "Inception")]
        [TestCase("Inception _", "Inception")]
        [TestCase("Inception ~", "Inception")]
        [TestCase("- Inception -", "Inception")]
        public void Trims_Trailing_Punctuation(string input, string expected)
        {
            Assert.That(TestableParser.Invoke(input), Is.EqualTo(expected));
        }

        [TestCase("Movie | 1080p | x264", "Movie")]
        [TestCase("Movie / Director's Cut", "Movie Director's Cut")]
        [TestCase("Movie~Director's Cut", "Movie Director's Cut")]
        public void Replaces_Separators_With_Space(string input, string expected)
        {
            Assert.That(TestableParser.Invoke(input), Is.EqualTo(expected));
        }

        [Test]
        public void Collapses_Multiple_Spaces()
        {
            Assert.That(
                TestableParser.Invoke("Movie    With   Spaces"),
                Is.EqualTo("Movie With Spaces"));
        }

        [Test]
        public void Cleans_Typical_Movie_Release_Name()
        {
            const string input = "Inception 2010 1080p BluRay x264 Dublado COMANDO4K.COM";
            Assert.That(TestableParser.Invoke(input), Is.EqualTo("Inception 2010"));
        }

        [Test]
        public void Cleans_Typical_Series_Episode_Name()
        {
            const string input = "Stranger.Things.S04E01.1080p.WEB-DL.x264.Dual.Audio";
            Assert.That(TestableParser.Invoke(input), Is.EqualTo("Stranger Things S04E01"));
        }

        [Test]
        public void Cleans_Anime_Release_Name()
        {
            const string input = "[Erai-raws] One Piece - 1080p WEB-DL AAC HEVC.mkv";
            Assert.That(TestableParser.Invoke(input), Is.EqualTo("One Piece"));
        }

        [Test]
        public void Cleans_Release_With_Size_And_URL()
        {
            const string input = "Filme Aleatório (1.5 GB) 1080p HIDRATORRENTS.ORG";
            Assert.That(TestableParser.Invoke(input), Is.EqualTo("Filme Aleatório"));
        }

        [Test]
        public void Cleans_Release_With_Multiple_Bracket_Groups()
        {
            const string input = "[Group1] Movie Title [1080p] (BluRay) [x264-RARBG]";
            Assert.That(TestableParser.Invoke(input), Is.EqualTo("Movie Title"));
        }
    }
}
