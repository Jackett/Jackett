using Autofac;
using Jackett.Common.Models.Config;
using Jackett.Common.Services.Interfaces;
using NUnit.Framework;

namespace Jackett.Test.Services
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
            var input = "test123";
            var protectedInput = ps.Protect(input);
            var output = ps.UnProtect(protectedInput);

            Assert.AreEqual(output, input);
            Assert.AreNotEqual(input, protectedInput);
        }
    }
}
