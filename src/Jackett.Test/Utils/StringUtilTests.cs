using Jackett.Common.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Web;
using NUnit.Framework;

namespace Jackett.Common.Utils.Tests
{
    [TestFixture]
    public class StringUtilTests
    {
        [Test]
        public void GetQueryStringTests()
        {
            //Initial Setup
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var encodings = new[]
            {
                Encoding.UTF8,
                Encoding.ASCII,
                Encoding.GetEncoding("iso-8859-1"),
                Encoding.GetEncoding("windows-1255"),
                Encoding.GetEncoding("windows-1252"),
                Encoding.GetEncoding("windows-1251"),
                null
            };
            var queries = new[]
            {
                "test",
                "españa",
                "Ру́сский",
                "Harry Potter",
                "dark&night"
            };
            var testCase = new NameValueCollection
            {
                {"st", "troy"},
                {"makin", "thisup"},
                {"st", "duplicate"}
            };
            const string combinedTemplate = "st=troy%2cduplicate{0}makin=thisup{0}q={1}";
            const string duplicateKeysTempalate = "st=troy{0}st=duplicate{0}makin=thisup{0}q={1}";
            foreach (var encoding in encodings)
                foreach (var query in queries)
                {
                    testCase["q"] = query;
                    var parsedEncoding = encoding ?? Encoding.UTF8;
                    var parsedQuery = HttpUtility.UrlEncode(query, parsedEncoding);

                    //Combined keys
                    StringAssert.AreEqualIgnoringCase(
                        string.Format(combinedTemplate, "&", parsedQuery), testCase.GetQueryString(encoding));
                    StringAssert.AreEqualIgnoringCase(
                        string.Format(combinedTemplate, ";", parsedQuery),
                        testCase.GetQueryString(encoding, separator: ";"));
                    StringAssert.AreEqualIgnoringCase(
                        string.Format(combinedTemplate, null, parsedQuery),
                        testCase.GetQueryString(encoding, separator: string.Empty));
                    StringAssert.AreEqualIgnoringCase(
                        string.Format(combinedTemplate, null, parsedQuery),
                        testCase.GetQueryString(encoding, separator: null));

                    //Separated keys
                    StringAssert.AreEqualIgnoringCase(
                        string.Format(duplicateKeysTempalate, "&", parsedQuery),
                        testCase.GetQueryString(encoding, true));
                    StringAssert.AreEqualIgnoringCase(
                        string.Format(duplicateKeysTempalate, ";", parsedQuery),
                        testCase.GetQueryString(encoding, true, ";"));
                    StringAssert.AreEqualIgnoringCase(
                        string.Format(duplicateKeysTempalate, null, parsedQuery),
                        testCase.GetQueryString(encoding, separator: string.Empty, duplicateKeysIfMulti: true));
                    StringAssert.AreEqualIgnoringCase(
                        string.Format(duplicateKeysTempalate, null, parsedQuery),
                        testCase.GetQueryString(encoding, separator: null, duplicateKeysIfMulti: true));
                }

            Assert.Throws<NullReferenceException>(() => ((NameValueCollection)null).GetQueryString());
            Assert.AreEqual(string.Empty, new NameValueCollection().GetQueryString());
        }

        [Test]
        public void ToEnumerableTest()
        {
            var original = new NameValueCollection
            {
                {"first", "firstVal"},
                {"second", "secondVal"},
                {"third", "thirdVal"},
                {"second", "anotherVal"}
            };
            var combined = new[]
            {
                new KeyValuePair<string, string>("first", "firstVal"),
                new KeyValuePair<string, string>("second", "secondVal,anotherVal"),
                new KeyValuePair<string, string>("third", "thirdVal")
            };
            var duplicateKeys = new[]
            {
                new KeyValuePair<string, string>("first", "firstVal"),
                new KeyValuePair<string, string>("second", "secondVal"),
                new KeyValuePair<string, string>("second", "anotherVal"),
                new KeyValuePair<string, string>("third", "thirdVal")
            };
            CollectionAssert.AreEqual(combined, original.ToEnumerable());
            CollectionAssert.AreEqual(duplicateKeys, original.ToEnumerable(true));
        }
    }
}
