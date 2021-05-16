using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using Jackett.Common.Utils;
using NUnit.Framework;

namespace Jackett.Test.Common.Utils
{
    [TestFixture]
    public class StringUtilTests
    {
        [Test]
        public void GetQueryStringTests()
        {
            #region Encoding Tests

            //Add windows-1251 to Encoding list if not present
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var win1251 = Encoding.GetEncoding("windows-1251");
            const string encodingTestValue = "Ру́сский";
            var encodingNvc = new NameValueCollection
            {
                {"query", encodingTestValue},
                {"space key", "space value"}
            };
            // WebUtilityHelpers.UrlEncode(encodingTestValue, Encoding.UTF8);
            const string utf8Encoded = "%D0%A0%D1%83%CC%81%D1%81%D1%81%D0%BA%D0%B8%D0%B9";
            // WebUtilityHelpers.UrlEncode(encodingTestValue, win1251);
            const string win1251Encoded = "%D0%F3%3F%F1%F1%EA%E8%E9";

            //Default encoding is UTF-8
            StringAssert.Contains(utf8Encoded, encodingNvc.GetQueryString());

            //Null encoding reverts to default encoding (UTF-8)
            StringAssert.Contains(utf8Encoded, encodingNvc.GetQueryString(encoding: null));

            //Ensure non-default encoding is utilized
            StringAssert.Contains(win1251Encoded, encodingNvc.GetQueryString(encoding: win1251));

            //Encoding should make values websafe, but not keys
            StringAssert.Contains("space key=space+value", encodingNvc.GetQueryString());

            #endregion

            #region Separator Tests

            var separatorNvc = new NameValueCollection
            {
                {"one", "value"},
                {"two", "value2"}
            };

            //Ensure default value is "&"
            Assert.AreEqual("one=value&two=value2", separatorNvc.GetQueryString());

            //Ensure separator is overridden
            Assert.AreEqual("one=value;two=value2", separatorNvc.GetQueryString(separator: ";"));

            //Ensure behavior when string.IsNullOrEmpty(separator)
            const string noSeparator = "one=valuetwo=value2";
            Assert.AreEqual(noSeparator, separatorNvc.GetQueryString(separator: null));
            Assert.AreEqual(noSeparator, separatorNvc.GetQueryString(separator: string.Empty));

            #endregion

            #region Split Keys Tests

            var duplicateKeysNvc = new NameValueCollection
            {
                {"key1", "value"},
                {"key2", "value2"},
                {"key1", "duplicate"}
            };

            //Default should keep duplicated keys combined
            Assert.AreEqual("key1=value%2Cduplicate&key2=value2", duplicateKeysNvc.GetQueryString());

            //Ensure keys are combined when requested
            Assert.AreEqual(
                "key1=value%2Cduplicate&key2=value2", duplicateKeysNvc.GetQueryString(duplicateKeysIfMulti: false));

            //Ensure keys are separated when requested
            Assert.AreEqual(
                "key1=value&key1=duplicate&key2=value2", duplicateKeysNvc.GetQueryString(duplicateKeysIfMulti: true));

            #endregion

            #region Edge Case Tests

            //Throws NullReferenceException if the NameValueCollection is null in all cases
            Assert.Throws<NullReferenceException>(() => ((NameValueCollection)null).GetQueryString());

            //Returns empty string on empty collection in all cases
            Assert.AreEqual(string.Empty, new NameValueCollection().GetQueryString());

            #endregion
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

        [Test]
        public void FindSubstringsBetween_ValidEntries_Succeeds()
        {
            var stringParts = new string[] { "<test>", "<abc>", "<def>" };
            var source = string.Concat(stringParts);

            var results = source.FindSubstringsBetween('<', '>', true);

            CollectionAssert.AreEqual(stringParts, results);
        }

        [Test]
        public void FindSubstringsBetween_NestedEntries_Succeeds()
        {
            var stringParts = new string[] { "(test(abc))", "(def)", "(ghi)" };
            var source = string.Concat(stringParts);

            var results = source.FindSubstringsBetween('(', ')', false);

            var expectedParts = new string[] { "abc", "test(abc)", "def", "ghi" };
            CollectionAssert.AreEqual(expectedParts, results);
        }
    }
}
