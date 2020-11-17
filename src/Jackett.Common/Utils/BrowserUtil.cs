using System;

namespace Jackett.Common.Utils
{
    public static class BrowserUtil
    {
        // When updating these make sure they are not detected by the incapsula bot detection engine
        // (e.g. kickasstorrent indexer)
        public static string ChromeUserAgent => Environment.OSVersion.Platform == PlatformID.Unix ?
            "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.132 Safari/537.36" :
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.132 Safari/537.36";

    }
}
