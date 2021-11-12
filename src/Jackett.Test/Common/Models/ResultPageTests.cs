using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jackett.Common.Indexers;
using Jackett.Common.Models;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Jackett.Test.Common.Models
{
    class TestIndexer : BaseIndexer
    {
        public TestIndexer()
            : base(id: "test_id",
                   name: "test_name",
                   description: "test_description",
                   link: "https://test.link/",
                   configService: null,
                   logger: null,
                   configData: null,
                   p: null,
                   cs: null)
        {
        }

        public override TorznabCapabilities TorznabCaps { get; protected set; }
        public override Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson) => throw new NotImplementedException();
        protected override Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query) => throw new NotImplementedException();
    }

    [TestFixture]
    public class ResultPageTests
    {
        [Test]
        public void TestXmlWithInvalidCharacters()
        {
            // 0x1A can't be represented in XML => https://stackoverflow.com/a/8506173
            // some ascii and unicode characters
            var text = "Title Ñ 理" + Convert.ToChar("\u001a") + Convert.ToChar("\u2813");
            var validText = "Title Ñ 理" + Convert.ToChar("\u2813");

            // link with characters that requires URL encode
            var link = new Uri("https://example.com/" + text);
            var validLink = "https://example.com/Title%20%C3%91%20%E7%90%86%1A%E2%A0%93";

            var resultPage = new ResultPage(
                new ChannelInfo // characters in channel info are safe because are provided by us
                {
                    Link = link
                })
            {
                Releases = new List<ReleaseInfo>
                    {
                        new ReleaseInfo // these fields are from websites and they can be problematic
                        {
                            Title = text,
                            Guid = link,
                            Link = link,
                            Details = link,
                            PublishDate = new DateTime(2020, 09, 22),
                            Description = text,
                            Author = text,
                            BookTitle = text,
                            Poster = link,
                            InfoHash = text,
                            MagnetUri = link,
                            Origin = new TestIndexer()
                        }
                    }
            };
            var xml = resultPage.ToXml(link);

            Assert.AreEqual(5, Regex.Matches(xml, validText).Count);
            Assert.AreEqual(8, Regex.Matches(xml, validLink).Count);

            // this should be in another test but it's here to avoid creating the whole object again
            Assert.True(xml.Contains("Tue, 22 Sep 2020 00:00:00 "));
        }
    }
}
