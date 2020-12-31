using Autofac;
using Jackett.Common.Models.Config;
using Jackett.Common.Services.Interfaces;
using Jackett.Test.TestHelpers;
using NUnit.Framework;

namespace Jackett.Test.Server.Services
{
    [TestFixture]
    internal class ProtectionServiceTests : TestBase
    {
        [Test]
        public void Should_be_able_to_encrypt_and_decrypt()
        {
            var ss = TestUtil.Container.Resolve<ServerConfig>();
            ss.InstanceId = "12345678";
            var ps = TestUtil.Container.Resolve<IProtectionService>();
            const string input = "test123";
            var protectedInput = ps.Protect(input);
            var output = ps.UnProtect(protectedInput);

            Assert.AreEqual(output, input);
            Assert.AreNotEqual(input, protectedInput);
        }
    }
}
