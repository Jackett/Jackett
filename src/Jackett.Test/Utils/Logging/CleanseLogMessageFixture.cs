using FluentAssertions;
using Jackett.Common.Utils.Logging;
using NUnit.Framework;

namespace Jackett.Test.Utils.Logging
{
    [TestFixture]
    public class CleanseLogMessageFixture
    {
        [TestCase(@"WebClient(HttpWebClient2).GetResultAsync(Method: POST Url: https://some-site.org/takelogin.php PostData: {username=mySecret, password=mySecret} RawBody: )")]
        public void should_clean_message(string message)
        {
            var cleansedMessage = CleanseLogMessage.Cleanse(message);

            cleansedMessage.Should().NotContain("mySecret");
            cleansedMessage.Should().NotContain("123%@%_@!#^#@");
            cleansedMessage.Should().NotContain("01233210");
        }
    }
}
