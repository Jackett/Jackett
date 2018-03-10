using NUnit.Framework;

namespace Jackett.Test
{
    abstract class TestBase
    {
        [SetUp]
        public void Setup()
        {
            TestUtil.SetupContainer();
        }
    }
}
