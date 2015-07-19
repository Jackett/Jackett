using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Jackett.Utils
{
    public static class ServerUtil
    {
        public static int[] RestrictedPorts = new int[] {
                                                             1,    // tcpmux
                                                              7,    // echo
                                                              9,    // discard
                                                              11,   // systat
                                                              13,   // daytime
                                                              15,   // netstat
                                                              17,   // qotd
                                                              19,   // chargen
                                                              20,   // ftp data
                                                              21,   // ftp access
                                                              22,   // ssh
                                                              23,   // telnet
                                                              25,   // smtp
                                                              37,   // time
                                                              42,   // name
                                                              43,   // nicname
                                                              53,   // domain
                                                              77,   // priv-rjs
                                                              79,   // finger
                                                              87,   // ttylink
                                                              95,   // supdup
                                                              101,  // hostriame
                                                              102,  // iso-tsap
                                                              103,  // gppitnp
                                                              104,  // acr-nema
                                                              109,  // pop2
                                                              110,  // pop3
                                                              111,  // sunrpc
                                                              113,  // auth
                                                              115,  // sftp
                                                              117,  // uucp-path
                                                              119,  // nntp
                                                              123,  // NTP
                                                              135,  // loc-srv /epmap
                                                              139,  // netbios
                                                              143,  // imap2
                                                              179,  // BGP
                                                              389,  // ldap
                                                              465,  // smtp+ssl
                                                              512,  // print / exec
                                                              513,  // login
                                                              514,  // shell
                                                              515,  // printer
                                                              526,  // tempo
                                                              530,  // courier
                                                              531,  // chat
                                                              532,  // netnews
                                                              540,  // uucp
                                                              556,  // remotefs
                                                              563,  // nntp+ssl
                                                              587,  // stmp?
                                                              601,  // ??
                                                              636,  // ldap+ssl
                                                              993,  // ldap+ssl
                                                              995,  // pop3+ssl
                                                              2049, // nfs
                                                              3659, // apple-sasl / PasswordServer
                                                              4045, // lockd
                                                              6000, // X11
                                                              6665, // Alternate IRC [Apple addition]
                                                              6666, // Alternate IRC [Apple addition]
                                                              6667, // Standard IRC [Apple addition]
                                                              6668, // Alternate IRC [Apple addition]
                                                              6669, // Alternate IRC [Apple addition]};
                                                                };
        public static bool IsPort(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            Regex numeric = new Regex(@"^[0-9]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            if (numeric.IsMatch(value))
            {
                try
                {
                    if (Convert.ToInt32(value) < 65536)
                        return true;
                }
                catch (OverflowException)
                {
                }
            }

            return false;
        }
    }
}
