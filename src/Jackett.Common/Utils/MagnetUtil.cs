using System;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using Jackett.Common.Helpers;

namespace Jackett.Common.Utils
{
    public static class MagnetUtil
    {
        // Update best trackers from https://github.com/ngosang/trackerslist
        private static readonly NameValueCollection _Trackers = new NameValueCollection
        {
            {"tr", "udp://tracker.opentrackr.org:1337/announce"},
            {"tr", "udp://tracker.coppersurfer.tk:6969/announce"},
            {"tr", "udp://9.rarbg.to:2710/announce"},
            {"tr", "udp://tracker.internetwarriors.net:1337/announce"},
            {"tr", "udp://tracker.cyberia.is:6969/announce"},
            {"tr", "udp://exodus.desync.com:6969/announce"},
            {"tr", "udp://explodie.org:6969/announce"},
            {"tr", "http://tracker1.itzmx.com:8080/announce"},
            {"tr", "udp://tracker.tiny-vps.com:6969/announce"},
            {"tr", "udp://open.stealth.si:80/announce"}
        };

        private static readonly string _TrackersEncoded = _Trackers.GetQueryString(null, true);

        public static Uri InfoHashToPublicMagnet(string infoHash, string title)
        {
            if (string.IsNullOrWhiteSpace(infoHash) || string.IsNullOrWhiteSpace(title))
                return null;
            return new Uri($"magnet:?xt=urn:btih:{infoHash}&dn={WebUtilityHelpers.UrlEncode(title, Encoding.UTF8)}&{_TrackersEncoded}");
        }

        public static string MagnetToInfoHash(Uri magnet)
        {
            try
            {
                var xt = ParseUtil.GetArgumentFromQueryString(magnet.ToString(), "xt");
                return xt.Split(':').Last(); // remove prefix urn:btih:
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
