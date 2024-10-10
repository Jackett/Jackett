using System.Linq;
using System.Text.RegularExpressions;
using Jackett.Common.Extensions;

namespace Jackett.Common.Utils.Logging
{
    // See https://github.com/Prowlarr/Prowlarr/blob/develop/src/NzbDrone.Common/Instrumentation/CleanseLogMessage.cs
    public class CleanseLogMessage
    {
        private static readonly Regex[] _CleansingRules =
        {
            // Url
            new Regex(@"(?<=[?&: ;])(apikey|api_key|(?:(?:access|api)[-_]?)?token|pass(?:key|wd)?|auth|authkey|user|u?id|api|[a-z_]*apikey|account|pwd)=(?<secret>[^&=""]+?)(?=[ ""&=]|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"(?<=[?& ;])[^=]*?(_?(?<!use|get_)token|username|passwo?rd)=(?<secret>[^&=]+?)(?= |&|$|;)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"(?<=[?& ;])[^=]*?(pid)=(?<secret>[a-z0-9]{32}[^&=]+?)(?= |&|$|;)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"rss(24h)?\.torrentleech\.org/(?!rss)(?<secret>[0-9a-z]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"torrentleech\.org/rss/download/[0-9]+/(?<secret>[0-9a-z]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"iptorrents\.com/[/a-z0-9?&;]*?(?:[?&;](u|tp)=(?<secret>[^&=;]+?))+(?= |;|&|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"/fetch/[a-z0-9]{32}/(?<secret>[a-z0-9]{32})", RegexOptions.Compiled),
            new Regex(@"\b(\w*)?(_?(?<!use|get_)token|username|passwo?rd)=(?<secret>[^&=]+?)(?= |&|$|;)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"(?<=authkey = "")(?<secret>[^&=]+?)(?="")", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"(?<=beyond-hd\.[a-z]+/api/torrents/)(?<secret>[^&=][a-z0-9]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"(?<=beyond-hd\.[a-z]+/torrent/download/[\w\d-]+[.]\d+[.])(?<secret>[a-z0-9]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"(?:sharewood)\.[a-z]{2,3}/api/(?<secret>[a-z0-9]{16,})/", RegexOptions.Compiled | RegexOptions.IgnoreCase),

            // UNIT3D
            new Regex(@"(?<=[a-z0-9-]+\.[a-z]+/torrent/download/\d+\.)(?<secret>[^&=][a-z0-9]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase),

            // Path
            new Regex(@"""C:\\Users\\(?<secret>[^\""]+?)(\\|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"""/(home|Users)/(?<secret>[^/""]+?)(/|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase),

            // uTorrent
            new Regex(@"\[""[a-z._]*(username|password)"",\d,""(?<secret>[^""]+?)""", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"\[""(boss_key|boss_key_salt|proxy\.proxy)"",\d,""(?<secret>[^""]+?)""", RegexOptions.Compiled | RegexOptions.IgnoreCase),

            // BroadcastheNet (;torrent_pass|torrents_notify_ is for MTV)
            new Regex(@"""?method""?\s*:\s*""(getTorrents)"",\s*""?params""?\s*:\s*\[\s*""(?<secret>[^""]+?)""", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"getTorrents\(""(?<secret>[^""]+?)""", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"(?<=\?|&|;|=)(authkey|torrent_pass|torrents_notify)[_=](?<secret>[^&=]+?)(?=""|&|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase),

            // Indexer Responses
            new Regex(@"(?:avistaz|exoticaz|cinemaz|privatehd)\.[a-z]{2,3}/rss/download/(?<secret>[^&=]+?)/(?<secret>[^&=]+?)\.torrent", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"(?:animebytes)\.[a-z]{2,3}/torrent/[0-9]+/download/(?<secret>[^&=]+?)[""]", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"""(info_hash|token|((pass|rss)[- _]?key))"":""(?<secret>[^&=]+?)""", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        };

        public static string Cleanse(string message)
        {
            if (message.IsNullOrWhiteSpace())
            {
                return message;
            }

            foreach (var regex in _CleansingRules)
            {
                message = regex.Replace(message, m =>
                {
                    var value = m.Value;

                    foreach (var capture in m.Groups["secret"].Captures.OfType<Capture>().Reverse())
                    {
                        value = value.Replace(capture.Index - m.Index, capture.Length, "(removed)");
                    }

                    return value;
                });
            }

            return message;
        }
    }
}
