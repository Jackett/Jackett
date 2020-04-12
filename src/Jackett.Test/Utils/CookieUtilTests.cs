using System.Collections.Generic;
using Jackett.Common.Utils;
using NUnit.Framework;

namespace Jackett.Test.Utils
{
    [TestFixture]
    public class CookieUtilTests
    {
        [Test]
        public void CookieHeaderToDictionaryTest()
        {
            // valid cookies with non-alpha characters in the value
            var cookieHeader = "__cfduid=d6237f041586694295; __cf_bm=TlOng/xyqckk-TMen38z+0RFYA7YA=";
            var expectedCookieDictionary = new Dictionary<string, string>
            {
                {"__cfduid", "d6237f041586694295"},
                {"__cf_bm", "TlOng/xyqckk-TMen38z+0RFYA7YA="}
            };
            CollectionAssert.AreEqual(expectedCookieDictionary, CookieUtil.CookieHeaderToDictionary(cookieHeader));

            // cookie with duplicate keys and whitespace separator instead of ;
            // this cookie is not valid according to the standard, but it occurs in Jackett because we are concatenating
            // cookies in many parts of the code (and we are not doing it well). this is safe because the whitespace
            // can't be part of the key nor the value.
            cookieHeader = "__cfduid=d6237f041586694295; __cf_bm=TlOng/xyqckk-TMen38z+0RFYA7YA= __cf_bm=test";
            expectedCookieDictionary = new Dictionary<string, string>
            {
                {"__cfduid", "d6237f041586694295"},
                {"__cf_bm", "test"} // we always assume the latest value is the most recent
            };
            CollectionAssert.AreEqual(expectedCookieDictionary, CookieUtil.CookieHeaderToDictionary(cookieHeader));

            // malformed cookies
            cookieHeader = "__cfduidd6237f041586694295; __cf_;bm TlOng; good_cookie=value";
            expectedCookieDictionary = new Dictionary<string, string>
            {
                {"good_cookie", "value"},
            };
            CollectionAssert.AreEqual(expectedCookieDictionary, CookieUtil.CookieHeaderToDictionary(cookieHeader));

            // null cookie header
            expectedCookieDictionary = new Dictionary<string, string>();
            CollectionAssert.AreEqual(expectedCookieDictionary, CookieUtil.CookieHeaderToDictionary(null));
        }

        [Test]
        public void CookieDictionaryToHeaderTest()
        {
            // valid cookies with non-alpha characters in the value
            var cookieDictionary = new Dictionary<string, string>
            {
                {"__cfduid", "d6237f041586694295"},
                {"__cf_bm", "TlOng/xyqckk-TMen38z+0RFYA7YA="}
            };
            var expectedCookieHeader = "__cfduid=d6237f041586694295; __cf_bm=TlOng/xyqckk-TMen38z+0RFYA7YA=";
            CollectionAssert.AreEqual(expectedCookieHeader, CookieUtil.CookieDictionaryToHeader(cookieDictionary));

            // malformed cookies
            cookieDictionary = new Dictionary<string, string>
            {
                {"__cfd;uid", "d6237f041586694295"},
                {"__cf_=bm", "34234234"},
                {"__cf_bm", "34234 234"},
                {"good_cookie", "value"}
            };
            expectedCookieHeader = "good_cookie=value";
            CollectionAssert.AreEqual(expectedCookieHeader, CookieUtil.CookieDictionaryToHeader(cookieDictionary));

            // null cookie dictionary
            expectedCookieHeader = "";
            CollectionAssert.AreEqual(expectedCookieHeader, CookieUtil.CookieDictionaryToHeader(null));
        }
    }
}
