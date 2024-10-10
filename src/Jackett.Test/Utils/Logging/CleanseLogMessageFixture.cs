using FluentAssertions;
using Jackett.Common.Utils.Logging;
using NUnit.Framework;

namespace Jackett.Test.Utils.Logging
{
    [TestFixture]
    public class CleanseLogMessageFixture
    {
        [TestCase(@"WebClient(HttpWebClient2).GetResultAsync(Method: POST Url: https://some-site.org/takelogin.php PostData: {username=mySecret, password=mySecret} RawBody: )")]
        [TestCase(@"WebClient(HttpWebClient2).GetResultAsync(Method: GET Url: https://www.sharewood.tv/api/2b51db35e1910123321025a12b9933d2/last-torrents?)")]
        public void should_clean_message(string message)
        {
            var cleansedMessage = CleanseLogMessage.Cleanse(message);

            cleansedMessage.Should().NotContain("mySecret");
            cleansedMessage.Should().NotContain("123%@%_@!#^#@");
            cleansedMessage.Should().NotContain("01233210");
        }
    }
}
