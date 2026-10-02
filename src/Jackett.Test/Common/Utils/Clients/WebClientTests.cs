using System;
using System.Net.Http;
using Jackett.Test.TestHelpers;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Jackett.Test.Common.Utils.Clients
{
    [TestFixture]
    public class WebClientTests
    {
        private class RedirectWebClient : TestWebClient
        {
            public static Uri Redirect(HttpResponseMessage response) => RedirectUri(response);
        }

        [TestCase("https://example.com/files/Linux-4×01.MP4.torrent", "https://example.com/files/Linux-4%C3%9701.MP4.torrent")]
        [TestCase("https://example.com/files/movie.torrent", "https://example.com/files/movie.torrent")]
        [TestCase("/files/Linux-4×01.MP4.torrent", "https://example.com/files/Linux-4%C3%9701.MP4.torrent")]
        [TestCase("movie.torrent", "https://example.com/movie.torrent")]
        public void TestRedirectUriLocation(string location, string expected)
        {
            var response = new HttpResponseMessage
            {
                RequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://example.com/download.php?id=1")
            };
            response.Headers.TryAddWithoutValidation("Location", location);

            Assert.AreEqual(expected, RedirectWebClient.Redirect(response).AbsoluteUri);
        }

        [TestCase("0;url=https://example.com/files/Linux-4×01.MP4.torrent", "https://example.com/files/Linux-4%C3%9701.MP4.torrent")]
        [TestCase("0;url=/files/movie.torrent", "https://example.com/files/movie.torrent")]
        public void TestRedirectUriRefresh(string refresh, string expected)
        {
            var response = new HttpResponseMessage
            {
                RequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://example.com/download.php?id=1")
            };
            response.Headers.TryAddWithoutValidation("Refresh", refresh);

            Assert.AreEqual(expected, RedirectWebClient.Redirect(response).AbsoluteUri);
        }
    }
}
