using Jackett.Common.Utils;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Jackett.Test.Common.Utils
{
    [TestFixture]
    public class EnvironmentUtilTests
    {
        [Test]
        public void TestJackettVersion()
        {
            var version = EnvironmentUtil.JackettVersion();
            Assert.True(version.StartsWith("v"));
            Assert.AreEqual(3, version.Split('.').Length);
        }
    }
}
