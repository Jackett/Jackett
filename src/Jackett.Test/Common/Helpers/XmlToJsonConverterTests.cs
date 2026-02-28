using System.Xml.Linq;
using Jackett.Common.Helpers;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Jackett.Test.Common.Helpers
{
    [TestFixture]
    public class XmlToJsonConverterTests
    {
        [Test]
        public void XmlToJsonConverter_XmlToJson_Empty()
        {
            var element = new XElement("test");
            var json = XmlToJsonConverter.XmlToJson(element);
            Assert.AreEqual("{}", json.ToString(Formatting.None));
        }

        [Test]
        public void XmlToJsonConverter_XmlToJson_WithAttributes()
        {
            var element = new XElement("test", new XAttribute("attr1", "value1"), new XAttribute("attr2", "value2"));
            var json = XmlToJsonConverter.XmlToJson(element);
            Assert.AreEqual("{\"@attributes\":{\"attr1\":\"value1\",\"attr2\":\"value2\"}}", json.ToString(Formatting.None));
        }

        [Test]
        public void XmlToJsonConverter_XmlToJson_WithChildElement()
        {
            var element = new XElement("test", new XElement("child"));
            var json = XmlToJsonConverter.XmlToJson(element);
            Assert.AreEqual("{\"child\":{}}", json.ToString(Formatting.None));
        }

        [Test]
        public void XmlToJsonConverter_XmlToJson_WithChildElement_WithAttributes()
        {
            var element = new XElement("test", new XElement("child", new XAttribute("attr1", "value1"), new XAttribute("attr2", "value2")));
            var json = XmlToJsonConverter.XmlToJson(element);
            Assert.AreEqual("{\"child\":{\"@attributes\":{\"attr1\":\"value1\",\"attr2\":\"value2\"}}}", json.ToString(Formatting.None));
        }

        [Test]
        public void XmlToJsonConverter_XmlToJson_WithChildElement_WithText()
        {
            var element = new XElement("test", new XElement("child", new XElement("subchild", "text")));
            var json = XmlToJsonConverter.XmlToJson(element);
            Assert.AreEqual("{\"child\":{\"subchild\":\"text\"}}", json.ToString(Formatting.None));
        }

        [Test]
        public void XmlToJsonConverter_XmlToJson_WithChildElementArray()
        {
            var element = new XElement("test", new XElement("child"), new XElement("child"));
            var json = XmlToJsonConverter.XmlToJson(element);
            Assert.AreEqual("{\"child\":[{},{}]}", json.ToString(Formatting.None));
        }

        [Test]
        public void XmlToJsonConverter_XmlToJson_WithChildElementArray_WithText()
        {
            var element = new XElement("test", new XElement("child", "text1"), new XElement("child", "text2"));
            var json = XmlToJsonConverter.XmlToJson(element);
            Assert.AreEqual("{\"child\":[\"text1\",\"text2\"]}", json.ToString(Formatting.None));
        }

        [Test]
        public void XmlToJsonConverter_XmlToJson_HandlesNamespaces()
        {
            var document = new XDocument(
                new XElement("root",
                    new XAttribute(XNamespace.Xmlns + "ns1", "http://example.com/ns1"),
                    new XAttribute(XNamespace.Xmlns + "ns2", "http://example.com/ns2"),
                    new XElement("{http://example.com/ns1}child1"),
                    new XElement("{http://example.com/ns2}child2")
                )
            );

            var json = XmlToJsonConverter.XmlToJson(document.Root);
            Assert.AreEqual("{\"@attributes\":{\"xmlns:ns1\":\"http://example.com/ns1\",\"xmlns:ns2\":\"http://example.com/ns2\"},\"ns1:child1\":{},\"ns2:child2\":{}}", json.ToString(Formatting.None));
        }
    }
}
