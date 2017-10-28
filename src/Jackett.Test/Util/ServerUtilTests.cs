using Jackett.Utils.Clients;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Jackett.Indexers;
using Newtonsoft.Json.Linq;
using Jackett;
using Newtonsoft.Json;
using Jackett.Utils;

namespace Jackett.Test.Indexers
{
    [TestFixture]
    class ServerUtilTests : TestBase
    {
        [Test]
        public void ResureRedirectIsFullyQualified_makes_redicts_fully_qualified()
        {
            var res = new WebClientByteResult()
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
