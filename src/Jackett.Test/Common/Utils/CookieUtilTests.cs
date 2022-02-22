using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Jackett.Common.Utils;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using CollectionAssert = NUnit.Framework.CollectionAssert;

namespace Jackett.Test.Common.Utils
{
    [TestFixture]
    public class CookieUtilTests
    {
        [Test]
        public void CookieHeaderToDictionaryGood()
        {
            // valid cookies with non-alpha characters in the value
            var cookieHeader = "__cfduid=d6237f041586694295; __cf_bm=TlOng/xyqckk-TMen38z+0RFYA7YA=";
            var expectedCookieDictionary = new Dictionary<string, string>
            {
                {"__cfduid", "d6237f041586694295"}, {"__cf_bm", "TlOng/xyqckk-TMen38z+0RFYA7YA="}
            };
            CollectionAssert.AreEqual(expectedCookieDictionary, CookieUtil.CookieHeaderToDictionary(cookieHeader));
        }

        [Test]
        public void CookieHeaderToDictionaryDuplicateKeys()
        {
            // cookie with duplicate keys and whitespace separator instead of ;
            // this cookie is not valid according to the standard, but it occurs in Jackett because we are concatenating
            // cookies in many parts of the code (and we are not doing it well). this is safe because the whitespace
            // can't be part of the key nor the value.
            var cookieHeader = "__cfduid=d6237f041586694295; __cf_bm=TlOng/xyqckk-TMen38z+0RFYA7YA= __cf_bm=test";
            var expectedCookieDictionary = new Dictionary<string, string>
            {
                {"__cfduid", "d6237f041586694295"},
                {"__cf_bm", "test"} // we always assume the latest value is the most recent
            };
            CollectionAssert.AreEqual(expectedCookieDictionary, CookieUtil.CookieHeaderToDictionary(cookieHeader));
        }

        [Test]
        public void CookieHeaderToDictionaryMalformed()
        {
            // malformed cookies
            var cookieHeader = "__cfduidd6237f041586694295; __cf_;bm TlOng; good_cookie=value";
            var expectedCookieDictionary = new Dictionary<string, string> { { "good_cookie", "value" }, };
            CollectionAssert.AreEqual(expectedCookieDictionary, CookieUtil.CookieHeaderToDictionary(cookieHeader));
        }

        [Test]
        [SuppressMessage("ReSharper", "CollectionNeverUpdated.Local")]
        public void CookieHeaderToDictionaryNull()
        {
            // null cookie header
            var expectedCookieDictionary = new Dictionary<string, string>();
            CollectionAssert.AreEqual(expectedCookieDictionary, CookieUtil.CookieHeaderToDictionary(null));
        }

        [Test]
        public void CookieDictionaryToHeaderGood()
        {
            // valid cookies with non-alpha characters in the value
            var cookieDictionary = new Dictionary<string, string>
            {
                {"__cfduid", "d6237f041586694295"}, {"__cf_bm", "TlOng/xyqckk-TMen38z+0RFYA7YA="}
            };
            var expectedCookieHeader = "__cfduid=d6237f041586694295; __cf_bm=TlOng/xyqckk-TMen38z+0RFYA7YA=";
            CollectionAssert.AreEqual(expectedCookieHeader, CookieUtil.CookieDictionaryToHeader(cookieDictionary));
        }

        [Test]
        public void CookieDictionaryToHeaderMalformed1()
        {
            // malformed key
            var cookieDictionary = new Dictionary<string, string>
            {
                {"__cf_=bm", "34234234"}
            };
            var ex = Assert.Throws<FormatException>(() => CookieUtil.CookieDictionaryToHeader(cookieDictionary));
            Assert.AreEqual("The cookie '__cf_=bm=34234234' is malformed.", ex.Message);
        }

        [Test]
        public void CookieDictionaryToHeaderMalformed2()
        {
            // malformed value
            var cookieDictionary = new Dictionary<string, string>
            {
                {"__cf_bm", "34234 234"}
            };
            var ex = Assert.Throws<FormatException>(() => CookieUtil.CookieDictionaryToHeader(cookieDictionary));
            Assert.AreEqual("The cookie '__cf_bm=34234 234' is malformed.", ex.Message);
        }

        [Test]
        public void CookieDictionaryToHeaderNull()
        {
            // null cookie dictionary
            var expectedCookieHeader = "";
            CollectionAssert.AreEqual(expectedCookieHeader, CookieUtil.CookieDictionaryToHeader(null));
        }
    }
}
