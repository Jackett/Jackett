using Jackett.Services;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;

namespace Jackett.Test.Services
{
    [TestFixture]
    class ProtectionServiceTests :  TestBase
    {

        [Test]
        public void Should_be_able_to_encrypt_and_decrypt()
        {
            var ss = TestUtil.Container.Resolve<IServerService>();
            ss.Config.InstanceId = "12345678";
            var ps = TestUtil.Container.Resolve<IProtectionService>();
            var input = "test123";
            var protectedInput = ps.Protect(input);
            var output = ps.UnProtect(protectedInput);

            Assert.AreEqual(output, input);
            Assert.AreNotEqual(input, protectedInput);
        }
    }
}
