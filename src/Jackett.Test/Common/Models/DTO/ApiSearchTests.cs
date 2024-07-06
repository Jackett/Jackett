using Jackett.Common.Models.DTO;
using NUnit.Framework;

namespace Jackett.Test.Common.Models.DTO
{
    [TestFixture]
    public class ApiSearchTests
    {
        [TestCase("The.Good.Lord.S01E05.720p.WEB.H264-CAKES", "The.Good.Lord.S01E05.720p.WEB.H264-CAKES", null, null)]
        [TestCase("The.Good.Lord.S01E05.", "The.Good.Lord.S01E05.", null, null)]
        [TestCase("The.Good.Lord.S01E05", "The.Good.Lord. S01E05", 1, "5")]
        [TestCase("The Good Lord S01E05", "The Good Lord S01E05", 1, "5")]
        [TestCase("The Good Lord S01 E05", "The Good Lord S01E05", 1, "5")]
        [TestCase("The Good Lord S01", "The Good Lord S01", 1, null)]
        [TestCase("The Good Lord E05", "The Good Lord", null, "5")]
        [TestCase("The.Good.Lord.s01e05", "The.Good.Lord.s01e05", null, null)]
        [TestCase("The.Good.Lord.S01e05", "The.Good.Lord.S01e05", null, null)]
        [TestCase("The.Good.Lord.s01E05", "The.Good.Lord.s01E05", null, null)]
        [TestCase("The.Good.Lord.S1E5", "The.Good.Lord.S1E5", null, null)]
        [TestCase("The.Good.Lord.S11E5", "The.Good.Lord.S11E5", null, null)]
        [TestCase("The.Good.Lord.S1E15", "The.Good.Lord.S1E15", null, null)]
        public void TestToTorznabQuery(string query, string expected, int? season, string episode)
        {
            var request = new ApiSearch { Query = query };
            var currentQuery = ApiSearch.ToTorznabQuery(request);

            Assert.AreEqual(expected, currentQuery.GetQueryString());
            Assert.AreEqual(season, currentQuery.Season);
            Assert.AreEqual(episode, currentQuery.Episode);
        }
    }
}
