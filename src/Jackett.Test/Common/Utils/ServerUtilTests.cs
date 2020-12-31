using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Jackett.Test.TestHelpers;
using NUnit.Framework;

namespace Jackett.Test.Common.Utils
{
    [TestFixture]
    internal class ServerUtilTests : TestBase
    {
        [Test]
        public void ResureRedirectIsFullyQualified_makes_redicts_fully_qualified()
        {
            var res = new WebResult
            {
                RedirectingTo = "list?p=1"
            };

            var req = new WebRequest()
            {
                Url = "http://my.domain.com/page.php"
            };

            // Not fully qualified  requiring redirect
            ServerUtil.ResureRedirectIsFullyQualified(req, res);
            Assert.AreEqual(res.RedirectingTo, "http://my.domain.com/list?p=1");

            // Fully qualified not needing modified
            res.RedirectingTo = "http://a.domain/page.htm";
            ServerUtil.ResureRedirectIsFullyQualified(req, res);
            Assert.AreEqual(res.RedirectingTo, "http://a.domain/page.htm");

            // Relative  requiring redirect
            req.Url = "http://my.domain.com/dir/page.php";
            res.RedirectingTo = "a/dir/page.html";
            ServerUtil.ResureRedirectIsFullyQualified(req, res);
            Assert.AreEqual(res.RedirectingTo, "http://my.domain.com/dir/a/dir/page.html");
        }
    }
}
