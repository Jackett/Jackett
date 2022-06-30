using NUnit.Framework;

namespace Jackett.Test.TestHelpers
{
    internal abstract class TestBase
    {
        [SetUp]
        public void Setup() => TestUtil.SetupContainer();
    }
}
