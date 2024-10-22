using System;
using FluentAssertions;
using NUnit.Framework;

namespace Jackett.Test.Common.Utils
{
    [TestFixture]
    public class UriFixture
    {
        [TestCase("abc://my_host.com:8080/root/api/")]
        [TestCase("abc://my_host.com:8080//root/api/")]
        [TestCase("abc://my_host.com:8080/root//api/")]
        [TestCase("abc://[::1]:8080/root//api/")]
        public void should_parse(string uri)
        {
            var newUri = new Uri(uri);
            newUri.AbsoluteUri.Should().Be(uri);
        }

        [TestCase("abc://host.com:8080/root/file.xml", "relative/path", "abc://host.com:8080/root/relative/path")]
        [TestCase("abc://host.com:8080/root/file.xml", "/relative/path", "abc://host.com:8080/relative/path")]
        [TestCase("abc://host.com:8080/root/file.xml?query=1#fragment", "relative/path", "abc://host.com:8080/root/relative/path")]
        [TestCase("abc://host.com:8080/root/file.xml?query=1#fragment", "/relative/path", "abc://host.com:8080/relative/path")]
        [TestCase("abc://host.com:8080/root/api", "relative/path", "abc://host.com:8080/root/relative/path")]
        [TestCase("abc://host.com:8080/root/api", "/relative/path", "abc://host.com:8080/relative/path")]
        [TestCase("abc://host.com:8080/root/api/", "relative/path", "abc://host.com:8080/root/api/relative/path")]
        [TestCase("abc://host.com:8080/root/api/", "/relative/path", "abc://host.com:8080/relative/path")]
        [TestCase("abc://host.com:8080/root/api/", "//otherhost.com/path", "abc://otherhost.com/path")]
        [TestCase("abc://host.com:8080/root/api/", "abc://otherhost.com/api/path", "abc://otherhost.com/api/path")]
        public void should_combine_uri(string basePath, string relativePath, string expected)
        {
            var newUri = new Uri(new Uri(basePath), new Uri(relativePath, UriKind.RelativeOrAbsolute));
            newUri.AbsoluteUri.Should().Be(expected);
        }
    }
}
