using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JackettTest
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
