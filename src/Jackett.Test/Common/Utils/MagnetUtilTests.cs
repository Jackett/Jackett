using System;
using Jackett.Common.Utils;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Jackett.Test.Common.Utils
{
    [TestFixture]
    public class MagnetUtilTests
    {
        [Test]
        public void TestInfoHashToPublicMagnet()
        {
            const string infoHash = "3333333333333333333333333333333333333333";
            const string title = "Torrent Title 😀"; // with unicode characters

            // TODO: I'm not sure if unicode characters must be encoded
            // good magnet
            var magnet = MagnetUtil.InfoHashToPublicMagnet(infoHash, title);
            Assert.True(magnet.ToString().StartsWith("magnet:?xt=urn:btih:3333333333333333333333333333333333333333&dn=Torrent+Title+😀&tr="));

            // bad magnet (no info hash)
            magnet = MagnetUtil.InfoHashToPublicMagnet("", title);
            Assert.AreEqual(null, magnet);

            // bad magnet (no title)
            magnet = MagnetUtil.InfoHashToPublicMagnet(infoHash, "");
            Assert.AreEqual(null, magnet);
        }

        [Test]
        public void TestMagnetToInfoHash()
        {
            // good magnet
            var magnet = new Uri("magnet:?xt=urn:btih:3333333333333333333333333333333333333333&dn=Torrent+Title+😀&tr=udp%3A%2F%2Ftracker.com%3A6969%2Fannounce");
            var infoHash = MagnetUtil.MagnetToInfoHash(magnet);
            Assert.AreEqual("3333333333333333333333333333333333333333", infoHash);

            // bad magnet
            magnet = new Uri("magnet:?tr=udp%3A%2F%2Ftracker.com%3A6969%2Fannounce");
            infoHash = MagnetUtil.MagnetToInfoHash(magnet);
            Assert.AreEqual(null, infoHash);
        }
    }
}
