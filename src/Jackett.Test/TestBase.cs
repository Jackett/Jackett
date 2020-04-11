using NUnit.Framework;

namespace Jackett.Test
{
    internal abstract class TestBase
    {
        [SetUp]
        public void Setup() => TestUtil.SetupContainer();
    }
}
