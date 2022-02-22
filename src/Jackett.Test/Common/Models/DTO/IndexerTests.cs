using System.Linq;
using Jackett.Common.Models.DTO;
using Jackett.Test.TestHelpers;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Jackett.Test.Common.Models.DTO
{
    [TestFixture]
    public class IndexerTests
    {
        [Test]
        public void TestConstructor()
        {
            var indexer = new TestWebIndexer();

            var dto = new Indexer(indexer);
            Assert.AreEqual("test_id", dto.id);
            Assert.AreEqual("test_name", dto.name);
            Assert.AreEqual("test_description", dto.description);
            Assert.AreEqual("private", dto.type);
            Assert.False(dto.configured);
            Assert.AreEqual("https://test.link/", dto.site_link);
            Assert.AreEqual(2, dto.alternativesitelinks.ToList().Count);
            Assert.AreEqual("en-us", dto.language);
            Assert.AreEqual("", dto.last_error);
            Assert.False(dto.potatoenabled);
            Assert.AreEqual(0, dto.caps.ToList().Count);
        }

        [Test]
        public void TestConstructorWithCategories()
        {
            var indexer = new TestWebIndexer();
            indexer.AddTestCategories();

            // test Jackett UI categories (internal JSON)
            var dto = new Indexer(indexer);
            var dtoCaps = dto.caps.ToList();
            Assert.AreEqual(10, dtoCaps.Count);
            Assert.AreEqual("1000", dtoCaps[0].ID);
            Assert.AreEqual("1030", dtoCaps[1].ID);
            Assert.AreEqual("1040", dtoCaps[2].ID);
            Assert.AreEqual("2000", dtoCaps[3].ID);
            Assert.AreEqual("2030", dtoCaps[4].ID);
            Assert.AreEqual("7000", dtoCaps[5].ID);
            Assert.AreEqual("7030", dtoCaps[6].ID);
            Assert.AreEqual("137107", dtoCaps[7].ID);
            Assert.AreEqual("100044", dtoCaps[8].ID);
            Assert.AreEqual("100040", dtoCaps[9].ID);

            // movies categories enable potato search
            Assert.True(dto.potatoenabled);
        }
    }
}
