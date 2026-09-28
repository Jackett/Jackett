using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
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
        public void CookieHeaderToDictionaryDeleted()
        {
            // a later expired value removes the cookie
            var cookieHeader = "sid=abc; cid=xyz; sid=" + CookieUtil.ExpiredCookieValue + "; flashes=test";
            var expectedCookieDictionary = new Dictionary<string, string>
            {
                {"cid", "xyz"},
                {"flashes", "test"}
            };
            CollectionAssert.AreEqual(expectedCookieDictionary, CookieUtil.CookieHeaderToDictionary(cookieHeader));
        }

        [Test]
        public void SetCookieToCookiePair()
        {
            var expired = "sid=" + CookieUtil.ExpiredCookieValue + ";";
            // PHP deletion
            Assert.AreEqual(expired, CookieUtil.SetCookieToCookiePair("sid=deleted; expires=Thu, 01-Jan-1970 00:00:01 GMT; Max-Age=0; path=/; secure; httponly"));
            Assert.AreEqual(expired, CookieUtil.SetCookieToCookiePair("sid=abc; Max-Age=0"));
            Assert.AreEqual(expired, CookieUtil.SetCookieToCookiePair("sid=abc; Expires=Wed, 21 Oct 2015 07:28:00 GMT"));
            // empty value, it would be ignored by CookieHeaderToDictionary without the placeholder
            Assert.AreEqual(expired, CookieUtil.SetCookieToCookiePair("sid=; Max-Age=0"));
            // Max-Age has precedence over Expires
            Assert.AreEqual("sid=abc;", CookieUtil.SetCookieToCookiePair("sid=abc; Expires=Wed, 21 Oct 2015 07:28:00 GMT; Max-Age=3600"));
            Assert.AreEqual("sid=abc;", CookieUtil.SetCookieToCookiePair("sid=abc; Max-Age=3600; Expires=Wed, 21 Oct 2015 07:28:00 GMT"));
            // "deleted" is a regular value if the cookie is not expired
            Assert.AreEqual("sid=deleted;", CookieUtil.SetCookieToCookiePair("sid=deleted; path=/"));
            Assert.AreEqual("sid=abc;", CookieUtil.SetCookieToCookiePair("sid=abc; expires=Fri, 01-Jan-2100 00:00:00 GMT; path=/"));
            Assert.AreEqual("sid=abc;", CookieUtil.SetCookieToCookiePair("sid=abc"));
            // Expires formats not supported by DateTime.TryParse
            Assert.AreEqual(expired, CookieUtil.SetCookieToCookiePair("sid=abc; expires=Thu Jan  1 00:00:01 1970"));
            Assert.AreEqual(expired, CookieUtil.SetCookieToCookiePair("sid=abc; expires=Thu, 01 Jan 1970 00:00:00 UTC"));
            Assert.AreEqual(expired, CookieUtil.SetCookieToCookiePair("sid=abc; expires=Thu, 01-Jan-1970 00:00:01 GMT+0000"));
            Assert.AreEqual(expired, CookieUtil.SetCookieToCookiePair("sid=abc; expires=Thursday, 01-Jan-70 00:00:01 GMT"));
            // wrong weekday
            Assert.AreEqual(expired, CookieUtil.SetCookieToCookiePair("sid=abc; expires=Mon, 01 Jan 1970 00:00:01 GMT"));
            // the last Expires wins
            Assert.AreEqual(expired, CookieUtil.SetCookieToCookiePair("sid=abc; expires=Fri, 01-Jan-2100 00:00:00 GMT; expires=Thu, 01-Jan-1970 00:00:01 GMT"));
            // Max-Age bigger than int
            Assert.AreEqual("sid=abc;", CookieUtil.SetCookieToCookiePair("sid=abc; Max-Age=99999999999; expires=Thu, 01-Jan-1970 00:00:01 GMT"));
            // cookie without name
            Assert.AreEqual("foo;", CookieUtil.SetCookieToCookiePair("foo; Max-Age=0"));
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

        [Test]
        public void RemoveAllCookies()
        {
            var cookiesContainer = new CookieContainer();
            var domainHttp = new Uri("http://testdomain1.com");
            cookiesContainer.Add(domainHttp, new Cookie("cookie1", "value1"));
            var domainHttps = new Uri("https://testdomain2.com");
            cookiesContainer.Add(domainHttps, new Cookie("cookie2", "value2"));
            Assert.AreEqual(1, cookiesContainer.GetCookies(domainHttp).Count);
            Assert.AreEqual(1, cookiesContainer.GetCookies(domainHttps).Count);

            CookieUtil.RemoveAllCookies(cookiesContainer);
            Assert.AreEqual(0, cookiesContainer.GetCookies(domainHttp).Count);
            Assert.AreEqual(0, cookiesContainer.GetCookies(domainHttps).Count);
        }

        [Test]
        public void RemoveAllCookiesWithPaths()
        {
            var cookiesContainer = new CookieContainer();

            var domainHttp = new Uri("http://testdomain1.com/path");
            var domainHttps = new Uri("https://testdomain2.com/path");

            cookiesContainer.Add(domainHttp, new Cookie("cookie1", "value1", "/"));
            cookiesContainer.Add(domainHttps, new Cookie("cookie2", "value2", "/"));
            cookiesContainer.Add(domainHttp, new Cookie("cookie3", "value3", "/path"));
            cookiesContainer.Add(domainHttps, new Cookie("cookie4", "value4", "/path"));
            cookiesContainer.Add(domainHttp, new Cookie("cookie5", "value5", "/path", ".testdomain1.com"));
            cookiesContainer.Add(domainHttps, new Cookie("cookie6", "value6", "/path", ".testdomain2.com"));

            Assert.AreEqual(3, cookiesContainer.GetCookies(domainHttp).Count);
            Assert.AreEqual(3, cookiesContainer.GetCookies(domainHttps).Count);

            CookieUtil.RemoveAllCookies(cookiesContainer);
            Assert.AreEqual(0, cookiesContainer.GetCookies(domainHttp).Count);
            Assert.AreEqual(0, cookiesContainer.GetCookies(domainHttps).Count);
        }
    }
}
